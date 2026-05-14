#nullable disable
using System;
using System.Linq;
using System.Threading.Tasks;
using ContosoUniversity.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Controllers;

/// <summary>
/// rw-007 (F-006/F-007): NotificationsController for the rewrite. Replaces
/// src/ContosoUniversity/Controllers/NotificationsController.cs which drained
/// an in-process MSMQ shim and had a no-op MarkAsRead (KL-NOTIF-002).
///
/// Authorization (ADR-006):
///   * Class-level [Authorize(Roles = "Admin,Reader")] gates Index +
///     GetNotifications + MarkAsRead so anonymous requests redirect to
///     /Account/SignIn (cookie auth challenge).
///   * MarkAsRead retains [ValidateAntiForgeryToken] (sec-001 carryover) so
///     forged cross-site posts are rejected with HTTP 400.
///
/// Persistence pipeline: producers (Departments/Students/Courses/Instructors)
/// call <see cref="INotificationService.PublishAsync"/> after every successful
/// SaveChanges. The envelope crosses an in-memory bounded
/// <see cref="INotificationQueue"/> and is persisted by
/// <see cref="NotificationProcessorBackgroundService"/>. Reads land directly
/// against SQL via <see cref="INotificationService.ListRecentAsync"/>; there
/// is no in-controller draining (the legacy controller drained the queue on
/// every GET, which masked persistence failures).
/// </summary>
[Authorize(Roles = "Admin,Reader")]
public class NotificationsController : Controller
{
    private const int DefaultPageSize = 25;

    private readonly INotificationService _notifications;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notifications,
        ILogger<NotificationsController> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    // GET: Notifications
    public IActionResult Index() => View();

    // GET: Notifications/GetNotifications
    public async Task<JsonResult> GetNotifications()
    {
        var rows = await _notifications.ListRecentAsync(DefaultPageSize, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        var payload = rows.Select(n => new
        {
            id = n.Id,
            entityType = n.EntityType,
            entityId = n.EntityId,
            operation = n.Operation,
            message = n.Message,
            createdAt = n.CreatedAt,
            createdBy = n.CreatedBy,
            isRead = n.IsRead,
            readAt = n.ReadAt
        }).ToList();
        return Json(new
        {
            success = true,
            notifications = payload,
            count = payload.Count
        });
    }

    // POST: Notifications/MarkAsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new { success = false, message = "id must be a positive integer" });
        }
        var updated = await _notifications.MarkAsReadAsync(id, HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!updated)
        {
            _logger.LogInformation("MarkAsRead requested for missing notification {NotificationId}", id);
            return NotFound(new { success = false, message = "notification not found" });
        }
        return Json(new { success = true });
    }
}
