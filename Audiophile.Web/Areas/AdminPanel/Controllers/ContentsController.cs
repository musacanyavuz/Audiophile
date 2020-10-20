using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class ContentsController : AdminBaseController
    {
        // GET
        public IActionResult Index()
        {
            using (var service = new TextService())
            {
                var list = service.GetContents();
                return View(list);
            }
        }

        public IActionResult Edit(int id)
        {
            using (var service = new TextService())
            {
                var content = service.GetContent(id);
                if (content == null)
                    return RedirectToAction("Index");
                    
                if (content.TextType == Enums.TextType.Normal)
                {
                    return View("EditNormal", content);
                }
                else if (content.TextType == Enums.TextType.ZenginIcerik)
                {
                    return View("EditRichText", content);
                }
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public IActionResult Edit(Content content)
        {
            using (var service = new TextService())
            {
                if (content.ID > 0)
                {
                    content.TextType = Enums.TextType.Normal;
                    var update = service.Update(content);
                    if (update)
                        AdminNotification = new UiMessage(NotyType.success, "İçerik güncellendi");
                    else
                        AdminNotification = new UiMessage(NotyType.error, "İçerik güncelleme hatası oluştu");
                }
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Route("Contents/Edit/{id}")]     
        public IActionResult EditRichText(Content content)
        {
            using (var service = new TextService())
            {
                if (content.ID > 0)
                {
                    content.TextType = Enums.TextType.ZenginIcerik;
                    var update = service.Update(content);
                    if (update)
                        AdminNotification = new UiMessage(NotyType.success, "İçerik güncellendi");
                    else
                        AdminNotification = new UiMessage(NotyType.error, "İçerik güncelleme hatası oluştu");
                }
                return RedirectToAction("Index");
            }
        }
        
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Content content)
        {
            if (content == null || string.IsNullOrEmpty(content.TextContent))
            {
                AdminNotification = new UiMessage(NotyType.error, "Geçersiz bir giriş yaptınız.");
                return RedirectToAction("Index");
            }
            using (var service = new TextService())
            {
                content.TextType = Enums.TextType.Normal;
                var insert = service.Insert(content);
                if (!insert)
                    AdminNotification = new UiMessage(NotyType.error, "Giriş başarısız oldu.");
                else
                    AdminNotification = new UiMessage(NotyType.success, "İçerik girişiniz yapıldı.");
            }
            return RedirectToAction("Index");
        }
    
        public IActionResult CreateRichText()
        {
            return View();
        }
    
        [HttpPost]
        public IActionResult CreateRichText(Content content)
        {
            if (content == null || string.IsNullOrEmpty(content.TextContent))
            {
                AdminNotification = new UiMessage(NotyType.error, "Geçersiz giriş yaptınız.");
                return View();
            }
            
            using (var service = new TextService())
            {
                var contentArrayType = int.Parse(content.Key.ToString());

                content.TextType = Enums.TextType.ZenginIcerik;
                content.Key = service.GetNextKey();
                var insertContent = service.Insert(content);
                if (!insertContent)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Zengin içerik girişi yapılamadı.");
                    return View();
                }
                var contentId = int.Parse(content.Key.ToString());
                
                content.ID = 0;
                content.TextContent = content.Title;
                content.TextType = Enums.TextType.Normal;
                content.Key = contentId + 1;

                var insertTitle = service.Insert(content);
                if (!insertContent)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Başlık girişi yapılamadı.");
                    return View();
                }
                var titleId = contentId + 1;

                if (content.Key > 0)
                {
                    var contentArray = new ContentArray
                    {
                        ContentID = contentId,
                        TypeID = contentArrayType,
                        TitleID = titleId
                    };
                    var insertArray = service.Insert(contentArray);
                    if (!insertArray)
                    {
                        AdminNotification = new UiMessage(NotyType.error, "İçerik listesi girişi yapılamadı.");
                    }

                    AdminNotification = new UiMessage(NotyType.success, "İçerik girişi yapıldı.");
                }



                return Redirect("Index");
            }
        }
    }
}