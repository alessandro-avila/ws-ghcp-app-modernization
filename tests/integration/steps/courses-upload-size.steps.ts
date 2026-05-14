// tests/integration/steps/courses-upload-size.steps.ts
// Step definitions backing tests/integration/features/sec-002-upload-size-cap.feature.
//
// These steps exercise the IIS request-filtering layer of the legacy
// ContosoUniversity MVC 5 app. The sec-002 fix changes Web.config so that IIS
// rejects POST bodies larger than 5 MB (5_242_880 bytes) with HTTP 413 BEFORE
// the ASP.NET pipeline runs. Pre-fix, IIS allows up to 10 MB and the entire
// body crosses the wire before the application returns ModelState errors.
//
// We do NOT construct a real multipart body — IIS request filtering only looks
// at the Content-Length header, so a raw byte buffer is sufficient to prove the
// IIS-layer behavior.

import { When, Then } from '@cucumber/cucumber';
import { strict as assert } from 'node:assert';
import { ContosoWorld } from '../support/world.js';

When(
  'I POST a body of {int} bytes to {string}',
  async function (this: ContosoWorld, byteCount: number, path: string) {
    // 'a' is one byte in UTF-8, so the on-disk body file ends up exactly
    // `byteCount` bytes long when world.request() writes it via fs.writeFileSync.
    const body = 'a'.repeat(byteCount);
    await this.request('POST', path, {
      // multipart/form-data is what a browser would send for the upload action,
      // but request filtering ignores the body — only Content-Length matters.
      headers: { 'Content-Type': 'multipart/form-data; boundary=---spec2cloud-sec-002' },
      body
    });
  }
);

Then(
  'the response status should not be {int}',
  function (this: ContosoWorld, forbidden: number) {
    assert.ok(this.lastResponse, 'No response captured.');
    assert.notEqual(
      this.lastResponse!.status,
      forbidden,
      `Expected status NOT to be ${forbidden}, got ${this.lastResponse!.status}. ` +
        `Body (first 300 chars): ${this.lastResponse!.body.substring(0, 300)}`
    );
  }
);
