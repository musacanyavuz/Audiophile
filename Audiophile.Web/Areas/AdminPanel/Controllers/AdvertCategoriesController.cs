using System.Linq;
using System.Threading.Tasks;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.Areas.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class AdvertCategoriesController : AdminBaseController
    {
        // GET
        public IActionResult Index()
        {
            using (var service = new AdvertCategoryService())
            {
                var categories = service.GetAllCategories();
                return View(categories);
            }
        }

        public IActionResult Edit(int id)
        {
            using (var service = new AdvertCategoryService())
            {
                var category = service.Get(id, 1);

                if (category == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Kategori bulunamadı");
                    return RedirectToAction("Index");
                }
                var categoryEn = service.Get(id, 2);
                var model = new AdvertCategoryEditViewModel
                {
                    CategoryTr = category,
                    CategoryEn = categoryEn
                };
                var categories = service.GetAllCategories();
                ViewBag.Categories = categories;
                return View(model);
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveEdit(AdvertCategoryPostModel model)
        {
            using (var fileService = new FileService())
            using (var contentService = new TextService())
            using (var service = new AdvertCategoryService())
            {
                var category = service.Get(model.ID);
                if (category == null)
                {
                    AdminNotification = new UiMessage(NotyType.error, "İlan kategorisi bulunamadı.");
                    return RedirectToAction("Index");
                }

                category.Order = model.Order;
                var isUpdated = service.Update(category);
                if (!isUpdated)
                {
                    AdminNotification = new UiMessage(NotyType.error, "İlan güncellenemedi.");
                    return RedirectToAction("Edit", new { id = model.ID });
                }
                if (category.ParentCategoryID != model.ParentCategoryID)
                {
                    category.ParentCategoryID = model.ParentCategoryID;

                    var update = service.Update(category);
                    if (!update)
                    {
                        AdminNotification = new UiMessage(NotyType.error, "İlan kategorisi güncellenemedi.");
                        return RedirectToAction("Edit", new { id = model.ID });
                    }
                }

                if (model.File != null)
                {
                    var fileName = model.SlugTr + System.IO.Path.GetExtension(model.File.FileName);
                    var filePath =  new FileService().FileUpload(GetLoginID(),model.File, "/Upload/Categories/", fileName);
                    category.IconSource = filePath;
                    var update = service.Update(category);
                    if (!update)
                    {
                        AdminNotification = new UiMessage(NotyType.error, "Kategori görseli güncellenemedi.");
                        return RedirectToAction("Edit", new { id = model.ID });
                    }
                }


                /* Türkçe Güncellemeler */
                contentService.Update(category.NameID, model.NameTr, 1);
                contentService.Update(category.DescriptionID, model.DescriptionTr, 1);
                contentService.Update(category.SeoDescriptionID, model.SeoDescriptionTr, 1);
                contentService.Update(category.SeoKeywordsID, model.SeoKeywordsTr, 1);
                contentService.Update(category.SlugID, model.SlugTr, 1);

                /* İngilizce Güncellemeler */
                contentService.Update(category.NameID, model.NameEn, 2);
                contentService.Update(category.DescriptionID, model.DescriptionEn, 2);
                contentService.Update(category.SeoDescriptionID, model.SeoDescriptionEn, 2);
                contentService.Update(category.SeoKeywordsID, model.SeoKeywordsEn, 2);
                contentService.Update(category.SlugID, model.SlugEn, 2);

                AdminNotification = new UiMessage(NotyType.success, "İlan güncellendi.");
                return RedirectToAction("Index");
            }
        }

        public IActionResult Create()
        {
            using (var service = new AdvertCategoryService())
            {
                var categories = service.GetAll();
                return View(categories);
            }
        }
        [HttpPost]
        public async Task<IActionResult> Create(AdvertCategoryPostModel model)
        {
            using (var fileService = new FileService())
            using (var contentService = new TextService())
            using (var service = new AdvertCategoryService())
            {
                var category = new AdvertCategory();
                category.ParentCategoryID = model.ParentCategoryID;
                category.Order = model.Order;
                category.IsActive = model.IsActive;

                var contentKey = contentService.GetNextKey();
                category.NameID = contentKey;
                contentKey++;
                category.DescriptionID = contentKey;
                contentKey++;
                category.SeoDescriptionID = contentKey;
                contentKey++;
                category.SeoKeywordsID = contentKey;
                contentKey++;
                category.SlugID = contentKey;

                contentService.Insert(category.NameID, model.NameTr, model.NameEn);
                contentService.Insert(category.DescriptionID, model.DescriptionTr, model.DescriptionEn);
                contentService.Insert(category.SeoDescriptionID, model.SeoDescriptionTr, model.SeoDescriptionEn);
                contentService.Insert(category.SeoKeywordsID, model.SeoKeywordsTr, model.SeoKeywordsEn);
                contentService.Insert(category.SlugID, model.SlugTr, model.SlugEn);

                var insert = service.Insert(category);
                if (!insert)
                {
                    AdminNotification = new UiMessage(NotyType.error, "Kategori kayıt edilemedi.");
                    return RedirectToAction("Create");
                }
                if (model.File != null)
                {
                    var fileName = model.SlugTr + System.IO.Path.GetExtension(model.File.FileName);
                    var filePath =  fileService.FileUpload(GetLoginID(), model.File, "/Upload/Categories/", fileName);
                    category.IconSource = filePath;
                    var update = service.Update(category);
                    if (!update)
                    {
                        AdminNotification = new UiMessage(NotyType.error, "Kategori görseli kayıt edilemedi.");
                        return RedirectToAction("Edit", new { id = model.ID });
                    }
                }

                AdminNotification = new UiMessage(NotyType.success, "İlan kategorisi eklendi.");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public JsonResult UpdateStatus(int id, bool status)
        {
            using (var service = new AdvertCategoryService())
            {
                var category = service.Get(id);
                if (category == null)
                {
                    return Json(new { isSuccess = false, message = "Kategori bulunamadı" });
                }

                category.IsActive = status;
                var update = service.Update(category);
                if (!update)
                    return Json(new { isSuccess = false, message = "Güncelleme başarısız" });

                return Json(new { isSuccess = true, message = "İlan kategorisi aktiflik durumu güncellendi." });
            }
        }
    }
}