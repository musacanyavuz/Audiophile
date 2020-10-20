using System;
using System.IO;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class BlogPostsController : AdminBaseController
    {
        // GET
        public IActionResult Index()
        {
            using (var service = new BlogPostService())
            {
                var posts = service.GetPosts();
                return View(posts);
            }
        }

        public IActionResult Details(int id)
        {
            using (var service = new BlogPostService())
            {
                var post = service.Get(id);
                if (post == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Yazı bulunamadı.");
                    return RedirectToAction("Index");
                }
                return View(post);
            }
        }

        [HttpPost]
        public IActionResult UpdateEndDate(int id, DateTime endDate)
        {
            using (var service = new BlogPostService())
            {
                var post = service.Get(id);
                if (post == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Post bulunamadı");
                    return RedirectToAction("Index");
                }
                post.DopingEndDate = endDate;
                var update = service.Update(post);
                if (update)
                    AdminNotification = new UiMessage(NotyType.success, "Yazı doping bitiş tarihi güncellendi.");
                else
                    AdminNotification = new UiMessage(NotyType.error, "Doping bitiş tarihi güncellemesi başarısız.");

                return RedirectToAction("Details", new {id});
            }
        }

        [HttpPost]
        public JsonResult UpdateStatus(int id, bool status)
        {
            using (var service = new BlogPostService())
            {
                var post = service.Get(id);
                if (post == null)
                {
                    return Json(new {isSuccess = false, message = "Yazı bulunamadı"});
                }

                post.IsActive = status;
                var update = service.Update(post);
                if (!update)
                    return Json(new {isSuccess = false, message = "Güncelleme başarısız"});
                
                return Json(new {isSuccess = true, message = "Yazı aktiflik durumu güncellendi."});
            }
        }
        
        [HttpPost]
        public JsonResult Delete(int id)
        {
            using (var service = new BlogPostService())
            {
                var delete = service.Delete(id);
                if (!delete)
                    return Json(new {isSuccess = false, message = "Silme işlemi başarısız"});

                var images = service.DeletePostImages(id);
                
                return Json(new {isSuccess = true, message = "Yazı silindi. Silinen Görsel Kaydı Sayısı: "+images});
            }
        }
        
        [HttpPost]
        public JsonResult DeleteImage(int id)
        {
            using (var service = new BlogPostService())
            {
                var image = service.GetBlogPostImage(id);
                if(image == null)
                    return Json(new {isSuccess = false, message = "Görsel bulunamadı."});
                    
                var delete = service.DeleteImage(new BlogPostImage{ID = id});
                if (!delete)
                    return Json(new {isSuccess = false, message = "Silme işlemi başarısız"});

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + image.Source);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + image.Thumbnail);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                return Json(new {isSuccess = true, message = "Görsel Silindi"});
            }
        }
    }
}