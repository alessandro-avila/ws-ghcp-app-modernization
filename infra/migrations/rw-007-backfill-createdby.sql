-- =============================================================================
-- rw-007 (closes SEC-CRITICAL-002): backfill Notification.CreatedBy.
--
-- The legacy app (src/ContosoUniversity/Services/NotificationService.cs)
-- defaulted CreatedBy to the literal string "System" when the caller did not
-- pass a userName. The rewrite (NotificationsController + NotificationService)
-- always passes the authenticated principal (User.Identity?.Name) so legacy
-- "System" rows are no longer produced going forward. This script labels the
-- pre-rewrite rows so the audit trail distinguishes "produced before user
-- attribution was wired" from "produced by an unidentified caller after the
-- rewrite" (the latter is now stamped "(unknown)" by NotificationService).
--
-- Idempotent: re-running is a no-op once every "System" row has been migrated.
-- Apply once against the ContosoUniversity database after deploying rw-007.
--
-- Invocation:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d ContosoUniversity ^
--          -i infra/migrations/rw-007-backfill-createdby.sql
-- =============================================================================

UPDATE [Notification]
SET    [CreatedBy] = 'System (pre-auth)'
WHERE  [CreatedBy] = 'System';

PRINT N'rw-007 backfill complete: Notification.CreatedBy ''System'' -> ''System (pre-auth)''.';
