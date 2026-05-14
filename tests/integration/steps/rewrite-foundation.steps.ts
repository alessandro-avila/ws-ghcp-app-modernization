// tests/integration/steps/rewrite-foundation.steps.ts
//
// Step definitions for the rewrite app (ASP.NET Core 8 + Kestrel).
// Backs tests/integration/features/rw-001a-foundation.feature and other @rewrite
// scenarios that need to switch the world's baseUrl from the legacy MVC 5 app to
// the new ASP.NET Core 8 app at http://localhost:7000.
//
// Generic steps such as `When I GET {string}`, `Then the response status should
// be {int}`, and `Then the response body should contain {string}` are defined
// in notifications-csrf.steps.ts and reused implicitly (Cucumber loads all .ts
// files under tests/integration/steps/**).

import { Given } from '@cucumber/cucumber';
import { strict as assert } from 'node:assert';
import { ContosoWorld } from '../support/world.js';

Given(
  'the rewrite ContosoUniversity app is reachable at the configured base URL',
  async function (this: ContosoWorld) {
    this.useRewriteBaseUrl();
    let response: Awaited<ReturnType<ContosoWorld['request']>> | null = null;
    try {
      response = await this.request('GET', '/');
    } catch (err) {
      assert.fail(
        `Could not reach ${this.baseUrl}/. ` +
          'Start the rewrite app from the workspace root with: ' +
          '`cd src/ContosoUniversity.Web && dotnet run --no-launch-profile --urls http://localhost:7000`. ' +
          `Set REWRITE_BASE_URL to override the default http://localhost:7000. ` +
          `Underlying error: ${(err as Error).message}`
      );
    }
    assert.ok(
      response.status >= 200 && response.status < 500,
      `Expected rewrite app to respond with a non-server-error status; got ${response.status}.`
    );
    // Reset cookies so the smoke GET above doesn't pollute scenario state.
    this.resetCookies();
  }
);
