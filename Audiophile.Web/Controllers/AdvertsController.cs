using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Audiophile.Common;
using Audiophile.Common.Extensions;
using Audiophile.Common.Iyzico;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.Helpers;
using Audiophile.Web.ViewModels;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using InstallmentPrice = Audiophile.Web.ViewModels.InstallmentPrice;

namespace Audiophile.Web.Controllers
{
    public class AdvertsController : BaseController
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment Env { get; }

        public AdvertsController(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }
        #region STEP_1_IlanEkle
        [Authorize]
        [Route("IlanEkle")]
        [Route("AddListing")]
        [Route("Hesabim/IlanDuzenle/{id}")]
        [Route("MyAccount/EditListing/{id}")]
        public IActionResult AddListing(int? id) //step1.
        {
            var lang = GetLang();
            using (var userService = new UserService())
            using (var setService = new SystemSettingService())
            using (var service = new AdvertService())
            using (var categoryService = new AdvertCategoryService())
            using (var textService = new TextService())
            {
                int loginid = GetLoginID();
                var settings = setService.GetSystemSettings();
                int freeAdvertLimit = int.Parse(settings.Single(s => s.Name == Enums.SystemSettingName.FreeAdvertPublishLimit).Value);
                var user = userService.GetSecurePaymentDetail(loginid);
                decimal advertPrice = 0;


                var userEntity = userService.Get(GetLoginID());
                bool IsPaymentStepActive = false;

                int bannerCount = userService.GetBannersCount(loginid);
                IsPaymentStepActive = (service.GetUserAdverts(GetLoginID()).Count >= freeAdvertLimit); // Müşterini max ilan sayısını aşmış ise yeni lan ekeleme ücretli olacak.
               
                //if (Env.EnvironmentName == "Production")
                //{
                //    IsPaymentStepActive = false;
                //}
                //if (GetLoginID() == 2)
                //{
                //    IsPaymentStepActive = true;
                //}
                if (IsPaymentStepActive)
                    advertPrice = Convert.ToDecimal(settings.SingleOrDefault(x => x.Name == Enums.SystemSettingName.AdvertPublishPrice).Value);

                HttpContext.Session.SetString("IsPaymentStepActive", (IsPaymentStepActive).ToString());
                
                var model = new AddListingStep1ViewModel()
                {
                    AdvertCategories = categoryService.GetAll(lang),
                    Advert = (id != null) ? service.GetMyAdvert(id.Value, GetLoginID()) : null,
                    CanUseSecurePayment = !string.IsNullOrEmpty(user?.IyzicoSubMerchantKey),
                    WarningAdverts = textService.GetText(Enums.Texts.WarningAdverts, lang),
                    FreeAdvertLimit = freeAdvertLimit,
                    AdvertPrice = advertPrice,
                    IsPaymentStepActive = IsPaymentStepActive

                };
                if (id != null)
                {
                    model.Advert.Photos = service.GetPhotos(model.Advert.ID);
                }

                return View(model);
            }
        }
        [Authorize]
        [HttpPost]
        [Route("IlanEkle")]
        [Route("AddListing")]
        [Route("Hesabim/IlanDuzenle/{id}")]
        [Route("MyAccount/EditListing/{id}")]
        public IActionResult AddListing(int id, Advert advert, int[] AvailableInstallments, string SelectedPhotos, int mainImage) //step1 save func.
        {
            try
            {
                AvailableInstallments = AvailableInstallments?.Distinct().ToArray();
                var lang = GetLang();
                using (var mailing = new MailingService())
                using (var settingService = new SystemSettingService())
                using (var service = new AdvertService())
                using (var userService = new UserService())
                {
                    advert.Title = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(advert.Title.ToLower());
                    var user = userService.GetSecurePaymentDetail(GetLoginID());
                    advert.Title = advert.Title.Modify();
                    if (id > 0)
                    {
                        var _ad = service.GetMyAdvert(id, GetLoginID());
                        if (_ad != null)
                        {
                            _ad.Title = advert.Title;
                            _ad.Content = advert.Content;
                            _ad.MoneyTypeID = advert.MoneyTypeID;
                            _ad.NewProductPrice = advert.NewProductPrice;
                            _ad.Price = advert.Price;
                            _ad.Brand = advert.Brand;
                            _ad.Model = advert.Model;
                            _ad.LastUpdateDate = DateTime.Now;
                            _ad.CategoryID = advert.CategoryID;
                            _ad.StockAmount = advert.StockAmount;
                            _ad.UseSecurePayment = (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle);
                            _ad.PaymentMethodID = advert.PaymentMethodID;

                            if (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle)
                                _ad.MoneyTypeID = (int)Enums.MoneyType.tl;

                            if (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle
                                && (AvailableInstallments == null || AvailableInstallments.Length == 0))
                            {
                                _ad.AvailableInstallments = "1,2,3,6,9,12";
                            }
                            else if (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle)
                            {
                                _ad.AvailableInstallments = Constants.Array2String(AvailableInstallments);
                            }
                            _ad.IsActive = false;
                            var update = service.Update(_ad);
                            if (!update)
                            {
                                TempData.Put("UiMessage",
                                    new UiMessage
                                    {
                                        Class = "danger",
                                        Message = new Localization().Get("İlan güncellenemedi.", "Ad update failed.", lang)
                                    });
                                return RedirectToAction("AddListing");
                            }
                            advert = _ad;
                        }
                        else
                        {
                            return Redirect("/");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(advert.Title))
                        {
                            Notification = new UiMessage(NotyType.error, "Lütfen başlığı oldurunuz.", "Please fill title.", lang);
                            return Redirect(Constants.GetURL(Enums.Routing.IlanEkle, lang));
                        }

                        var resimIdler = SelectedPhotos.Split(",");
                        foreach (var im in resimIdler)
                        {
                            var resim = service.GetPhoto(int.Parse(im));
                            if (resim != null) continue;
                            Notification = new UiMessage(NotyType.error, "Resim Yüklenmediğinden Dolayı İlan Eklenemedi.",
                                "Please upload image", lang);
                            return RedirectToAction("AddListing");
                        }
                        advert.Title = advert.Title.Replace("&", " ");
                        advert.IsActive = false;
                        advert.CreatedDate = DateTime.Now;
                        advert.UserID = GetLoginID();
                        advert.UseSecurePayment = (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle);
                        advert.IpAddress = GetIpAddress();
                        advert.IsDraft = true;
                        if (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle)
                            advert.MoneyTypeID = (int)Enums.MoneyType.tl;

                        if (advert.UseSecurePayment && string.IsNullOrEmpty(user?.IyzicoSubMerchantKey))
                        {
                            Notification = new UiMessage(NotyType.error, "Güvenli ödeme bilgilerinizi doldurunuz.",
                                "Please fill your secure payment details.", lang);
                            return RedirectToAction("AddListing");
                        }
                        if (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle && (AvailableInstallments == null || AvailableInstallments.Length == 0))
                        {
                            advert.AvailableInstallments = "1,2,3,6,9,12";
                        }
                        else if (advert.PaymentMethodID == (int)Enums.PaymentMethods.KrediKartiIle)
                        {
                            advert.AvailableInstallments = Constants.Array2String(AvailableInstallments);
                        }
                        var insert = service.Insert(advert);
                        if (!insert)
                        {
                            TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("İlan oluşturulamadı.", "Ad create failed.", lang) });
                            return RedirectToAction("AddListing");
                        }
                    }
                   
                    if (advert.UseSecurePayment)
                    {
                        HttpContext.Session.SetString("IsPaymentStepActive", false.ToString()); //
                    }

                    try
                    {
                        var imageIds = UpdateNewPhotos(advert.ID, SelectedPhotos);
                        if (mainImage != 0)
                        {
                            var mainPhoto = service.GetPhoto(mainImage);
                            if (mainPhoto != null)
                            {
                                advert.CoverPhoto = mainPhoto.Source;
                                advert.Thumbnail = mainPhoto.Thumbnail;
                                advert.UserID = GetLoginID();
                                service.Update(advert);
                            }
                        }
                        else if ((string.IsNullOrEmpty(advert.Thumbnail) || string.IsNullOrEmpty(advert.CoverPhoto)) && imageIds?.Length > 0 && id == 0)
                        {
                            var mainPhoto = service.GetPhoto(imageIds[0]);
                            if (mainPhoto != null)
                            {
                                advert.CoverPhoto = mainPhoto.Source;
                                advert.Thumbnail = mainPhoto.Thumbnail;
                                advert.UserID = GetLoginID();
                                service.Update(advert);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        using (var logService = new LogServices())
                        {
                            Log log = new Log()
                            {
                                Function = "AdvertsController.AddListing",
                                Message = GetLoginID() + "id li kullanici AdvertsController.AddListing metot içerisindeki try'da hata aldi.",
                                Params = JsonConvert.SerializeObject(new List<string>
                        {
                            id.ToString(),
                            JsonConvert.SerializeObject(advert),
                            JsonConvert.SerializeObject(AvailableInstallments),
                            SelectedPhotos,
                            mainImage.ToString()
                        }),
                                Detail = JsonConvert.SerializeObject(e),
                                CreatedDate = DateTime.Now
                            };
                            logService.Insert(log);
                        }
                    }


                    var request = new AdvertPublishRequest
                    {
                        AdvertID = advert.ID,
                        IsActive = true,
                        RequestDate = DateTime.Now,
                        UserID = GetLoginID()
                    };
                    service.InsertPublishRequest(request);

                    var admins = settingService.GetSystemSettings()
                      ?.SingleOrDefault(s => s.Name == Enums.SystemSettingName.YoneticiMailleri)?.Value;
                    string message = string.Empty;
                    string mailSubject = string.Empty;
                    const string href = "https://audiophile.org/AdminPanel";
                    if (admins != null)
                    {
                        if (id == 0)
                        {
                            mailSubject = "Yeni ilan girişi yapıldı.";
                            message = $"{advert.ID} id'li ilan girişi yapıldı.<br/>Başlık: {advert.Title}<br/><a href={href}>İlanı Onaylamak İçin Tıkla</a>";
                        }
                        else
                        {
                            mailSubject = "İlan güncellemesi yapıldı.";
                            message = $"{advert.ID} id'li ilan güncellenmiştir .<br/>Başlık: {advert.Title}<br/><a href={href}>İlanı Onaylamak İçin Tıkla</a>";
                        }
                        mailing.SendMail2Admins(admins, mailSubject, message);
                    }


                }

                var route = new Localization().Get("/IlanEkle/IlanDetaylari/", "/AddListing/AdDetails/", lang) + advert.ID;
                return Redirect(route);
            }
            catch (Exception ex)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        Function = "AdvertsController.AddListing",
                        Message = GetLoginID() + "id li kullanici AdvertsController.AddListing işlem yaparken hata aldi.",
                        Params = JsonConvert.SerializeObject(new List<string>
                        {
                            id.ToString(),
                            JsonConvert.SerializeObject(advert),
                            JsonConvert.SerializeObject(AvailableInstallments),
                            SelectedPhotos,
                            mainImage.ToString()
                        }),
                        Detail = JsonConvert.SerializeObject(ex),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }

