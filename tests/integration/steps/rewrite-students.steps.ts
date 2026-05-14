// tests/integration/steps/rewrite-students.steps.ts
//
// Step definitions specific to the rw-004 StudentsController feature
// (tests/integration/features/rw-004-students.feature).
//
// Reuses the generic HTTP / antiforgery / sign-in step library defined in
// notifications-csrf.steps.ts (legacy) and rewrite-auth.steps.ts (rewrite).
// This file only adds the positional ordering assertion needed by the
// `?sortOrder=name_desc` scenario.

import { Then } from '@cucumber/cucumber';
import { strict as assert } from 'node:assert';
import { ContosoWorld } from '../support/world.js';

Then(
  'the response body should have {string} appear before {string}',
  function (this: ContosoWorld, first: string, second: string) {
    assert.ok(this.lastResponse, 'No response captured.');
    const body = this.lastResponse!.body;
    const firstIdx = body.indexOf(first);
    const secondIdx = body.indexOf(second);
    assert.ok(
      firstIdx !== -1,
      `Expected body to contain "${first}". Body (first 300 chars): ${body.substring(0, 300)}`
    );
    assert.ok(
      secondIdx !== -1,
      `Expected body to contain "${second}". Body (first 300 chars): ${body.substring(0, 300)}`
    );
    assert.ok(
      firstIdx < secondIdx,
      `Expected "${first}" (at index ${firstIdx}) to appear before "${second}" (at index ${secondIdx}) in the response body.`
    );
  }
);
