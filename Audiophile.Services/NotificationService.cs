using System;
using System.Collections.Generic;
using System.Linq;
using Audiophile.Models;
using Dapper.FastCrud;

namespace Audiophile.Services
{
    public class NotificationService : BaseService
    {
        public Notification Get(int id)
        {
            try
            {
                return GetConnection().Get(new Notification {ID = id});
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        public bool Insert(Notification notification)
        {
            try
            {
                GetConnection().Insert(notification);
                //if (notification.ID > 0)
                //    SendMailNotification(notification);
                return notification.ID > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        
        public bool Update(Notification notification)
        {
            try
            {
                return GetConnection().Update(notification); 
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public List<Notification> GetNotifications(int userId)
        {
            try
            {
                return GetConnection().Find<Notification>(s => s
                    .Where($"{nameof(Notification.UserID):C}=@userId and {nameof(Notification.ReadedDate):C} is null")
                    .WithParameters(new {userId})
                ).ToList();
            }
            catch (Exception e)
            {
                return null;
            }
        }


        public void SendMailNotification(Notification notification)
        {
            try
            {
                using (var mailing = new MailingService())
                using (var userService = new UserService())
                {
                    var user = userService.Get(notification.UserID);
                    if (user == null)
                        return;

                    mailing.SendNotificationMail(user, notification);
                }
            }
            catch (Exception e)
            {
            }
        }
        
        
        
    }
}