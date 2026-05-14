// tests/integration/steps/rewrite-hardening.steps.ts
//
// Step definitions for the rewrite app's production-hardening middleware (rw-001d).
// Backs tests/integration/features/rw-001d-hardening.feature.
//
// All requests target the rewrite app (defaults to https://localhost:7001).
// Generic steps such as `When I GET {string}`, `Then the response status should
// be {int}`, `Then the response body should contain {string}`, and `Then the
// response body should not contain {string}` are defined in
// notifications-csrf.steps.ts and reused implicitly (Cucumber loads all .ts
// files under tests/integration/steps/**).

import { Then } from '@cucumber/cucumber';
import { strict as assert } from 'node:assert';
import { ContosoWorld } from '../support/world.js';

Then(
  'the response should include header {string} with value {string}',
  function (this: ContosoWorld, headerName: string, expectedValue: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    const actual = this.lastResponse!.headers[headerName.toLowerCase()];
    assert.ok(
      actual !== undefined,
      `Expected response to include header "${headerName}". ` +
        `Headers seen: [${Object.keys(this.lastResponse!.headers).join(', ')}].`
    );
    assert.equal(
      actual,
      expectedValue,
      `Expected header "${headerName}" to equal "${expectedValue}", got "${actual}".`
    );
  }
);

Then(
  'the response should include header {string} matching {string}',
  function (this: ContosoWorld, headerName: string, fragment: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    const actual = this.lastResponse!.headers[headerName.toLowerCase()];
    assert.ok(
      actual !== undefined,
      `Expected response to include header "${headerName}". ` +
        `Headers seen: [${Object.keys(this.lastResponse!.headers).join(', ')}].`
    );
    assert.ok(
      actual!.includes(fragment),
      `Expected header "${headerName}" to contain "${fragment}", got "${actual}".`
    );
  }
);
