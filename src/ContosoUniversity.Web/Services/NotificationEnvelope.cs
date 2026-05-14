using System;
using ContosoUniversity.Web.Domain;

namespace ContosoUniversity.Web.Services;

/// <summary>
/// rw-007 (F-006): in-memory transport DTO for the producer→queue→consumer
/// notification pipeline. Created by controllers via
/// <see cref="INotificationService.PublishAsync"/>; placed onto the bounded
/// <see cref="INotificationQueue"/>; drained by
/// <see cref="NotificationProcessorBackgroundService"/> which calls
/// <see cref="INotificationService.PersistAsync"/>.
///
/// Replaces the legacy <c>System.Messaging</c>-based payload that crossed the
/// MessageQueue boundary as XML. The rewrite keeps everything in-process so the
/// payload is a simple immutable record — no serializer, no MSMQ shim.
/// </summary>
public sealed record NotificationEnvelope(
    string EntityType,
    string EntityId,
    EntityOperation Operation,
    string DisplayName,
    string CreatedBy,
    DateTime CreatedAt);
