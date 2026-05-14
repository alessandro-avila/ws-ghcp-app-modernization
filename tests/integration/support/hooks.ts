// tests/integration/support/hooks.ts — Cucumber lifecycle hooks
// - Disables TLS certificate verification for the legacy IIS Express self-signed cert
//   (curl `-k` also bypasses cert checks; this also helps any direct fetch usage).
// - Resets per-scenario state on the World.
// - Cleans up the per-scenario tmp directory.

import { After, Before, BeforeAll } from '@cucumber/cucumber';
import { ContosoWorld } from './world.js';

BeforeAll(function () {
  process.env['NODE_TLS_REJECT_UNAUTHORIZED'] = '0';
});

Before(function (this: ContosoWorld) {
  this.resetCookies();
});

After(function (this: ContosoWorld) {
  this.cleanup();
});

