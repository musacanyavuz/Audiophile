using System;
using System.Collections.Generic;
using System.IO;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.Areas.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using OfficeOpenXml;
using OfficeOpenXml.Table;

namespace Audiophile.Web.Areas.AdminPanel.Controllers
{
    public class AdvertsController : AdminBaseController
    {
        // GET
        public IActionResult Index()
        {
            using (var service = new AdvertService())
            {
                //var list = service.GetAdverts();
                return View(null);
            }
        }

        [Route("AdminPanel/Listing/GetList")]
        public JsonResult GetList(Search search, int start, int length)
        {
            using (var service = new AdvertService())
            {
                var adverts = service.GetList(search?.Value, length, start);
                var count = service.GetAdvertsCount();
                var filteredCount = 0;
                if (!string.IsNullOrEmpty(search?.Value))
                    filteredCount = service.GetAdvertsCount(search?.Value);

                return Json(new
                {
                    recordsTotal = count,
                    recordsFiltered = filteredCount == 0 ? count : filteredCount,
                    data = adverts
                });
            }
        }

        public IActionResult AddDoping(int id)
        {
            using (var publicService = new PublicService())
            using (var service = new AdvertService())
            {
                var ad = service.GetAdvert(id);
                if (ad == null)
                {
                    return RedirectToAction("Index");
                }
                var dopingTypes = publicService.GetDopingTypes();
                var model = new AdvertsAddDopingViewModel
                {
                    Advert = ad,
                    DopingTypes = dopingTypes
                };
                return View(model);
            }
        }
        [HttpPost]
        public IActionResult AddDoping(AdvertDoping advertDoping)
        {
            using (var service = new AdvertService())
            {
                var insert = service.InsertAdvertDoping(advertDoping);
                if (insert)
                    AdminNotification = new UiMessage(NotyType.success, "Doping eklendi.");
                else
                    AdminNotification = new UiMessage(NotyType.error, "Doping eklenirken hata oluştu.");
            }
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            using (var service = new AdvertService())
            {
                var ad = service.GetAdvert(id);
                if (ad == null)
                    return RedirectToAction("Index");

                return View(ad);
            }
        }

        [HttpPost]
        public IActionResult Update(AdvertEditModel advertEditModel)
        {
            using (var service = new AdvertService())
            {
                var ad = service.GetAdvert(advertEditModel.Id);
                ad.Title = advertEditModel.Title;
                ad.Brand = advertEditModel.Brand;
                ad.Model = advertEditModel.Model;
                service.Update(ad);
                return RedirectToAction("Details", new { id = advertEditModel.Id });
            }
        }


        [HttpPost]
        [Route("AdminPanel/UpdateDopingStatus")]
        public JsonResult UpdateDopingStatus(int id, bool status)
        {
            using (var service = new AdvertService())
            {
                var doping = service.GetAdvertDoping(id);
                if (doping == null)
                    return Json(new { isSuccess = false, message = "Doping bulunamadı" });
                doping.IsActive = status;
                var update = service.UpdateAdvertDoping(doping);
                if (!update)
                    return Json(new { isSuccess = false, message = "Doping güncelleme başarısız" });
                return Json(new { isSuccess = true, message = "İlgili doping " + (status ? "aktif" : "pasif") + " olarak güncellendi!" });
            }
        }
        [HttpPost]
        [Route("AdminPanel/DeleteListing")]
        public JsonResult DeleteListing(int id)
        {
            using (var service = new AdvertService())
            {
                var ad = service.GetAdvert(id);
                if (ad == null)
                    return Json(new { isSuccess = false, message = "İlan bulunamadı" });

                if (ad.Photos != null && ad.Photos.Any())
                {
                    try
                    {
                        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        foreach (var photo in ad.Photos)
                        {
                            path = path + photo.Thumbnail;
                            if (System.IO.File.Exists(path))
                            {
                                System.IO.File.Delete(path);
                            }
                            path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + photo.Source);
                            if (System.IO.File.Exists(path))
                            {
                                System.IO.File.Delete(path);
                            }
                            service.DeletePhoto(photo);
                        }
                    }
                    catch (Exception e)
                    {
                        using (var logService = new LogServices())
                        {
                            logService.Log(new Log
                            {
                                Function = "AdvertServie.DeletePublishRequests",
                                CreatedDate = DateTime.Now,
                                Message = e.Message,
                                Detail = e.ToString(),
                                IsError = true,
                                Params = "advertId" + id.ToString()
                            });
                        }


                       
                    }
                }
                service.DeletePublishRequests(id);
                var delete = service.Delete(id);
                if (!delete)
                    return Json(new { isSuccess = false, message = "İlan silinemedi" });

                return Json(new { isSuccess = true, message = "İlan silindi." });
            }
        }

        [HttpPost]
        [Route("AdminPanel/UpdateListingStatus")]
        public JsonResult UpdateListingStatus(int id, bool status)
        {
            using (var service = new AdvertService())
            {
                var ad = service.GetAdvert(id);
                if (ad == null)
                {
                    return Json(new { isSuccess = false, message = "İlan bulunamadı" });
                }

                ad.IsActive = status;
                var update = service.Update(ad);
                if (!update)
                    return Json(new { isSuccess = false, message = "Güncelleme başarısız" });

                return Json(new { isSuccess = true, message = "İlan aktiflik durumu güncellendi." });
            }
        }

        [HttpPost]
        [Route("AdminPanel/DeleteListingImage")]
        public JsonResult DeleteListingImage(int id)
        {
            using (var service = new AdvertService())
            {
                var image = service.GetPhoto(id);
                if (image == null)
                    return Json(new { isSuccess = false, message = "İlgili görsel bulunamadı." });

                var delete = service.DeletePhoto(new AdvertPhoto { ID = id });
                if (!delete)
                    return Json(new { isSuccess = false, message = "İlgili görseli silme işlemi başarısız" });

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + image.Thumbnail);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + image.Source);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }

                return Json(new { isSuccess = true, message = "İlgili görsel başarıyla silindi" });
            }
        }

        public IActionResult ExcelExport()
        {
            string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            using (var package = new ExcelPackage())
            using (var service = new AdvertService())
            {
                var adverts = service.GetAdverts();
                var worksheet = package.Workbook.Worksheets.Add("UserExpert");
                var export = new List<Advert>();
                foreach (var advert in adverts)
                {
                    export.Add(advert);
                }
                worksheet.Cells["A1"].LoadFromCollection(export, true, TableStyles.Light8);
                for (var col = 1; col < 50 + 1; col++)
                {
                    worksheet.Column(col).AutoFit();
                }
                return File(package.GetAsByteArray(), XlsxContentType,
                    "adverts.xlsx");
            }
        }
    }
}