                Notification = new UiMessage(NotyType.error, "Sistem bir hata oluştu. Lütfen yazdığınız verileri kontrol ederek tekrar deneyiniz.", "Something went wrong. Please try again..", GetLang());
                return Redirect(Constants.GetURL(Enums.Routing.IlanEkle, GetLang()));

            }

        }

        private int[] UpdateNewPhotos(int advertId, string selectedPhotos)
        {
            if (string.IsNullOrEmpty(selectedPhotos)) return null;
            var photoIds = selectedPhotos.Split(',');
            var ids = (from id in photoIds where int.TryParse(id, out _) select int.Parse(id)).ToList();

            if (ids.Count <= 0) return null;
            using (var service = new AdvertService())
            {
                var update = service.UpdateAdvertPhotos(advertId, ids.ToArray());
                if (update)
                    return ids.ToArray();
            }
            return null;
        }
        #endregion

        #region STEP_2_IlanFotograflari
        [Authorize]
        [Route("IlanEkle/IlanFotograflari/{id}")]
        [Route("AddListing/AdPhotos/{id}")]
        public IActionResult AdPhotos(int id) //step2.
        {
            using (var service = new AdvertService())
            {
                var advert = service.GetMyAdvert(id, GetLoginID());
                if (advert == null)
                    return Redirect("/");

                advert.Photos = service.GetPhotos(id);
                TempData["nextStep"] = 3;
                return View(advert);
            }
        }
        [Authorize]
        [HttpPost]
        [Route("AddListing/AdPhotoUpload/{id}")]
        public async Task<IActionResult> AdPhotoUpload(int id, string MainImage)
        {
            var lang = GetLang();
            using (var fileService = new FileService())
            using (var imageService = new ImageService())
            using (var service = new AdvertService())
            {
                var advert = service.GetMyAdvert(id, GetLoginID());
                if (advert == null)
                    return Redirect("/");

                var files = HttpContext.Request.Form.Files;
                for (var index = 0; index < files.Count; index++)
                {
                    try
                    {
                        const string directory = "/Upload/Sales/";
                        var file = files[index];
                        if (file.Length < 5242880) continue;
                        var ext = Path.GetExtension(file.FileName);
                        var path = fileService.FileUpload(GetLoginID(), file, "/Upload/Sales/", $"{advert.ID}_{(index + 1)}_{DateTime.Now.ToShortDateString().Replace(".", "")}{DateTime.Now.ToShortTimeString().Replace(":", "")}{ext}");
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            //var fileName = $"{advert.ID}_{(index + 1)}_{DateTime.Now.ToShortDateString().Replace(".", "")}{DateTime.Now.ToShortTimeString().Replace(":", "")}";
                            //var optimize75 = imageService.Optimize75(path, directory, fileName);

                            var thumbName = $"{advert.ID}_{(index + 1)}_thumb_{DateTime.Now.ToShortDateString().Replace(".", "")}{DateTime.Now.ToShortTimeString().Replace(":", "")}.jpg";
                            var result = imageService.CreateThumbnail(GetLoginID(), path, directory, thumbName);
                            if (result)
                            {
                                if (file.FileName == MainImage)
                                {
                                    advert.CoverPhoto = path;
                                    advert.Thumbnail = directory + thumbName;
                                    var update = service.Update(advert);
                                    if (!update)
                                    {
                                        var tran = new Localization();
                                        var locMessage = tran.Get(" ismiyle güncellemeye çalıştığınız resim güncellenemedi!", "Something went wrong", lang);
                                        return Json(new
                                        {
                                            isSuccess = false,
                                            Message = file.FileName + locMessage
                                        });
                                    }
                                }
                                var insert = service.InsertAdvertPhoto(new AdvertPhoto
                                {
                                    AdvertID = advert.ID,
                                    Source = path,
                                    Thumbnail = directory + thumbName,
                                    CreatedDate = DateTime.Now
                                });
                                if (!insert)
                                {
                                    var tran = new Localization();
                                    var locMessage = tran.Get(" ismiyle sunucuya yüklemeye çalıştığınız resim yüklenemedi!", " the images you tried to upload to the server with their names could not be uploaded", lang);
                                    return Json(new
                                    {
                                        isSuccess = false,
                                        Message = file.FileName + locMessage
                                    });
                                }

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        using (var logService = new LogServices())
                        {
                            Log log = new Log()
                            {
                                Function = "AdvertsController.AdPhotoUpload",
                                Message = GetLoginID() + "id li kullanici AdvertsController.AdPhotoUpload işlem yaparken hata aldi.",
                                Detail = e.ToString(),
                                CreatedDate = DateTime.Now
                            };
                            logService.Insert(log);
                        }
                    }
                }
                var route = new Localization().Get("/IlanEkle/IlanDetaylari/", "/AddListing/AdDetails/", lang) + advert.ID;
                return Json(new { isSuccess = true, route });
            }
        }
        [Authorize]
        [HttpPost]
        [Route("AddListing/UploadPhoto/")]
        public IActionResult UploadPhoto(int id)
        {
            string[] optimize, directorys, idss;

            var lang = GetLang();
            using (var fileService = new FileService())
            using (var imageService = new ImageService())
            using (var service = new AdvertService())
            {

                var files = HttpContext.Request.Form.Files;
                idss = new string[files.Count];
                optimize = new string[files.Count];
                directorys = new string[files.Count];
                string path = "";
                for (var index = 0; index < files.Count; index++)
                {
                    try
                    {
                        var directory = "/Upload/Sales/";
                        var file = files[index];
                        if (file.Length < 5242880) //10mb
                        {
                            string randomFileName = GetLoginID().ToString() + "_" + (index + 1) + "_" + DateTime.Now.ToShortDateString().Replace(".", "") + DateTime.Now.ToShortTimeString().Replace(":", "");
                            var ext = Path.GetExtension(file.FileName);
                            path = fileService.FileUpload(GetLoginID(), file, "/Upload/Sales/", randomFileName + "" + ext);
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                //var fileName = GetLoginID().ToString() + "_" + (index + 1) + "_" + DateTime.Now.ToShortDateString().Replace(".", "") + DateTime.Now.ToShortTimeString().Replace(":", "");
                                //var optimize75 = imageService.Optimize75(path, directory, fileName);

                                var thumbName = GetLoginID().ToString() + "_" + (index + 1) + "_thumb" + "_" + DateTime.Now.ToShortDateString().Replace(".", "") + DateTime.Now.ToShortTimeString().Replace(":", "") + ".jpg";
                                var result = imageService.CreateThumbnail(GetLoginID(), path, directory, thumbName);
                                if (result)
                                {
                                    var advertPhoto = new AdvertPhoto
                                    {
                                        AdvertID = 0,
                                        Source = path,
                                        Thumbnail = directory + thumbName,
                                        CreatedDate = DateTime.Now
                                    };
                                    var insert = service.InsertAdvertPhoto(advertPhoto);
                                    if (!insert)
                                    {
                                        var tran = new Localization();
                                        var locMessage = tran.Get(" ismiyle sunucuya yüklemeye çalıştığınız resim yüklenemedi!", " the images you tried to upload to the server with their names could not be uploaded", lang);
                                        return Json(new
                                        {
                                            isSuccess = false,
                                            Message = file.FileName + locMessage
                                            //source = optimize75,
                                            //thumbnail = directory + thumbName,
                                            //id = advertPhoto.ID
                                        });
                                    }

                                    idss[index] = advertPhoto.ID.ToString();
                                    directorys[index] = directory + thumbName;
                                    optimize[index] = path;
                                }
                            }
                            else
                            {
                                var tran = new Localization();
                                var locMessage = tran.Get(" ismiyle sunucuya yüklemeye çalıştığınız resim yüklenemedi!", " the images you tried to upload to the server with their names could not be uploaded", lang);
                                return Json(new
                                {
                                    isSuccess = false,
                                    Message = file.FileName + locMessage
                                });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        using (var logService = new LogServices())
                        {
                            Log log = new Log()
                            {
                                Function = "AdvertsController.UploadPhoto",
                                Message = GetLoginID() + "id li kullanici AdvertsController.UploadPhoto işlem yaparken hata aldi.",
                                Detail = e.ToString(),
                                CreatedDate = DateTime.Now
                            };
                            logService.Insert(log);
                        }
                    }
                }
                return Json(new
                {
                    isSuccess = true,
                    source = optimize,
                    thumbnail = directorys,
                    id = idss
                });
                //return Json(new { isSuccess = false  });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("DeleteAdPhoto")]
        public JsonResult DeletePhoto(int id)
        {
            using (var service = new AdvertService())
            {
                var lang = GetLang(false);
                var t = new Localization();
                var photo = service.GetPhoto(id);
                if (photo?.Advert == null)
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Fotoğrafa erişilemedi.",
                        "Unable to access the photo.", lang)
                    });
                if (photo.Advert.UserID != GetLoginID())
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Size ait olmayan bir ilanı güncelleyemezsiniz.",
                        "You cannot update a post that does not belong to you.", lang)
                    });
                var photoList = service.GetPhotos(photo.AdvertID);
                if (photoList.Count == 1)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Fotoğraf silinemedi. Ürünün en az bir resmi bulunmak zorundadır.",
                            "Photo delete failed. Only one image.", lang)
                    });
                }

                var adverts = service.GetAdvert(photo.AdvertID);
                if (adverts.Thumbnail == photo.Thumbnail)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Fotoğraf silinemedi. Ana resim olduğundan dolayı silenememektedir.",
                            "Photo delete failed. Main photo not delete. ", lang)
                    });
                }
                var del = service.DeletePhoto(photo);
                if (del)
                {
                    new FileService().DeleteFile(photo.Source);
                    new FileService().DeleteFile(photo.Thumbnail);
                    return Json(new { isSuccess = true, message = t.Get("Fotoğraf silindi.", "Photo has been deleted.", lang) });
                }
                return Json(new
                {
                    isSuccess = false,
                    message = t.Get("Fotoğraf silinemedi.",
                    "Photo delete failed.", lang)
                });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("UpdatePhotosOrder")]
        public JsonResult UpdatePhotosOrder(int id, int[] photos)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });
                if (photos == null || photos.Length == 0)
                    return Json(new { isSuccess = false, message = t.Get("İlan fotoğrafı bulunamadı", "Could not found advert photo", lang) });

                for (int i = 0; i < photos.Length; i++)
                {
                    var update = service.UpdatePhotoOrder(photos[i], (i + 1));
                    if (!update)
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            message = t.Get("Sıralama güncellenirken bir hata oluştu",
                            "Ad photo order update failed", lang)
                        });
                    }
                }

                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Fotoğraf sıralaması güncellendi", "Updated order of advert photos", lang)
                });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("UpdateMainPhoto")]
        public JsonResult UpdateMainPhoto(int id, int photoId)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });
                var photo = service.GetPhoto(photoId);
                if (photo == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });

                ad.Thumbnail = photo.Thumbnail;
                var update = service.Update(ad);
                if (!update)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("İlan güncellenirken bir hata oluştu",
                        "Ad update failed", lang)
                    });
                }

                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("İlan güncellendi", "Advert updated", lang)
                });
            }
        }
        [Authorize]
        [HttpPost]
        [Route("ControlImage")]
        public JsonResult ControlImage(int id)
        {
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null)
                    return Json(new { isSuccess = false });
                var photo = service.GetPhotos(ad.ID);
                if (photo == null)
                {
                    return Json(new
                    {
                        isSuccess = false
                    });
                }

                return Json(new
                {
                    isSuccess = true
                });
            }
        }



        #endregion

        #region STEP_3.1__IlanDetaylari
        [Authorize]
        [Route("IlanEkle/IlanDetaylari/{id}")]
        [Route("AddListing/AdDetails/{id}")]
        public IActionResult AdDetails(int id) //step2.
        {
            var lang = GetLang();
            using (var service = new AdvertService())
            using (var publicService = new PublicService())
            using (var textService = new TextService())
            {
                var advert = service.GetMyAdvert(id, GetLoginID());
                var model = new AdDetailsStep2ViewModel
                {
                    Advert = advert,
                    CargoAreas = publicService.GetCargoAreas(),
                    WarningAdverts = textService.GetText(Enums.Texts.WarningAdverts, lang)
                };
                return View(model);
            }
        }
        [Authorize]
        [HttpPost]
        [Route("AddListing/AdDetails/{id}")]
        public IActionResult AdDetails(Advert advert) //step2 save.
        {
            var lang = GetLang();
            using (var service = new AdvertService())
            {
                var loginId = GetLoginID();
                var entity = service.GetMyAdvert(advert.ID, loginId);
                if (entity == null)
                {
                    TempData.Put("UiMessage", new UiMessage
                    {
                        Class = "danger",
                        Message = new Localization()
                            .Get("İlan detayları girişi yapılamadı.", "Ad details insert failed.", lang)
                    });
                    return RedirectToAction("AdDetails", new { id = advert.ID });
                }
                entity.ShippingTypeID = advert.ShippingTypeID;
                entity.CargoAreaID = advert.CargoAreaID;
                entity.FreeShipping = advert.FreeShipping;
                entity.ShippingPrice = advert.ShippingPrice;
                entity.OriginalBox = advert.OriginalBox;
                entity.TermsOfUse = advert.TermsOfUse;
                entity.WebSite = advert.WebSite;
                entity.ProductDefects = advert.ProductDefects;
                var update = service.Update(entity);
                if (!update)
                {
                    TempData.Put("UiMessage", new UiMessage
                    {
                        Class = "danger",
                        Message = new Localization()
                        .Get("İlan detayları girişi yapılamadı.", "Ad details insert failed.", lang)
                    });
                    return RedirectToAction("AdDetails", new { id = advert.ID });
                }
            }
            var route = new Localization().Get("/IlanEkle/IlaniOneCikar/", "/AddListing/FeaturedAd/", lang) + advert.ID;
            //if( Convert.ToBoolean( HttpContext.Session.GetString("PaymentStepIsActive")) == true)
            //   route = new Localization().Get("/IlanEkle/YeniIlanOdemesi/", "/AddListing/DoNewAdvertPaytment/", lang) + advert.ID;
            return Redirect(route);
        }

        #endregion

        #region //  STEP_3.2_YeniIlanOdemesi
        //[Authorize]
        //[Route("IlanEkle/YeniIlanOdemesi/{id}")]
        //[Route("AddListing/DoNewAdvertPaytment/{id}")]
        //public IActionResult DoNewAdvertPaytment(int id)
        //{
        //    var lang = GetLang();
        //    using (var service = new AdvertService())
        //    using (var textService = new TextService())
        //    {
        //        var advert = service.GetMyAdvert(id, GetLoginID());
        //        var model = new DoNewAdvertPaytmentViewModel
        //        {
        //            Advert = advert,
        //            WarningAdverts = textService.GetText(Enums.Texts.WarningAdverts, lang)
        //        };
        //        return View(model);
        //    }
        //}
        /// <summary>
        ///  Yeni ilan ekleme için ödeme alır.
        /// </summary>     
        //[Authorize]
        //[HttpPost]
        //[Route("IlanEkle/YeniIlanOdemesi/{id}")]
        //[Route("AddListing/DoNewAdvertPaytment/{id}")]
        //public JsonResult DoNewAdvertPaytment(int id, string cardName, string cardNumber, string month, string year, string cvc, bool securePayment)
        //{
        //    var lang = GetLang();

        //    using (var userService = new UserService())
        //    using (var prService = new PaymentRequestService())
        //    using (var publicService = new PublicService())
        //    using (var advertService = new AdvertService())
        //    {
        //        var advert = advertService.GetMyAdvert(id, GetLoginID());

        //        #region Validations

        //        if (advert == null)
        //            return Json(new { isSuccess = false, url = "/" });
        //        if (advert.UserID != GetLoginID())
        //            return Json(new
        //            {
        //                isSuccess = false,
        //                message = new Localization().Get("Erişim hatası.", "Access error.", lang)
        //            });
        //        if (securePayment && (string.IsNullOrEmpty(cardName) || string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(month) ||
        //            string.IsNullOrEmpty(year) || string.IsNullOrEmpty(cvc)))
        //        {
        //            return Json(new
        //            {
        //                isSuccess = false,
        //                message = new Localization().Get(
        //                    "Kredi kartı bilgilerinizi doldurunuz.",
        //                    "Please fill in your credit card information.", lang)
        //            });
        //        }



        //        #endregion
        //        var user = userService.Get(GetLoginID());
        //        if (user.District == null || user.City == null)
        //        {
        //            return Json(new
        //            {
        //                isSuccess = false,
        //                message = new Localization().Get("Lütfen Üyelik Bilgileriniz içindeki \"Şehir\" bilgisini güncelleyiniz ! <br />" +
        //                                                 "Lütfen Üyelik Bilgileriniz içindeki \"İlçe\" bilgisini güncelleyiniz!", "Please update your \"City\" in your \"Personal Information\" page" +
        //                                                                                                                          "Please update your \"District\" in your \"Personal Information\" page", lang)
        //            });
        //        }
        //        var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);




        //        // 
        //        int totalPrice = 1;
        //        if (securePayment)
        //        {
        //            var pr = new PaymentRequest()
        //            {
        //                Price = totalPrice,
        //                CreatedDate = DateTime.Now,
        //                Type = Enums.PaymentType.IlanEkleme,
        //                UserID = GetLoginID(),
        //                AdvertID = id,
        //                SecurePayment = true,
        //                IpAddress = GetIpAddress(),
        //                Description = user.Name + " adlı kullanıcı ilan ekleme a talebi oluşturdu. ",
        //                Status = Enums.PaymentRequestStatus.Bekleniyor
        //            };
        //            var prInsert = prService.Insert(pr);
        //            if (!prInsert)
        //            {
        //                return Json(new
        //                {
        //                    isSuccess = false,
        //                    message = new Localization().Get(
        //                        "Doping ödeme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
        //                        "Your doping payment record could not be created. Please contact us.", lang)
        //                });
        //            }

        //            var payment = buildPaymentForNewAdvert(user, userAddress, GetIpAddress(), cvc, cardName, month, year,
        //                cardNumber, pr.ID, totalPrice, "İlan Ekleme Girişi", "DoNewAdvertPaytment/Callback");

        //            if (payment.Status == "success")
        //            {
        //                return Json(new { isSuccess = true, html = payment.HtmlContent });
        //            }
        //            else
        //            {
        //                var paymentRequest = prService.Delete(pr);
        //                return Json(new { isSuccess = false, message = payment.ErrorMessage });
        //            }
        //        }
        //        else
        //        {

        //            var pr = new PaymentRequest()
        //            {
        //                Price = totalPrice,
        //                CreatedDate = DateTime.Now,
        //                Type = Enums.PaymentType.IlanEkleme,
        //                UserID = GetLoginID(),
        //                AdvertID = id,
        //                SecurePayment = false,
        //                IpAddress = GetIpAddress(),
        //                Description = user.Name + " adlı kullanıcı ilan ekleme a talebi oluşturdu. ",
        //                Status = Enums.PaymentRequestStatus.Bekleniyor
        //            };
        //            var prInsert = prService.Insert(pr);
        //            if (!prInsert)
        //            {
        //                return Json(new
        //                {
        //                    isSuccess = false,
        //                    message = new Localization().Get(
        //                        "Ilan ekleme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
        //                        "Your New Advert Record could not be created. Please contact us.", lang)
        //                });
        //            }

        //        }




        //        var route = new Localization().Get("/IlanEkle/IlaniOneCikar/", "/AddListing/FeaturedAd/", lang) + advert.ID;
        //        return Json(new { isSuccess = true, url = route });
        //    }
        //    return null;
        //}
        #endregion

        #region STEP_4_IlaniOneCikar
        [Authorize]
        [Route("IlanEkle/IlaniOneCikar/{id}")]
        [Route("AddListing/FeaturedAd/{id}")]
        public IActionResult FeaturedAd(int id) //step4.
        {
            var lang = GetLang();


            using (var publicService = new PublicService())
            using (var advertService = new AdvertService())
            using (var setService = new SystemSettingService())         
            using (var textService = new TextService())
            {
                var advert = advertService.GetAdvert(id);
                var model = new FeaturedAdStep3ViewModel()
                {
                    Advert = advert,
                    DopingTypes = publicService.GetDopingTypes(),
                    AdvertDopings = advertService.GetadvertDopings(id),

                };
                var settings = setService.GetSystemSettings();

                if (HttpContext.Session.GetString("IsPaymentStepActive") != null && Convert.ToBoolean(HttpContext.Session.GetString("IsPaymentStepActive")) == true)
                {
                    var advertPrice =Convert.ToInt32(settings.Where(x=>x.Name == Enums.SystemSettingName.AdvertPublishPrice).FirstOrDefault().Value);                  
                    ViewBag.AdvertPrice = advertPrice;
                }

                return View(model);
            }
        }
        #region // Doping Ekleme Yeni
        /*
        [Authorize]
        [HttpPost]
        [Route("AddListing/FeaturedAd/{id}")]
        public JsonResult FeaturedAdNew(int id, int[] dopingTypeIds, bool securePayment, string cardName, string cardNumber, string month, string year, string cvc) //step4 save func.
        {
            var lang = GetLang();
            bool paymentStepIsActive = HttpContext.Session.GetString("PaymentStepIsActive") != null && Convert.ToBoolean(HttpContext.Session.GetString("PaymentStepIsActive")) == true;
            using (var userService = new UserService())
            using (var prService = new PaymentRequestService())
            using (var publicService = new PublicService())
            using (var service = new AdvertService())
            using (var sytemSettingService = new SystemSettingService())
            {
                var advert = service.GetMyAdvert(id, GetLoginID());
                //AdvertPublishPrice
                #region Validations

                if (advert == null)
                    return Json(new { isSuccess = false, url = "/" });
                if (advert.UserID != GetLoginID())
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Erişim hatası.", "Access error.", lang)
                    });
                if (securePayment && (string.IsNullOrEmpty(cardName) || string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(month) ||
                    string.IsNullOrEmpty(year) || string.IsNullOrEmpty(cvc)))
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get(
                            "Kredi kartı bilgilerinizi doldurunuz.",
                            "Please fill in your credit card information.", lang)
                    });
                }

                if (dopingTypeIds == null || dopingTypeIds.Length == 0)
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Doping bulunamadı.", "Doping not found.", lang)
                    });

                #endregion
                var user = userService.Get(GetLoginID());
                if (user.District == null || user.City == null)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Lütfen Üyelik Bilgileriniz içindeki \"Şehir\" bilgisini güncelleyiniz ! <br />" +
                                                         "Lütfen Üyelik Bilgileriniz içindeki \"İlçe\" bilgisini güncelleyiniz!", "Please update your \"City\" in your \"Personal Information\" page" +
                                                                                                                                  "Please update your \"District\" in your \"Personal Information\" page", lang)
                    });
                }
                var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);


                var dopings = publicService.GetDopingTypes();
                dopings = dopings.Where(x => dopingTypeIds.Contains(x.ID)).ToList();
                var totalDopingsPrice = 0;
                var dopingNames = "";
                var advertDopings = new List<AdvertDoping>();
                if (dopings.Count != 0 || paymentStepIsActive == true)
                {
                    foreach (var dopingType in dopings)
                    {

                        totalDopingsPrice += dopingType.Price;
                        dopingNames += dopingType.Name + " " + dopingType.Day + " Gün - " + dopingType.Price + " TL |";
                        var advertDoping = new AdvertDoping
                        {
                            AdvertID = id,
                            TypeID = dopingType.ID,
                            Price = dopingType.Price,
                            IsActive = false,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddDays(dopingType.Day),
                            IsPendingApproval = true
                        };
                        var insert = service.InsertAdvertDoping(advertDoping);
                        advertDopings.Add(advertDoping);
                        if (!insert)
                        {
                            return Json(new { isSuccess = false, message = new Localization().Get("Doping girişi yapılamadı. ", "Doping insert failed.", lang) + " #" + dopingType.Name });
                        }
                    }

                    if (securePayment)
                    {
                        int prID = 0;

                        #region İlan Ücreti Ödemesi

                        int advertPublishPrice = 0; // Yeni ilan yayınlamak için ödenmesi gereken tutar.
                        var settings = sytemSettingService.GetSystemSettings();
                        advertPublishPrice = int.Parse(settings.Single(s => s.Name == Enums.SystemSettingName.AdvertPublishPrice).Value);
                        var prForAdvert = new PaymentRequest()
                        {
                            Price = advertPublishPrice,
                            CreatedDate = DateTime.Now,
                            Type = Enums.PaymentType.IlanEkleme,
                            UserID = GetLoginID(),
                            AdvertID = id,
                            SecurePayment = false,
                            IpAddress = GetIpAddress(),
                            Description = user.Name + " adlı kullanıcı ilan Yeni İlan satın alma talebi oluşturdu. ",
                            Status = Enums.PaymentRequestStatus.Bekleniyor
                        };
                        
                        var advertPaymentCompleted = prService.Insert(prForAdvert);
                        if (!advertPaymentCompleted)
                        {
                            return Json(new
                            {
                                isSuccess = false,
                                message = new Localization().Get(
                                    "İlan ödeme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
                                    "Your Advert payment record could not be created. Please contact us.", lang)
                            });
                        }

                        #endregion
                        #region  Doping Ödemesi
                        if(totalDopingsPrice > 0)
                        {
                            var prForDopping = new PaymentRequest()
                            {
                                Price = totalDopingsPrice,
                                CreatedDate = DateTime.Now,
                                Type = Enums.PaymentType.IlanDoping,
                                UserID = GetLoginID(),
                                AdvertID = id,
                                SecurePayment = true,
                                IpAddress = GetIpAddress(),
                                Description = user.Name + " adlı kullanıcı ilan dopingi satın alma talebi oluşturdu. " + dopingNames,
                                Status = Enums.PaymentRequestStatus.Bekleniyor
                            };
                            var prInsert = prService.Insert(prForDopping);
                            if (!prInsert)
                            {
                                return Json(new
                                {
                                    isSuccess = false,
                                    message = new Localization().Get(
                                        "Doping ödeme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
                                        "Your doping payment record could not be created. Please contact us.", lang)
                                });
                            }
                            UpdateDopingPaymentId(advertDopings, prForDopping.ID);
                            if (prForDopping.ID > 0) prID = prForDopping.ID;
                        }

                        #endregion
                       
                        if (prForAdvert.ID > 0) prID = prForAdvert.ID;

                        var paymentTotal = totalDopingsPrice + advertPublishPrice;
                        string paymentName = advertPublishPrice > 0 ? " Yeni İlan Yaınlama Ödemesi  : " + advertPublishPrice + " TL" :"";
                        if (totalDopingsPrice > 0)
                            paymentName += " İlan Doping Alımı : " + totalDopingsPrice + " TL.";
                      
                        var payment = buildPaymentForNewAdvert(user, userAddress, GetIpAddress(), cvc, cardName, month, year,
                            cardNumber, prID, paymentTotal, paymentName, "Doping/Callback");

                        if (payment.Status == "success")
                        {
                            return Json(new { isSuccess = true, html = payment.HtmlContent });
                        }
                        else
                        {
                            var paymentRequest = prService.Delete(pr);
                            return Json(new { isSuccess = false, message = payment.ErrorMessage });
                        }
                    }
                    else
                    {
                        #region İlan Ücreti Ödemesi
                        double advertPublishPrice = 0; // Yeni ilan yayınlamak için ödenmesi gereken tutar.
                        var settings = sytemSettingService.GetSystemSettings();
                        advertPublishPrice = int.Parse(settings.Single(s => s.Name == Enums.SystemSettingName.AdvertPublishPrice).Value);
                        var prForAdvert = new PaymentRequest()
                        {
                            Price = advertPublishPrice,
                            CreatedDate = DateTime.Now,
                            Type = Enums.PaymentType.IlanEkleme,
                            UserID = GetLoginID(),
                            AdvertID = id,
                            SecurePayment = false,
                            IpAddress = GetIpAddress(),
                            Description = user.Name + " adlı kullanıcı ilan Yeni İlan satın alma talebi oluşturdu. ",
                            Status = Enums.PaymentRequestStatus.Bekleniyor
                        };
                        var advertPaymentCompleted = prService.Insert(prForAdvert);
                        if (!advertPaymentCompleted)
                        {
                            return Json(new
                            {
                                isSuccess = false,
                                message = new Localization().Get(
                                    "İlan ödeme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
                                    "Your Advert payment record could not be created. Please contact us.", lang)
                            });
                        }
                        #endregion

                        #region Doping Ödemesi
                        var pr = new PaymentRequest()
                        {
                            Price = totalDopingsPrice,
                            CreatedDate = DateTime.Now,
                            Type = Enums.PaymentType.IlanDoping,
                            UserID = GetLoginID(),
                            AdvertID = id,
                            SecurePayment = false,
                            IpAddress = GetIpAddress(),
                            Description = user.Name + " adlı kullanıcı ilan dopingi satın alma talebi oluşturdu. " + dopingNames,
                            Status = Enums.PaymentRequestStatus.Bekleniyor
                        };
                        var prInsert = prService.Insert(pr);
                        if (!prInsert)
                        {
                            return Json(new
                            {
                                isSuccess = false,
                                message = new Localization().Get(
                                    "Doping ödeme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
                                    "Your doping payment record could not be created. Please contact us.", lang)
                            });
                        }
                        UpdateDopingPaymentId(advertDopings, pr.ID);
                        #endregion

                    }

                }


                var route = new Localization().Get("/IlanEkle/Sonuc/", "/AddListing/Summary/", lang) + advert.ID;
                return Json(new { isSuccess = true, url = route });
            }
        }
        */
        #endregion
        [Authorize]
        [HttpPost]
        [Route("AddListing/FeaturedAd/{id}")]
        public JsonResult FeaturedAd(int id, int[] dopingTypeIds, bool securePayment,
            string cardName, string cardNumber, string month, string year, string cvc) //step4 save func.
        {
            var lang = GetLang();

            using (var userService = new UserService())
            using (var prService = new PaymentRequestService())
            using (var publicService = new PublicService())
            using (var service = new AdvertService())
            {
                var advert = service.GetMyAdvert(id, GetLoginID());

                #region Validations

                if (advert == null)
                    return Json(new { isSuccess = false, url = "/" });
                if (advert.UserID != GetLoginID())
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Erişim hatası.", "Access error.", lang)
                    });
                if (securePayment && (string.IsNullOrEmpty(cardName) || string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(month) ||
                    string.IsNullOrEmpty(year) || string.IsNullOrEmpty(cvc)))
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get(
                            "Kredi kartı bilgilerinizi doldurunuz.",
                            "Please fill in your credit card information.", lang)
                    });
                }

                if (dopingTypeIds == null || dopingTypeIds.Length == 0)
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Doping bulunamadı.", "Doping not found.", lang)
                    });

                #endregion
                var user = userService.Get(GetLoginID());
                if (user.District == null || user.City == null)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Lütfen Üyelik Bilgileriniz içindeki \"Şehir\" bilgisini güncelleyiniz ! <br />" +
                                                         "Lütfen Üyelik Bilgileriniz içindeki \"İlçe\" bilgisini güncelleyiniz!", "Please update your \"City\" in your \"Personal Information\" page" +
                                                                                                                                  "Please update your \"District\" in your \"Personal Information\" page", lang)
                    });
                }
                var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);


                var dopings = publicService.GetDopingTypes();
                dopings = dopings.Where(x => dopingTypeIds.Contains(x.ID)).ToList();
                var totalPrice = 0;
                var dopingNames = "";
                var advertDopings = new List<AdvertDoping>();
                if (dopings.Count != 0)
                {
                    foreach (var dopingType in dopings)
                    {

                        totalPrice += dopingType.Price;
                        dopingNames += dopingType.Name + " " + dopingType.Day + " Gün - " + dopingType.Price + " TL |";
                        var advertDoping = new AdvertDoping
                        {
                            AdvertID = id,
                            TypeID = dopingType.ID,
                            Price = dopingType.Price,
                            IsActive = false,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddDays(dopingType.Day),
                            IsPendingApproval = true
                        };
                        var insert = service.InsertAdvertDoping(advertDoping);
                        advertDopings.Add(advertDoping);
                        if (!insert)
                        {
                            return Json(new { isSuccess = false, message = new Localization().Get("Doping girişi yapılamadı. ", "Doping insert failed.", lang) + " #" + dopingType.Name });
                        }
                    }

                    if (securePayment)
                    {
                        //if (GetLoginID() == 2)
                        //    totalPrice = 1;
                        var pr = new PaymentRequest()
                        {
                            Price = totalPrice,
                            CreatedDate = DateTime.Now,
                            Type = Enums.PaymentType.IlanDoping,
                            UserID = GetLoginID(),
                            AdvertID = id,
                            SecurePayment = true,
                            IpAddress = GetIpAddress(),
                            Description = user.Name + " adlı kullanıcı ilan dopingi satın alma talebi oluşturdu. " + dopingNames,
                            Status = Enums.PaymentRequestStatus.Bekleniyor
                        };
                        var prInsert = prService.Insert(pr);
                        if (!prInsert)
                        {
                            return Json(new
                            {
                                isSuccess = false,
                                message = new Localization().Get(
                                    "Doping ödeme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
                                    "Your doping payment record could not be created. Please contact us.", lang)
                            });
                        }
                        UpdateDopingPaymentId(advertDopings, pr.ID);

                        var payment = BuildPayment(user, userAddress, GetIpAddress(), cvc, cardName, month, year,
                            cardNumber, pr.ID, totalPrice, "İlan Doping Alımı", "Doping/Callback");

                        if (payment.Status == "success")
                        {
                            return Json(new { isSuccess = true, html = payment.HtmlContent });
                        }
                        else
                        {
                            var paymentRequest = prService.Delete(pr);
                            return Json(new { isSuccess = false, message = payment.ErrorMessage });
                        }
                    }
                    else
                    {
                        var pr = new PaymentRequest()
                        {
                            Price = totalPrice,
                            CreatedDate = DateTime.Now,
                            Type = Enums.PaymentType.IlanDoping,
                            UserID = GetLoginID(),
                            AdvertID = id,
                            SecurePayment = false,
                            IpAddress = GetIpAddress(),
                            Description = user.Name + " adlı kullanıcı ilan dopingi satın alma talebi oluşturdu. " + dopingNames,
                            Status = Enums.PaymentRequestStatus.Bekleniyor
                        };
                        var prInsert = prService.Insert(pr);
                        if (!prInsert)
                        {
                            return Json(new
                            {
                                isSuccess = false,
                                message = new Localization().Get(
                                    "Doping ödeme kaydınız oluşturulamadı. Lütfen iletişim bölümünden ulaşınız.",
                                    "Your doping payment record could not be created. Please contact us.", lang)
                            });
                        }
                        UpdateDopingPaymentId(advertDopings, pr.ID);
                    }

                }


                var route = new Localization().Get("/IlanEkle/Sonuc/", "/AddListing/Summary/", lang) + advert.ID;
                return Json(new { isSuccess = true, url = route });
            }
        }

        private void UpdateDopingPaymentId(List<AdvertDoping> advertDopings, int paymentId)
        {
            using (var service = new AdvertService())
            {
                foreach (var advertDoping in advertDopings)
                {
                    service.UpdateDopingPaymentId(advertDoping.ID, paymentId);
                }
            }
        }

        #endregion

        #region STEP_5_Sonuc

        [Authorize]
        [Route("IlanEkle/Sonuc/{id}")]
        [Route("AddListing/Summary/{id}")]
        public IActionResult Summary(int id)
        {
            using (var service = new AdvertService())
            {
                var advert = service.GetMyAdvert(id, GetLoginID());
                service.SetDraft(id, false);
                return View(advert);
            }
        }



        #endregion


        #region İlanDetay

        [Route("Ilan/{categorySlug}/{subcategorySlug}/{title}/{id}")]
        [Route("Advert/{categorySlug}/{subcategorySlug}/{title}/{id}")]
        [Route("Ilan/{categorySlug}/{title}/{id}")]
        [Route("Advert/{categorySlug}/{title}/{id}")]
        [Route("Ilan/{title}/{id}")]
        [Route("Advert/{title}/{id}")]
        public IActionResult Details(int id)
        {
            var lang = GetLang();
            using (var catService = new AdvertCategoryService())
            using (var publicService = new PublicService())
            using (var userService = new UserService())
            using (var service = new AdvertService())
            {
                var advert = service.Get(id);
                if (advert == null)
                    return Redirect("/");
                var advertCats = catService.GetAll();
                advert.User = userService.Get(advert.UserID);
                var category = catService.Get(advert.CategoryID, lang);
                advert.CategoryNameTr = category.Name;
                var model = new AdvertDetailsViewModel
                {
                    Advert = advert,
                    AdvertCategories = advertCats,
                    SimilarAdverts = service.GetSimilarAdverts(advert.CategoryID, 4, lang),
                    CargoArea = publicService.GetCargoAreaById(advert.CargoAreaID)
                };
                if (lang == (int)Enums.Languages.tr)
                {
                    ViewBag.TranslateUrl = "/Advert/" + Common.Localization.Slug(advert.Title) + "/" + advert.ID;
                }
                else
                {
                    ViewBag.TranslateUrl = "/Ilan/" + Common.Localization.Slug(advert.Title) + "/" + advert.ID;
                }
                service.UpdateViewCount(id);
                return View(model);
            }
        }

        #endregion

        #region Doping Ödeme

        private ThreedsInitialize BuildPayment(User user, UserAddress userAddress, string ip, string cvc, string fullname,
            string month, string year, string number, int paymentRequestId, int price, string paymentName, string callback)
        {
            Address address;
            string addressLine;
            if (userAddress == null)
            {
                addressLine = user?.District?.Name + " " + user?.City?.Name;
                address = new Address
                {
                    City = user?.City?.Name ?? "Istanbul",
                    Country = user?.Country?.Name ?? "Turkey",
                    ContactName = user?.Name,
                    Description = addressLine
                };
            }
            else
            {
                addressLine = userAddress.Address;
                address = new Address
                {
                    City = userAddress.City?.Name ?? userAddress.CityText,
                    Country = userAddress.Country?.Name ?? "Turkey",
                    ContactName = user.Name,
                    Description = userAddress.Address
                };
            }

            user.Name = user.Name.Trim();
            var name = user.Name;
            var surName = "Audiophile.org";
            if (user.Name.Contains(" "))
            {
                name = user.Name.Substring(0, user.Name.LastIndexOf(' ') + 1);
                surName = user.Name.Substring(user.Name.LastIndexOf(' ') + 1);
            }

            var buyer = new Buyer
            {
                Id = user.ID.ToString(),
                Name = name,
                Surname = surName,
                City = address.City,
                Country = address.Country,
                Email = user.Email,
                GsmNumber = string.IsNullOrEmpty(user.MobilePhone) ? null : user.MobilePhone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""),
                // todo  : ? 
                IdentityNumber = "40459550113",
                Ip = ip,
                RegistrationAddress = addressLine
            };
            var card = new PaymentCard
            {
                Cvc = cvc,
                CardHolderName = fullname,
                ExpireMonth = month,
                ExpireYear = year,
                CardNumber = number,
                RegisterCard = 0
            };

            return IyzicoService.PayBanner(paymentRequestId, price, card,
                buyer, address, address, paymentRequestId,
                paymentName, callback);
        }


        [Route("Doping/Callback")]
        public IActionResult Callback(Iyzico3dCallback callback)
        {
            var lang = GetLang();
            var profileUrl = Constants.GetURL(Enums.Routing.ProfilSayfam, lang);

            if (callback == null || callback.ConversationId == 0)
            {
                Notification = new UiMessage(NotyType.error,
                    new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                    10000);
                return Redirect(profileUrl);
            }

            using (var prService = new PaymentRequestService())
            {
                var prId = Convert.ToInt32(callback.ConversationId);
                var pr = prService.Get(prId);
                if (pr == null)
                {
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Ödeme kaydı bulunamadı. Lütfen site yönetimi ile iletişime geçin."
                            , "Payment record not found.. Please contact the site administration.", lang),
                        10000);
                    return Redirect(profileUrl);
                }
                if (string.IsNullOrEmpty(callback.PaymentId) || callback.PaymentId == "0")
                {
                    prService.Delete(pr);
                    Notification = new UiMessage(NotyType.error,
                        new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                        10000);
                    return Redirect(profileUrl);
                }
                var route = new Localization().Get("/IlanEkle/Sonuc/", "/AddListing/Summary/", lang) + pr.AdvertID;
                var request = new CreateThreedsPaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = callback.ConversationId.ToString(),
                    PaymentId = callback.PaymentId,
                    ConversationData = callback.ConversationData
                };
                var threedsPayment = ThreedsPayment.Create(request, IyzicoService.GetOptions());
                if (threedsPayment.Status == "success")
                {
                    pr.IsSuccess = true;
                    pr.Status = Enums.PaymentRequestStatus.OnlineOdemeYapildi;
                    pr.ResponseDate = DateTime.Now;
                    pr.PaymentId = threedsPayment.PaymentId;
                    pr.PaymentTransactionID = threedsPayment.PaymentItems.First().PaymentTransactionId;
                    var update = prService.Update(pr);
                    if (!update)
                    {
                        Notification = new UiMessage(NotyType.success, new Localization().Get(
                                "Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin."
                                , "Payment completed but could not be saved in the system. Please contact the site administration.", lang),
                            10000);
                        return Redirect(profileUrl);
                    }
                    else
                    {
                        //dopingService.ActivateAllPendingDopings(pr.AdvertID);
                        //dopingler otomatik aktifleşmeyecek, admin panelinden aktifleştirilecek.
                        Notification = new UiMessage(NotyType.success, new Localization().Get(
                                "Doping alım işleminiz tamamlandı."
                                , "Your doping purchase process is completed.", lang),
                            10000);
                        return Redirect(route);
                    }
                }
                else
                {
                    prService.Delete(pr);
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Doping alım işlemi başarısız. "
                            , "Doping purchase process is failed. ", lang) + threedsPayment.ErrorMessage,
                        10000);
                    return Redirect(route);
                }
            }
        }


        #endregion


        #region İlan Ekleme için Ödeme

        private ThreedsInitialize buildPaymentForNewAdvert(User user, UserAddress userAddress, string ip, string cvc, string fullname,
           string month, string year, string number, int paymentRequestId, int price, string paymentName, string callback)
        {
            Address address;
            string addressLine;
            if (userAddress == null)
            {
                addressLine = user?.District?.Name + " " + user?.City?.Name;
                address = new Address
                {
                    City = user?.City?.Name ?? "Istanbul",
                    Country = user?.Country?.Name ?? "Turkey",
                    ContactName = user?.Name,
                    Description = addressLine
                };
            }
            else
            {
                addressLine = userAddress.Address;
                address = new Address
                {
                    City = userAddress.City?.Name ?? userAddress.CityText,
                    Country = userAddress.Country?.Name ?? "Turkey",
                    ContactName = user.Name,
                    Description = userAddress.Address
                };
            }

            user.Name = user.Name.Trim();
            var name = user.Name;
            var surName = "Audiophile.org";
            if (user.Name.Contains(" "))
            {
                name = user.Name.Substring(0, user.Name.LastIndexOf(' ') + 1);
                surName = user.Name.Substring(user.Name.LastIndexOf(' ') + 1);
            }

            var buyer = new Buyer
            {
                Id = user.ID.ToString(),
                Name = name,
                Surname = surName,
                City = address.City,
                Country = address.Country,
                Email = user.Email,
                GsmNumber = string.IsNullOrEmpty(user.MobilePhone) ? null : user.MobilePhone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""),
                // todo  : ? 
                IdentityNumber = "40459550113",
                Ip = ip,
                RegistrationAddress = addressLine
            };
            var card = new PaymentCard
            {
                Cvc = cvc,
                CardHolderName = fullname,
                ExpireMonth = month,
                ExpireYear = year,
                CardNumber = number,
                RegisterCard = 0
            };

            return IyzicoService.PayNewAdbvert(paymentRequestId, price, card,
                buyer, address, address, paymentRequestId,
                paymentName, callback);
        }

        [Route("DoNewAdvertPaytment/Callback")]
        public IActionResult DoNewAdvertPaytment(Iyzico3dCallback callback)
        {
            var lang = GetLang();
            var profileUrl = Constants.GetURL(Enums.Routing.ProfilSayfam, lang);

            if (callback == null || callback.ConversationId == 0)
            {
                Notification = new UiMessage(NotyType.error,
                    new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                    10000);
                return Redirect(profileUrl);
            }

            using (var prService = new PaymentRequestService())
            {
                var prId = Convert.ToInt32(callback.ConversationId);
                var pr = prService.Get(prId);
                if (pr == null)
                {
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Ödeme kaydı bulunamadı. Lütfen site yönetimi ile iletişime geçin."
                            , "Payment record not found.. Please contact the site administration.", lang),
                        10000);
                    return Redirect(profileUrl);
                }
                if (string.IsNullOrEmpty(callback.PaymentId) || callback.PaymentId == "0")
                {
                    prService.Delete(pr);
                    Notification = new UiMessage(NotyType.error,
                        new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                        10000);
                    return Redirect(profileUrl);
                }
                var route = new Localization().Get("/IlanEkle/Sonuc/", "/AddListing/Summary/", lang) + pr.AdvertID;
                var request = new CreateThreedsPaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = callback.ConversationId.ToString(),
                    PaymentId = callback.PaymentId,
                    ConversationData = callback.ConversationData
                };
                var threedsPayment = ThreedsPayment.Create(request, IyzicoService.GetOptions());
                if (threedsPayment.Status == "success")
                {
                    pr.IsSuccess = true;
                    pr.Status = Enums.PaymentRequestStatus.OnlineOdemeYapildi;
                    pr.ResponseDate = DateTime.Now;
                    pr.PaymentId = threedsPayment.PaymentId;
                    pr.PaymentTransactionID = threedsPayment.PaymentItems.First().PaymentTransactionId;
                    var update = prService.Update(pr);
                    if (!update)
                    {
                        Notification = new UiMessage(NotyType.success, new Localization().Get(
                                "Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin."
                                , "Payment completed but could not be saved in the system. Please contact the site administration.", lang),
                            10000);
                        return Redirect(profileUrl);
                    }
                    else
                    {

                        //dopingService.ActivateAllPendingDopings(pr.AdvertID);
                        //dopingler otomatik aktifleşmeyecek, admin panelinden aktifleştirilecek.
                        Notification = new UiMessage(NotyType.success, new Localization().Get(
                             "İlan alım işleminiz tamamlandı."
                             , "Your Advert purchase process is completed.", lang),
                         10000);
                        return Redirect(route);
                    }
                }
                else
                {
                    prService.Delete(pr);
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "İlan alım işlemi başarısız. "
                            , "Advert purchase process is failed. ", lang) + threedsPayment.ErrorMessage,
                        10000);
                    return Redirect(route);
                }
            }
        }
        #endregion
        [Route("GetInstallmentPrices")]
        public PartialViewResult GetInstallmentPrices(double price)
        {
            var result = IyzicoService.GetInstallmentInfoAllBanks(price);
            if (result.IsSuccess == false)
            {
                return null;
            }
            using (var service = new SystemSettingService())
            {
                var settings = service.GetSystemSettings();
                var audioPhileCommission = double.Parse(settings
                    .Single(x => x.Name == Enums.SystemSettingName.AudiophileKomisyonuYuzde).Value);
                var iyzicoCommission = double.Parse(settings
                    .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuYuzde).Value);
                var iyzicoCommissionTL = double.Parse(settings
                    .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuTL).Value); //iyzico sabit TL komisyon

                var list = new List<InstallmentPrice>();
                foreach (var detail in result.Data.InstallmentDetails)
                {
                    var card = new InstallmentPrice()
                    {
                        BankCode = detail.BankCode.ToString(),
                        BankName = detail.BankName,
                        CardName = detail.CardFamilyName,
                        Details = new List<InstallmentPriceDetail>()
                    };
                    foreach (var installmentPrice in detail.InstallmentPrices)
                    {
                        //if (installmentPrice.InstallmentNumber == 1)
                        //    continue;

                        var p = double.Parse(installmentPrice.TotalPrice.Replace(".", ","));
                        var bankCommission = p - price; //taksitler için alınan banka komisyonu
                        var apComission = (price * audioPhileCommission) / 100; //audiophile komisyonu
                        var iyzCommission = 0.0;
                        if (installmentPrice.InstallmentNumber == 1)
                        { //taksitli işlemlerde taksit farkına iyzico komisyonu dahil geliyor
                            iyzCommission = (price * iyzicoCommission) / 100; // iyzico yüzdelik komisyon
                            iyzCommission += iyzicoCommissionTL;
                        }
                        var subMerchantPrice = price - bankCommission - apComission - iyzCommission;

                        card.Details.Add(new InstallmentPriceDetail
                        {
                            InstallmentNumber = installmentPrice.InstallmentNumber,
                            Price = installmentPrice.Price,
                            TotalPrice = price.ToString("0.##"),
                            SubmerchantPrice = subMerchantPrice.ToString("0.##")
                        });
                        installmentPrice.TotalPrice = p.ToString("0.##");
                    }
                    list.Add(card);
                }
                return PartialView("~/Views/Partials/InstallmentPrices.cshtml", list);
            }
        }

        [Route("Ilanlar/Satici/{id}/{name}")]
        [Route("{id:int}/{name}")]
        [Route("Adverts/Seller/{id}/{name}")]
        public IActionResult SellerAds(int id)
        {
            var lang = GetLang();
            using (var adService = new AdvertService())
            using (var userService = new UserService())
            {
                var user = userService.Get(id);
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Satıcı bulunamadı.", "Seller not found.", lang);
                    return Redirect("/");
                }

                var ads = adService.GetUserAdverts(id, GetLoginID(), lang);
                var list = Ads2HomePageItems(ads, lang);
                if (string.IsNullOrEmpty(user.ProfilePicture))
                {
                    user.ProfilePicture = "/Content/img/avatar-no-image.png";
                }
                var model = new SellerViewModel
                {
                    Ads = list,
                    Seller = user
                };
                return View(model);
            }
        }
    }
}