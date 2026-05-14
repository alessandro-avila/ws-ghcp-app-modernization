// tests/integration/support/hooks.ts — Cucumber lifecycle hooks
// - Disables TLS certificate verification for the legacy IIS Express self-signed cert
//   (curl `-k` also bypasses cert checks; this also helps any direct fetch usage).
// - Resets per-scenario state on the World.
// - Cleans up the per-scenario tmp directory.
// - Scrubs cumulative test pollution from prior runs (Person/Course/Department/
//   Notification rows added by Create/Edit scenarios) before the suite starts.
//   Seed rows (LastName LIKE 'Rewrite-Seed-%' / 'Seed' + Reader-Sortable students /
//   CourseID=99001) are preserved.

import { After, Before, BeforeAll } from '@cucumber/cucumber';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { ContosoWorld } from './world.js';

const execFileAsync = promisify(execFile);

BeforeAll(async function () {
  process.env['NODE_TLS_REJECT_UNAUTHORIZED'] = '0';
  // Best-effort cleanup of pollution from previous cucumber runs. Failures
  // here are non-fatal — sqlcmd may not be available in CI, and the seeders
  // re-create everything on host start anyway.
  try {
    const sql = [
      "UPDATE Department SET InstructorID=NULL WHERE InstructorID IN (SELECT ID FROM Person WHERE LastName LIKE 'rw-%' AND LastName NOT LIKE 'Rewrite-Seed-%');",
      "DELETE FROM CourseAssignment WHERE InstructorID IN (SELECT ID FROM Person WHERE LastName LIKE 'rw-%' AND LastName NOT LIKE 'Rewrite-Seed-%');",
      "DELETE FROM OfficeAssignment WHERE InstructorID IN (SELECT ID FROM Person WHERE LastName LIKE 'rw-%' AND LastName NOT LIKE 'Rewrite-Seed-%');",
      "DELETE FROM Enrollment WHERE StudentID IN (SELECT ID FROM Person WHERE LastName LIKE 'rw-%' AND LastName NOT LIKE 'Rewrite-Seed-%');",
      "DELETE FROM Person WHERE LastName LIKE 'rw-%' AND LastName NOT LIKE 'Rewrite-Seed-%';",
      "DELETE FROM Notification WHERE EntityId LIKE 'rw-%' OR Message LIKE '%rw-%';",
      "DELETE FROM Department WHERE Name LIKE 'rw-%' AND Name NOT LIKE 'Rewrite-Seed-%';",
      "DELETE FROM Enrollment WHERE CourseID IN (SELECT CourseID FROM Course WHERE CourseID >= 99100 AND CourseID < 99999 AND Title LIKE 'rw-%');",
      "DELETE FROM CourseAssignment WHERE CourseID IN (SELECT CourseID FROM Course WHERE CourseID >= 99100 AND CourseID < 99999 AND Title LIKE 'rw-%');",
      "DELETE FROM Course WHERE CourseID >= 99100 AND CourseID < 99999 AND Title LIKE 'rw-%';",
      // The rw-006 AC#10 scenario intentionally edits the seeded Instructor to
      // LastName='rw-006-admin-edit' / OfficeAssignment.Location=''. The
      // PERSON cleanup above already removes that renamed row. Re-create the
      // canonical seed (Instructor + OfficeAssignment + CourseAssignment +
      // Enrollment) so the next suite run starts from the documented seed
      // state (the in-process SeedSchoolData only runs once at host start).
      "IF NOT EXISTS (SELECT 1 FROM Person WHERE LastName='Rewrite-Seed-Instructor-rw006') BEGIN INSERT INTO Person (LastName, FirstName, HireDate, Discriminator) VALUES ('Rewrite-Seed-Instructor-rw006','Rew','2024-01-01','Instructor'); DECLARE @iid INT = SCOPE_IDENTITY(); IF NOT EXISTS (SELECT 1 FROM OfficeAssignment WHERE InstructorID=@iid) INSERT INTO OfficeAssignment (InstructorID, Location) VALUES (@iid, 'Office 99-001'); IF EXISTS (SELECT 1 FROM Course WHERE CourseID=99001) AND NOT EXISTS (SELECT 1 FROM CourseAssignment WHERE InstructorID=@iid AND CourseID=99001) INSERT INTO CourseAssignment (InstructorID, CourseID) VALUES (@iid, 99001); END;"
    ].join(' ');
    await execFileAsync(
      'sqlcmd',
      ['-S', '(localdb)\\MSSQLLocalDB', '-d', 'ContosoUniversity', '-h', '-1', '-W', '-Q', sql],
      { maxBuffer: 5 * 1024 * 1024, windowsHide: true }
    );
  } catch {
    // Non-fatal — proceed with whatever state the DB is in.
  }
});

Before(function (this: ContosoWorld) {
  this.resetCookies();
});

After(function (this: ContosoWorld) {
  this.cleanup();
});

