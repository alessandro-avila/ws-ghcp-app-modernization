// tests/integration/steps/notifications-csrf.steps.ts
// Step definitions backing tests/integration/features/sec-001-csrf-mark-as-read.feature.
//
// Targets: legacy ContosoUniversity MVC 5 app on IIS Express (https://localhost:44300).
// All steps are HTTP-based — no browser automation required for sec-001.

import { Given, When, Then, DataTable } from '@cucumber/cucumber';
import { strict as assert } from 'node:assert';
import { ContosoWorld } from '../support/world.js';

// --- Background ---------------------------------------------------------------

Given(
  'the legacy ContosoUniversity app is reachable at the configured base URL',
  async function (this: ContosoWorld) {
    let response: Awaited<ReturnType<ContosoWorld['request']>> | null = null;
    try {
      response = await this.request('GET', '/');
    } catch (err) {
      assert.fail(
        `Could not reach ${this.baseUrl}/. ` +
          'Start the legacy app from Visual Studio (F5 on src/ContosoUniversity/ContosoUniversity.sln) ' +
          'or run IIS Express against src/ContosoUniversity/.vs/ContosoUniversity/config/applicationhost.config. ' +
          'The legacy app requires Windows Authentication; the test runner uses curl.exe --ntlm with SSPI default credentials. ' +
          `Set LEGACY_BASE_URL to override the default https://localhost:44300. Underlying error: ${(err as Error).message}`
      );
    }
    assert.ok(
      response.status >= 200 && response.status < 500,
      `Expected legacy app to respond with a non-server-error status; got ${response.status}.`
    );
    // Reset cookies so that the smoke GET above doesn't pollute the scenario state.
    this.resetCookies();
  }
);

// --- Generic HTTP steps -------------------------------------------------------

When('I GET {string}', async function (this: ContosoWorld, path: string) {
  await this.request('GET', path);
});

When(
  'I POST {string} with form data:',
  async function (this: ContosoWorld, path: string, table: DataTable) {
    const params = new URLSearchParams();
    for (const [key, value] of table.raw()) {
      params.append(key, value);
    }
    await this.request('POST', path, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: params
    });
  }
);

// --- Anti-forgery token acquisition ------------------------------------------

Given(
  'I have obtained an anti-forgery token from {string}',
  async function (this: ContosoWorld, path: string) {
    const response = await this.request('GET', path);
    assert.equal(
      response.status,
      200,
      `Expected GET ${path} to return 200 so the anti-forgery token can be parsed; got ${response.status}.`
    );
    const token = this.extractAntiForgeryToken(response.body);
    assert.ok(
      token,
      `Could not extract an anti-forgery token from ${path}. ` +
        'The page may not contain @Html.AntiForgeryToken() / @Html.BeginForm().'
    );
    // Stash on the world for the next POST step. The companion cookie is already
    // captured in the cookie jar by request().
    (this as unknown as { __antiForgeryToken?: string }).__antiForgeryToken = token;
  }
);

When(
  'I POST {string} with the anti-forgery token and form data:',
  async function (this: ContosoWorld, path: string, table: DataTable) {
    const token = (this as unknown as { __antiForgeryToken?: string }).__antiForgeryToken;
    assert.ok(
      token,
      'No anti-forgery token cached — preceding "I have obtained an anti-forgery token" step did not run.'
    );
    const params = new URLSearchParams();
    params.append('__RequestVerificationToken', token);
    for (const [key, value] of table.raw()) {
      params.append(key, value);
    }
    await this.request('POST', path, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: params
    });
  }
);

// --- Assertions ---------------------------------------------------------------

Then('the response status should be {int}', function (this: ContosoWorld, expected: number) {
  assert.ok(this.lastResponse, 'No response captured.');
  assert.equal(
    this.lastResponse!.status,
    expected,
    `Expected status ${expected}, got ${this.lastResponse!.status}. Body (first 300 chars): ` +
      this.lastResponse!.body.substring(0, 300)
  );
});

Then(
  'the response status should be one of {string}',
  function (this: ContosoWorld, allowed: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    const allowedStatuses = allowed.split(',').map((s) => Number(s.trim()));
    assert.ok(
      allowedStatuses.includes(this.lastResponse!.status),
      `Expected status to be one of [${allowedStatuses.join(', ')}], got ${this.lastResponse!.status}. ` +
        `Body (first 300 chars): ${this.lastResponse!.body.substring(0, 300)}`
    );
  }
);

Then(
  'the response Content-Type should match {string}',
  function (this: ContosoWorld, expected: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    const contentType = this.lastResponse!.headers['content-type'] ?? '';
    assert.ok(
      contentType.includes(expected),
      `Expected Content-Type to include "${expected}", got "${contentType}".`
    );
  }
);

Then(
  'the response body should contain {string}',
  function (this: ContosoWorld, expected: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    assert.ok(
      this.lastResponse!.body.includes(expected),
      `Expected body to contain "${expected}". Body (first 300 chars): ` +
        this.lastResponse!.body.substring(0, 300)
    );
  }
);

Then(
  'the response body should not contain {string}',
  function (this: ContosoWorld, forbidden: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    assert.ok(
      !this.lastResponse!.body.includes(forbidden),
      `Expected body NOT to contain "${forbidden}". Body (first 300 chars): ` +
        this.lastResponse!.body.substring(0, 300)
    );
  }
);

Then(
  'the response JSON field {string} should equal true',
  function (this: ContosoWorld, field: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    let parsed: unknown;
    try {
      parsed = JSON.parse(this.lastResponse!.body);
    } catch (err) {
      assert.fail(
        `Response body is not valid JSON. First 300 chars: ${this.lastResponse!.body.substring(0, 300)}`
      );
    }
    const value = (parsed as Record<string, unknown>)[field];
    assert.equal(value, true, `Expected JSON.${field} === true, got ${JSON.stringify(value)}.`);
  }
);
