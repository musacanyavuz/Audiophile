using System.Linq;
using Audiophile.Common;
using Audiophile.Services;
using Audiophile.Web.Areas.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class SettingsController : AdminBaseController
    {
        // GET
        public IActionResult Index()
        {
            using (var service = new PublicService())
            using (var settingService = new SystemSettingService())
            {
                var settings = settingService.GetSystemSettings();
                var dopings = service.GetDopingTypes();
                var model = new SettingsIndexViewModel
                {
                    Dopings = dopings,
                    SystemSettins = settings
                };
                return View(model);
            }            
        }
        
        public IActionResult EditDoping(int id)
        {
            using (var service = new DopingService())
            {
                var doping = service.Get(id);
                if (doping == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Doping bulunamadı.");
                    return RedirectToAction("Index");
                }
                return View(doping);
            }
        }
        [HttpPost]
        public IActionResult EditDoping(int id, int price, int days)
        {
            using (var service = new DopingService())
            {
                var doping = service.Get(id);
                if (doping == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Doping bulunamadı.");
                    return RedirectToAction("Index");
                }
                doping.Price = price;
                doping.Day = days;
                
                var update = service.Update(doping);
                if (!update)
                    AdminNotification = new UiMessage(NotyType.error, "Doping güncellenemedi.");
                else
                    AdminNotification = new UiMessage(NotyType.success, "Doping güncellendi.");
                
                return RedirectToAction("Index");
            }
        }

        public IActionResult EditSetting(int id)
        {
            using (var service = new SystemSettingService())
            {
                var setting = service.GetSystemSettings().SingleOrDefault(s => s.ID == id);
                if (setting == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Sistem tercihi bulunamadı.");
                    return RedirectToAction("Index");
                }
                return View(setting);
            }
        }
        [HttpPost]
        public IActionResult EditSetting(int id, string value)
        {
            using (var service = new SystemSettingService())
            {
                var setting = service.GetSystemSettings().SingleOrDefault(s => s.ID == id);
                if (setting == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Sistem tercihi bulunamadı.");
                    return RedirectToAction("Index");
                }
                setting.Value = value;
                var update = service.Update(setting);
                if (!update)
                    AdminNotification = new UiMessage(NotyType.error, "Ayar güncellenemedi.");
                else
                    AdminNotification = new UiMessage(NotyType.success, "Ayar güncellendi.");
                
                return RedirectToAction("Index");
            }
        }
        
    }
}