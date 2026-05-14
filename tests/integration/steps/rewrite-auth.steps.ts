// tests/integration/steps/rewrite-auth.steps.ts
//
// Step definitions for the rewrite app's cookie/identity auth (rw-001b).
// Backs tests/integration/features/rw-001b-auth.feature.
//
// All requests target https://localhost:7001 (the rewrite HTTPS endpoint) so
// __Host- prefixed cookies are accepted (the prefix requires Secure + Path=/ +
// no Domain, which only works over HTTPS).

import { Given, Then } from '@cucumber/cucumber';
import { strict as assert } from 'node:assert';
import { ContosoWorld, HttpResponse } from '../support/world.js';

interface AuthScenarioState {
  signInResponse?: HttpResponse;
  matchedCookieRaw?: string;
  matchedCookieName?: string;
}

function authState(world: ContosoWorld): AuthScenarioState {
  const carrier = world as unknown as { __auth?: AuthScenarioState };
  carrier.__auth ??= {};
  return carrier.__auth;
}

// --- Background --------------------------------------------------------------

Given(
  'the rewrite ContosoUniversity app is reachable over HTTPS',
  async function (this: ContosoWorld) {
    this.useRewriteBaseUrl();
    let response: HttpResponse | null = null;
    try {
      response = await this.request('GET', '/');
    } catch (err) {
      assert.fail(
        `Could not reach ${this.baseUrl}/. ` +
          'Start the rewrite app from the workspace root with: ' +
          '`dotnet run --project src/ContosoUniversity.Web --launch-profile https`. ' +
          `Set REWRITE_BASE_URL to override the default https://localhost:7001. ` +
          `Underlying error: ${(err as Error).message}`
      );
    }
    assert.ok(
      response.status >= 200 && response.status < 500,
      `Expected rewrite app to respond with a non-server-error status; got ${response.status}.`
    );
    this.resetCookies();
    // Clear per-scenario auth state too so cookie/sign-in assertions are isolated.
    (this as unknown as { __auth?: AuthScenarioState }).__auth = {};
  }
);

// --- Sign-in -----------------------------------------------------------------

Given(
  'I have signed in as the seeded admin user',
  async function (this: ContosoWorld) {
    await signInAs.call(this, 'admin@contoso.test', 'Adm1n!Pass');
  }
);

Given(
  'I have signed in as the seeded reader user',
  async function (this: ContosoWorld) {
    await signInAs.call(this, 'reader@contoso.test', 'Read3r!Pass');
  }
);

async function signInAs(this: ContosoWorld, email: string, password: string): Promise<void> {
  // Fetch the sign-in form to acquire the anti-forgery token + cookie.
  const formResponse = await this.request('GET', '/Account/SignIn');
  assert.equal(
    formResponse.status,
    200,
    `Expected GET /Account/SignIn to return 200 so the anti-forgery token can be parsed; ` +
      `got ${formResponse.status}. Body (first 300 chars): ${formResponse.body.substring(0, 300)}`
  );
  const token = this.extractAntiForgeryToken(formResponse.body);
  assert.ok(
    token,
    'Could not extract an anti-forgery token from /Account/SignIn. The view may not render @Html.AntiForgeryToken().'
  );
  const params = new URLSearchParams();
  params.append('__RequestVerificationToken', token!);
  params.append('Email', email);
  params.append('Password', password);
  const post = await this.request('POST', '/Account/SignIn', {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: params
  });
  assert.ok(
    post.status === 200 || post.status === 302,
    `Expected POST /Account/SignIn to return 200 or 302 on success; got ${post.status}. ` +
      `Body (first 300 chars): ${post.body.substring(0, 300)}`
  );
  authState(this).signInResponse = post;
}

// --- Generic header / cookie assertions --------------------------------------

Then(
  'the response Location header should start with {string}',
  function (this: ContosoWorld, prefix: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    const location = this.lastResponse!.headers['location'];
    assert.ok(
      location,
      `Expected a Location header, but none was set. Status was ${this.lastResponse!.status}.`
    );
    assert.ok(
      location!.startsWith(prefix),
      `Expected Location to start with "${prefix}", got "${location}".`
    );
  }
);

Then(
  'a Set-Cookie header for {string} should have been issued',
  function (this: ContosoWorld, cookieName: string) {
    const state = authState(this);
    const target = state.signInResponse ?? this.lastResponse;
    assert.ok(target, 'No sign-in response or recent response captured.');
    const match = target.setCookies.find((raw) => {
      const eq = raw.indexOf('=');
      if (eq <= 0) return false;
      return raw.slice(0, eq).trim() === cookieName;
    });
    assert.ok(
      match,
      `Expected a Set-Cookie for "${cookieName}". Observed Set-Cookie headers: ${
        target.setCookies.length === 0 ? '(none)' : target.setCookies.map((c) => c.split('=')[0]).join(', ')
      }`
    );
    state.matchedCookieRaw = match;
    state.matchedCookieName = cookieName;
  }
);

Then(
  'that Set-Cookie header should include the {word} attribute',
  function (this: ContosoWorld, attribute: string) {
    const state = authState(this);
    assert.ok(
      state.matchedCookieRaw,
      'No previously-matched Set-Cookie header. Run "a Set-Cookie header for ... should have been issued" first.'
    );
    const tokens = state.matchedCookieRaw!
      .split(';')
      .slice(1)
      .map((t) => t.trim());
    const found = tokens.some((t) => t.toLowerCase() === attribute.toLowerCase());
    assert.ok(
      found,
      `Expected Set-Cookie for "${state.matchedCookieName}" to include attribute "${attribute}". ` +
        `Attributes seen: [${tokens.join(', ')}]. Raw: ${state.matchedCookieRaw}`
    );
  }
);

Then(
  'that Set-Cookie header should include {string}',
  function (this: ContosoWorld, attribute: string) {
    // For attributes that include "=", e.g. `SameSite=Strict`.
    const state = authState(this);
    assert.ok(
      state.matchedCookieRaw,
      'No previously-matched Set-Cookie header. Run "a Set-Cookie header for ... should have been issued" first.'
    );
    const tokens = state.matchedCookieRaw!
      .split(';')
      .slice(1)
      .map((t) => t.trim().toLowerCase());
    const found = tokens.includes(attribute.toLowerCase());
    assert.ok(
      found,
      `Expected Set-Cookie for "${state.matchedCookieName}" to include "${attribute}". ` +
        `Attributes seen: [${tokens.join(', ')}]. Raw: ${state.matchedCookieRaw}`
    );
  }
);
