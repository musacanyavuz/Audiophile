using System;
using System.IO;
using System.Threading.Tasks;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class BannersController : AdminBaseController
    {
        public IActionResult Index()
        {
            using (var service = new BannerService())
            {
                var list = service.GetAllBanners();                
                return View(list);
            }
        }

        public IActionResult Create()
        {
            using (var userService = new UserService())
            {
                ViewBag.WaterMark = 0;
                var users = userService.GetAll();
                return View(users);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create(Banner banner)
        {
            if (banner == null)
            {
                AdminNotification = new UiMessage(NotyType.error, "Banner geçersiz");
                return Redirect("Create");
            }
            if (banner.ImageSourceFile == null)
            {
                AdminNotification = new UiMessage(NotyType.error, "Görsel geçersiz");
                return Redirect("Create");
            }
            using (var service = new BannerService())
            {
                bool useWaterMark = true;
                string waterMark = Request.Form["UseWaterMark"];
                if (waterMark != null)
                {
                    useWaterMark = Convert.ToInt16(waterMark) == 1 ? true : false;
                }
                var fileName = "banner_"+ new Random().Next(100,1000)+Path.GetExtension(banner.ImageSourceFile.FileName);
                var filePath =  new FileService().FileUpload(GetLoginID(), banner.ImageSourceFile,"/Upload/Dopings/", fileName, useWaterMark);
                banner.ImageSource = filePath;
                banner.CreatedDate = DateTime.Now;
                banner.IsApproved = true;
                banner.ApprovedDate = DateTime.Now;
                banner.ApprovedUserID = GetLoginID();

                var insert = service.Insert(banner);
                if (!insert)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner girişi başarısız");
                    return Redirect("Create");
                }
                AdminNotification = new UiMessage(NotyType.success, "Banner girişi yapıldı.");
                return Redirect("Index");
            }
        }

        #region Details

        public IActionResult Details(int id)
        {
            using (var service = new BannerService())
            {
                var banner = service.GetWithUser(id);
                if (banner == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner bulunamadı");
                    return RedirectToAction("Index");
                }
                ViewBag.WaterMark = 0;
                
                return View(banner);
            }
        }
        
        [HttpPost]
        public IActionResult UpdateEndDate(int id, DateTime endDate)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner bulunamadı");
                    return RedirectToAction("Index");
                }
                banner.EndDate = endDate;
                var update = service.Update(banner);
                if (update)
                    AdminNotification = new UiMessage(NotyType.success, "Banner bitiş tarihi güncellendi.");
                else
                    AdminNotification = new UiMessage(NotyType.error, "Banner tarihi güncellemesi başarısız.");

                return RedirectToAction("Details", new {id});
            }
        }

        public async Task<IActionResult> UpdateFile(int id, IFormFile file)
        {
            using (var service = new BannerService())
            {
                bool useWaterMark = true;
                var banner = service.Get(id);
                if (banner == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner bulunamadı");
                    return RedirectToAction("Index");
                }
                if (file == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Görsel tespit edilemedi.");
                    return RedirectToAction("Details", new {id = id});
                }
                string waterMark = Request.Form["UseWaterMark"];
                if (waterMark != null)
                {
                    useWaterMark = Convert.ToInt16(waterMark) == 1 ? true :  false;
                }
                var fileName = "banner_"+ id +"_"+ new Random().Next(100) +System.IO.Path.GetExtension(file.FileName);
                var filePath =  new FileService().FileUpload(GetLoginID(), file, "/Upload/Dopings/", fileName, useWaterMark);
                banner.ImageSource = filePath;
                var update = service.Update(banner);
                if (!update)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner görseli güncellenemedi.");
                    return RedirectToAction("Details", new {id = id});
                }
                AdminNotification = new UiMessage(NotyType.success, "Banner görseli güncellendi.");
                
            }
            return RedirectToAction("Details", new {id});
        }
        [HttpPost]
        public IActionResult UpdateTitle(int id, string title)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner bulunamadı");
                    return RedirectToAction("Details", new {id});
                }

                if (string.IsNullOrEmpty(title))
                {
                    AdminNotification = new UiMessage(NotyType.error, "Başlık geçersiz");
                    return RedirectToAction("Details", new {id});
                }

                banner.Title = title;
                var update = service.Update(banner);
                if (!update)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner güncellenemedi");
                    return RedirectToAction("Details", new {id});
                }
                
                AdminNotification = new UiMessage(NotyType.success, "Banner güncellendi");
                return RedirectToAction("Details", new {id});
            }
        }
        
        [HttpPost]
        public IActionResult UpdateUrl(int id, string url)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner bulunamadı");
                    return RedirectToAction("Details", new {id});
                }

                if (string.IsNullOrEmpty(url))
                {
                    AdminNotification = new UiMessage(NotyType.error, "URL geçersiz");
                    return RedirectToAction("Details", new {id});
                }

                banner.Url = url;
                var update = service.Update(banner);
                if (!update)
                {
                    AdminNotification = new UiMessage(NotyType.error, "URL güncellenemedi");
                    return RedirectToAction("Details", new {id});
                }
                
                AdminNotification = new UiMessage(NotyType.success, "URL güncellendi");
                return RedirectToAction("Details", new {id});
            }
        }
        
        [HttpPost]
        public IActionResult UpdateType(int id, Enums.BannerType type)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner bulunamadı");
                    return RedirectToAction("Details", new {id});
                }
                banner.TypeID = type;
                var update = service.Update(banner);
                if (!update)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Banner güncellenemedi");
                    return RedirectToAction("Details", new {id});
                }
                
                AdminNotification = new UiMessage(NotyType.success, "Banner güncellendi");
                return RedirectToAction("Details", new {id});
            }
        }
        #endregion
        
        [HttpPost]
        public JsonResult UpdateStatus(int id, bool status)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null)
                {
                    return Json(new {isSuccess = false, message = "Banner bulunamadı"});
                }

                banner.IsActive = status;
                var update = service.Update(banner);
                if (!update)
                    return Json(new {isSuccess = false, message = "Güncelleme başarısız"});
                
                return Json(new {isSuccess = true, message = "Banner aktiflik durumu güncellendi."});
            }
        }
        
        [HttpPost]
        public JsonResult Delete(int id)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                
                var delete = service.Delete(id);
                if (!delete)
                    return Json(new {isSuccess = false, message = "Silme işlemi başarısız"});

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + banner.ImageSource);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                return Json(new {isSuccess = true, message = "Banner silindi." });
            }
        }

        
    }
}