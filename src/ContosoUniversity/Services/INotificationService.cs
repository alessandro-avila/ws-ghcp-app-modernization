using System;
using ContosoUniversity.Models;

namespace ContosoUniversity.Services
{
    public interface INotificationService : IDisposable
    {
        void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null);
        void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null);
        Notification ReceiveNotification();
        void MarkAsRead(int id);
    }
}
