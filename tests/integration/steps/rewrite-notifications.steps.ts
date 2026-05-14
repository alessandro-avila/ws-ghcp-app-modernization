// tests/integration/steps/rewrite-notifications.steps.ts
//
// Step definitions backing tests/integration/features/rw-007-notifications.feature.
//
// Targets: rewrite ASP.NET Core 8 app on Kestrel (https://localhost:7001).
// Reuses generic verbs from rewrite-auth.steps.ts (sign-in + reachability) and
// notifications-csrf.steps.ts (anti-forgery token + form-encoded POST + Then-
// assertions). New verbs added here cover the Notifications-specific flow:
//   - seeding a Notification row directly via sqlcmd so AC#5 has a known id
//     to mark-as-read against;
//   - posting MarkAsRead with the captured id + cached antiforgery token;
//   - polling sqlcmd to assert that a Notification row was persisted with the
//     correct CreatedBy after the Channel<T>+BackgroundService pipeline drains.
//
// All sqlcmd reads target the same LocalDB instance + ContosoUniversity database
// the app uses so the assertions are end-to-end (HTTP -> Channel<T> ->
// BackgroundService -> SaveChangesAsync -> SQL row).

import { Given, When, Then } from '@cucumber/cucumber';
import { strict as assert } from 'node:assert';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { ContosoWorld } from '../support/world.js';

const execFileAsync = promisify(execFile);

const DB_SERVER = '(localdb)\\MSSQLLocalDB';
const DB_NAME = 'ContosoUniversity';

interface NotificationState {
  seedId?: number;
  antiForgeryToken?: string;
}

function notifState(world: ContosoWorld): NotificationState {
  const carrier = world as unknown as { __rw007?: NotificationState };
  carrier.__rw007 ??= {};
  return carrier.__rw007;
}

async function runSqlScalar(sql: string): Promise<string> {
  const args = ['-S', DB_SERVER, '-d', DB_NAME, '-h', '-1', '-W', '-Q', sql];
  try {
    const { stdout } = await execFileAsync('sqlcmd', args, {
      maxBuffer: 5 * 1024 * 1024,
      windowsHide: true
    });
    return stdout.trim();
  } catch (err) {
    const e = err as NodeJS.ErrnoException & { stdout?: string; stderr?: string };
    throw new Error(
      `sqlcmd invocation failed for "${sql}": ${e.message}` +
        (e.stderr ? `\nstderr: ${e.stderr}` : '')
    );
  }
}

// --- AC#5: seed a known notification, capture its id ------------------------

Given(
  'a seed notification exists for rw-007 with id captured',
  async function (this: ContosoWorld) {
    // Insert a deterministic Notification row so the MarkAsRead test has
    // something to flip. Use a stable EntityId marker so we can re-fetch
    // afterward without races against other test scenarios.
    const marker = `rw-007-mark-${Date.now()}`;
    const out = await runSqlScalar(
      `SET NOCOUNT ON; ` +
        `INSERT INTO Notification (EntityType, EntityId, Operation, Message, CreatedAt, CreatedBy, IsRead) ` +
        `VALUES ('Department', '${marker}', 'CREATE', 'rw-007 seed marker', SYSUTCDATETIME(), 'rw-007-test', 0); ` +
        `SELECT CAST(Id AS NVARCHAR(20)) FROM Notification WHERE EntityId = '${marker}';`
    );
    // sqlcmd may interleave message rows with result rows. Pick the first
    // line that parses as a positive integer.
    const idLine = out
      .split(/\r?\n/)
      .map((l) => l.trim())
      .find((l) => /^\d+$/.test(l));
    const parsed = idLine ? parseInt(idLine, 10) : NaN;
    assert.ok(
      Number.isFinite(parsed) && parsed > 0,
      `Could not parse seed Notification.Id from sqlcmd output:\n${out}`
    );
    notifState(this).seedId = parsed;
  }
);

// --- AC#5: POST MarkAsRead with the captured id ------------------------------

When(
  'I POST the seeded notification mark-as-read form',
  async function (this: ContosoWorld) {
    const state = notifState(this);
    const token = (this as unknown as { __antiForgeryToken?: string }).__antiForgeryToken;
    assert.ok(token, 'No anti-forgery token cached — preceding token-acquisition step did not run.');
    assert.ok(state.seedId, 'No seeded Notification.Id captured — preceding seed step did not run.');
    const params = new URLSearchParams();
    params.append('__RequestVerificationToken', token);
    params.append('id', String(state.seedId));
    await this.request('POST', '/Notifications/MarkAsRead', {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: params
    });
  }
);

// --- AC#5 assertion: row.IsRead is true --------------------------------------

Then(
  'the seeded notification should be persisted with IsRead=true',
  async function (this: ContosoWorld) {
    const state = notifState(this);
    assert.ok(state.seedId, 'No seeded Notification.Id captured.');
    const value = await runSqlScalar(
      `SELECT CAST(IsRead AS INT) FROM Notification WHERE Id = ${state.seedId};`
    );
    assert.equal(
      value,
      '1',
      `Expected Notification.IsRead = 1 for seeded Id ${state.seedId}; got "${value}".`
    );
  }
);

// --- AC#6: POST a new department to trigger publish --------------------------

When(
  'I POST a new department with Name {string} Budget {string} StartDate {string} and no administrator selected',
  async function (
    this: ContosoWorld,
    name: string,
    budget: string,
    startDate: string
  ) {
    const token = (this as unknown as { __antiForgeryToken?: string }).__antiForgeryToken;
    assert.ok(token, 'No anti-forgery token cached — preceding token-acquisition step did not run.');
    const params = new URLSearchParams();
    params.append('__RequestVerificationToken', token);
    params.append('Name', name);
    params.append('Budget', budget);
    params.append('StartDate', startDate);
    // No InstructorID — the field is optional on the form.
    await this.request('POST', '/Departments/Create', {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: params
    });
  }
);

// --- AC#6 assertion: a Notification row exists with the expected attributes --

Then(
  'a notification should have been persisted with EntityType {string} Operation {string} CreatedBy {string}',
  async function (
    this: ContosoWorld,
    entityType: string,
    operation: string,
    createdBy: string
  ) {
    // Background service drains the Channel asynchronously. Poll up to ~10s
    // (20 * 500ms) for the row to appear so the assertion is not flaky.
    const sql =
      `SELECT TOP 1 CAST(Id AS NVARCHAR(20)) FROM Notification ` +
      `WHERE EntityType = '${entityType}' AND Operation = '${operation}' AND CreatedBy = '${createdBy}' ` +
      `ORDER BY Id DESC;`;
    let lastSeen = '';
    for (let attempt = 0; attempt < 20; attempt++) {
      const value = await runSqlScalar(sql);
      lastSeen = value;
      if (value && value !== '' && value.toLowerCase() !== 'null') {
        return;
      }
      await new Promise((resolve) => setTimeout(resolve, 500));
    }
    assert.fail(
      `No Notification row found within 10s for ` +
        `EntityType="${entityType}" Operation="${operation}" CreatedBy="${createdBy}". ` +
        `Last sqlcmd output: "${lastSeen}".`
    );
  }
);
