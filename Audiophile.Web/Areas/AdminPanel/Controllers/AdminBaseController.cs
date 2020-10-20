using System;
using System.Linq;
using System.Security.Claims;
using Audiophile.Common;
using Audiophile.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    [Area("AdminPanel")]
    [Authorize(Roles = "Admin")]
    public class AdminBaseController : Controller
    {
        public UiMessage AdminNotification
        {
            get => TempData["ErrorMessage"] == null ? null : TempData.Get<UiMessage>("Notification");
            set => TempData.Put("Notification", new UiMessage(){ Type = value.Type, Message = value.Message });
        }

        public int GetLoginID()
        {
            if (User.Identity.IsAuthenticated)
            {
                return int.Parse(User.Claims.Single(x => x.Type == ClaimTypes.UserData).Value);
            }
            return 0;
        }
        
        public string GetIpAddress()
        {
            try
            {
                return HttpContext.Connection.RemoteIpAddress.ToString();
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }
}