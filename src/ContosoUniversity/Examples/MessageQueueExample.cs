using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ContosoUniversity.Infrastructure;
using ContosoUniversity.Models;
using ContosoUniversity.Services;

namespace ContosoUniversity.Examples
{
    /// <summary>
    /// Example demonstrating the usage of the custom MessageQueue implementation
    /// NOTE: This example needs to be updated for ASP.NET Core dependency injection
    /// </summary>
    public class MessageQueueExample
    {
        public void BasicQueueOperations()
        {
            // Create a new queue
            var queuePath = "TestQueue";
            var queue = MessageQueue.Create(queuePath);

            // Send messages
            queue.Send("Hello World!", "Simple String Message");
            queue.Send(new { Name = "John", Age = 30 }, "JSON Object");

            // Receive messages
            var message1 = queue.Receive();
            Console.WriteLine($"Received: {message1.Body}, Label: {message1.Label}");

            var message2 = queue.Receive();
            Console.WriteLine($"Received: {message2.Body}, Label: {message2.Label}");

            // Clean up
            MessageQueue.Delete(queuePath);
        }

        public void NotificationServiceExample(IConfiguration configuration)
        {
            var notificationService = new NotificationService(configuration);

            // Send some notifications
            notificationService.SendNotification("Student", "123", "John Doe", EntityOperation.CREATE, "admin");
            notificationService.SendNotification("Course", "456", "Mathematics", EntityOperation.UPDATE, "teacher");
            notificationService.SendNotification("Department", "789", EntityOperation.DELETE, "admin");

            // Receive notifications
            Notification notification;
            while ((notification = notificationService.ReceiveNotification()) != null)
            {
                Console.WriteLine($"Notification: {notification.Message}");
                Console.WriteLine($"Created by: {notification.CreatedBy} at {notification.CreatedAt}");
                Console.WriteLine("---");
            }
        }
    }
}