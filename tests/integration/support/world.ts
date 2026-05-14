// tests/integration/support/world.ts — custom Cucumber World
// Provides an HTTP client configured for the legacy ContosoUniversity MVC 5 app
// (IIS Express, https://localhost:44300, self-signed cert, Windows Authentication).
//
// The legacy app's IIS Express config has anonymousAuthentication=disabled and
// windowsAuthentication=enabled (see src/ContosoUniversity/.vs/.../applicationhost.config).
// Node's built-in fetch does not support NTLM, so we shell out to curl.exe which
// uses Schannel/SSPI on Windows for transparent default-credentials NTLM auth
// (`--ntlm --user :`).
//
// Cookies are persisted per-scenario via curl's `-c` / `-b` cookie-jar file in
// a per-instance tmp directory cleaned up in the After hook.

import { execFile } from 'node:child_process';
import { mkdtempSync, rmSync, writeFileSync, existsSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { promisify } from 'node:util';
import { setWorldConstructor, World, IWorldOptions } from '@cucumber/cucumber';

const execFileAsync = promisify(execFile);

export interface HttpResponse {
  status: number;
  statusText: string;
  headers: Record<string, string>;
  body: string;
}

export class ContosoWorld extends World {
  /** Base URL of the system under test. Defaults to legacy MVC 5 endpoint. */
  baseUrl: string;

  /** Per-scenario tmp directory for the curl cookie jar file. */
  private tmpDir: string;
  private cookieJarPath: string;

  /** Most recent response, used by Then-steps for assertions. */
  lastResponse: HttpResponse | null = null;

  constructor(options: IWorldOptions) {
    super(options);
    this.baseUrl = process.env['LEGACY_BASE_URL'] ?? 'https://localhost:44300';
    this.tmpDir = mkdtempSync(join(tmpdir(), 'spec2cloud-'));
    this.cookieJarPath = join(this.tmpDir, 'cookies.txt');
  }

  /**
   * Issue an HTTP request to the system under test via curl.exe with NTLM/SSPI.
   * - `-k`: accept self-signed certs (IIS Express)
   * - `-s`: silent (no progress bar)
   * - `-i`: include response headers in stdout (so we can parse status + headers)
   * - `--ntlm --user :`: use Windows default credentials via SSPI
   * - `-c` / `-b`: persist cookies across requests in this scenario
   * - `-X` / `-H` / `-d`: method, headers, body
   * - `--max-redirs 0`: do not auto-follow redirects (mirrors `fetch redirect:'manual'`)
   */
  async request(
    method: 'GET' | 'POST',
    path: string,
    init: { headers?: Record<string, string>; body?: string | URLSearchParams } = {}
  ): Promise<HttpResponse> {
    const url = new URL(path, this.baseUrl).toString();
    const headers: Record<string, string> = { ...(init.headers ?? {}) };

    // Render body
    let bodyString: string | undefined;
    if (init.body !== undefined) {
      bodyString = init.body instanceof URLSearchParams ? init.body.toString() : init.body;
      if (!('Content-Type' in headers) && !('content-type' in headers)) {
        headers['Content-Type'] = 'application/x-www-form-urlencoded';
      }
    }

    const args: string[] = [
      '-k',
      '-s',
      '-i',
      '--ntlm',
      '--user', ':',
      '-c', this.cookieJarPath,
      '-b', this.cookieJarPath,
      // Note: do NOT pass --max-redirs 0 — it conflicts with NTLM's multi-step
      // 401-challenge handshake (curl exits 47 / TOO_MANY_REDIRECTS). curl does
      // not follow redirects by default; we intentionally omit -L instead.
      '-X', method
    ];
    for (const [k, v] of Object.entries(headers)) {
      args.push('-H', `${k}: ${v}`);
    }
    if (bodyString !== undefined) {
      // Use --data-binary @file to avoid curl's "@" / "&" interpretation issues
      // and handle special characters safely via a tmp file on disk.
      const bodyFile = join(this.tmpDir, `req-${Date.now()}-${Math.random().toString(36).slice(2)}.dat`);
      writeFileSync(bodyFile, bodyString, 'utf8');
      args.push('--data-binary', `@${bodyFile}`);
    }
    args.push(url);

    let stdout: string;
    try {
      const result = await execFileAsync('curl.exe', args, {
        maxBuffer: 50 * 1024 * 1024,
        windowsHide: true
      });
      stdout = result.stdout;
    } catch (err) {
      const e = err as NodeJS.ErrnoException & { stdout?: string; stderr?: string };
      throw new Error(
        `curl invocation failed for ${method} ${url}: ${e.message}` +
          (e.stderr ? `\nstderr: ${e.stderr}` : '')
      );
    }

    this.lastResponse = parseCurlResponse(stdout);
    return this.lastResponse;
  }

  resetCookies(): void {
    if (existsSync(this.cookieJarPath)) {
      try { writeFileSync(this.cookieJarPath, ''); } catch { /* best effort */ }
    }
    this.lastResponse = null;
  }

  /** Cleanup tmp dir at end of scenario. */
  cleanup(): void {
    try { rmSync(this.tmpDir, { recursive: true, force: true }); } catch { /* best effort */ }
  }

  /**
   * Extract the MVC anti-forgery token from an HTML response body.
   * Returns null if no token is present.
   */
  extractAntiForgeryToken(html: string): string | null {
    const match = html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
    return match ? match[1] : null;
  }
}

setWorldConstructor(ContosoWorld);

/**
 * Parse a curl `-i` response. Curl with NTLM emits multiple HTTP response blocks
 * (the 401 challenge, then the final authenticated response). We keep the LAST
 * block, which is the one we care about.
 */
function parseCurlResponse(raw: string): HttpResponse {
  const text = raw.replace(/\r\n/g, '\n');
  const positions: number[] = [];
  const re = /^HTTP\/[0-9.]+\s/gm;
  let m: RegExpExecArray | null;
  while ((m = re.exec(text)) !== null) {
    positions.push(m.index);
  }
  if (positions.length === 0) {
    throw new Error(`No HTTP response line found in curl output: ${text.slice(0, 200)}`);
  }
  const lastStart = positions[positions.length - 1];
  const block = text.slice(lastStart);
  const headerEnd = block.indexOf('\n\n');
  if (headerEnd === -1) {
    const statusLine = block.split('\n', 1)[0];
    const { status, statusText } = parseStatusLine(statusLine);
    return { status, statusText, headers: {}, body: '' };
  }
  const headerSection = block.slice(0, headerEnd);
  const body = block.slice(headerEnd + 2);
  const lines = headerSection.split('\n');
  const statusLine = lines.shift() ?? '';
  const { status, statusText } = parseStatusLine(statusLine);
  const headers: Record<string, string> = {};
  for (const line of lines) {
    const idx = line.indexOf(':');
    if (idx > 0) {
      const name = line.slice(0, idx).trim().toLowerCase();
      const value = line.slice(idx + 1).trim();
      headers[name] = headers[name] ? `${headers[name]}, ${value}` : value;
    }
  }
  return { status, statusText, headers, body };
}

function parseStatusLine(line: string): { status: number; statusText: string } {
  // "HTTP/1.1 200 OK"
  const parts = line.trim().split(/\s+/);
  const statusRaw = parts[1] ?? '0';
  const status = Number.parseInt(statusRaw, 10);
  const statusText = parts.slice(2).join(' ');
  return { status: Number.isFinite(status) ? status : 0, statusText };
}
