using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.Helpers;
using Audiophile.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Audiophile.Web.Controllers
{
    public class BaseController : Controller
    {
        public bool Production = true;

        public BaseController()
        {

        }
        public int GetLang(bool setTranslate = true)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userLang = User.Claims.Single(x => x.Type == ClaimTypes.Locality)?.Value;
                if (userLang != null)
                {
                    SetLang(userLang);
                    return Constants.GetLang(userLang);
                }
            }

            Localization();
            var lang = Constants.GetLang(HttpContext.Session.GetString("lang"));
            if (setTranslate)
            {
                var x = Request.Path;
                var y = Constants.UrlTranslate(x, lang);
                ViewBag.TranslateUrl = y;
            }
            return lang;
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

        public void Localization()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("lang")))
            {
                HttpContext.Session.SetString("lang", "tr");
            }
        }

        public void SetLang(string lang)
        {
            HttpContext.Session.SetString("lang", lang);
        }

        public int GetLoginID()
        {
            try
            {
                return int.Parse(User.Claims.Single(x => x.Type == ClaimTypes.UserData).Value);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public UiMessage Notification
        {
            get { return TempData["ErrorMessage"] == null ? null : TempData.Get<UiMessage>("Notification"); }
            set { TempData.Put("Notification", new UiMessage() { Type = value.Type, Message = value.Message }); }
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userLang = User.Claims.Single(x => x.Type == ClaimTypes.Locality)?.Value;
                if (userLang != null)
                {
                    SetLang(userLang);
                }
                using (var uService = new UserService())
                using (var service = new NotificationService())
                {
                    var loginId = GetLoginID();
                    ViewBag.Notifications = service.GetNotifications(loginId);
                    var list = uService.GetSales(loginId);
                    if (list != null && list.Any())
                    {
                        var count = list.Count(x => x.Status == Enums.PaymentRequestStatus.OnlineOdemeYapildi ||
                                                    x.Status == Enums.PaymentRequestStatus.KargoyaVerildi ||
                                                    x.Status == Enums.PaymentRequestStatus.Bekleniyor ||
                                                    x.Status == Enums.PaymentRequestStatus.TeslimEdildi);
                        if (count > 0)
                        {
                            ViewBag.PendingCargo = count.ToString();
                        }
                    }
                    var buys = uService.GetBuys(GetLoginID());
                    if (buys != null && buys.Any())
                    {
                        var count = buys.Count(p => p.Status == Enums.PaymentRequestStatus.KargoyaVerildi || 
                                                    p.Status == Enums.PaymentRequestStatus.OnlineOdemeYapildi ||
                                                    p.Status == Enums.PaymentRequestStatus.Bekleniyor ||
                                                    p.Status == Enums.PaymentRequestStatus.TeslimEdildi
                                                    );
                        if (count > 0)
                        {
                            ViewBag.Buys = count.ToString();
                        }
                    }
                    ViewBag.LikesCount = $"({uService.GetMyLikesCount(loginId)})";
                    ViewBag.AdvertsCount = $"({uService.GetAdvertsCount(loginId)})";
                    ViewBag.BlogPostsCount = $"({uService.GetBlogPostsCount(loginId)})";
                    ViewBag.ArticlesCount = $"({uService.GetArticlesCount(loginId)})";
                    ViewBag.BannersCount = $"({uService.GetBannersCount(loginId)})";

                }
            }
            base.OnActionExecuting(filterContext);
        }

        //dil değiştirildiğinde aynı sayfa içerisinde kalıp routingin de dil değiştirmesi için
        //dil değiştirme linklerine diğer dil linkini ekliyorum
        /*
        public void SetOtherUrl(int routing, int lang)
        { 
            if (lang == 1)
                ViewBag.OtherUrl = Constants.GetURL(routing, 2);
            else
                ViewBag.OtherUrl = Constants.GetURL(routing, 1);
        }
        */

        public List<HomePageItem> Ads2HomePageItems(List<Advert> ads, int lang)
        {
            var result = new List<HomePageItem>();
            foreach (var ad in ads)
            {
                result.Add(Ad2HomePageItem(ad, lang));
            }
            return result;
        }

        public HomePageItem Ad2HomePageItem(Advert ad, int lang)
        {
            var result = new HomePageItem
            {
                ID = ad.ID,
                Title = ad.Title,
                ILiked = ad.IsILiked,
                ViewCount = ad.ViewCount,
                Type = Enums.HomePageItemType.Ilan,
                CreatedDate = ad.CreatedDate,
                ImageSource = ad.Thumbnail,
                LikesCount = ad.LikesCount,
                AdvertOrder = ad.AdvertOrder,
                MoneyType = ad.MoneyTypeID,
                Price = ad.Price.ToString(),
                PriceCurrency = Constants.GetMoney(ad.MoneyTypeID),
                OldPrice = ad.NewProductPrice + " " + Constants.GetMoney(ad.MoneyTypeID),
                NewProductPrice = $"{ad.NewProductPrice:N} {Constants.GetMoney(ad.MoneyTypeID)}",
                DecPrice = $"{ad.Price:N} {Constants.GetMoney(ad.MoneyTypeID)}",
                Description = ad.Content,
                Brand = ad.Brand,
                ProductStatus = ad.ProductStatus,
                StockAmount = ad.StockAmount.ToString(),
                CategoryName = lang == 1 ? ad.CategoryNameTr : ad.CategoryNameEn,
                LastUpdateDate = ad.LastUpdateDate ?? ad.CreatedDate,
                UserName = ad.UserName,
                Content = ad.Content,
                Model = ad.Model,
                IsActive = ad.IsActive,
                Url =
                    $"{Constants.GetURL((int)Enums.Routing.Ilan, lang)}/{Common.Localization.Slug(ad.Title)}/{ad.ID}"
            };

            if (ad.LabelDopingModel != null)
            {
                result.LabelDopingType = ad.LabelDopingModel.Name;
            }
            result.YellowDoping = ad.YellowFrameDoping > 0;
            if (ad.UseSecurePayment && !string.IsNullOrEmpty(ad.AvailableInstallments))
            {
                var items = ad.AvailableInstallments.Split(',');
                int[] installments = Array.ConvertAll(items, s => int.Parse(s));
                result.SecurePayment = new Localization().Get("Gitti Bu ile ", "Secure Payment ", lang)
                                       + installments.Max() +
                                       new Localization().Get(" taksit", " Settlem", lang);
            }

            return result;
        }
        public HomePageItem Blog2HomePageItem(BlogPost post, int lang)
        {
            var result = new HomePageItem();
            result.ID = post.ID;
            result.Title = post.Title;
            result.Type = Enums.HomePageItemType.Blog;
            result.LastUpdateDate = post.CreatedDate;
            result.CreatedDate = post.CreatedDate;
            result.ImageSource = post.Thumbnail;
            result.ViewCount = post.ViewCount;
            result.Url = "/blog/" +
                         Constants.GetBlogCategorySlug(post.CategoryID, lang) + "/" +
                         Common.Localization.Slug(post.Title) +
                         "/" + post.ID;
            return result;
        }


        public void LogInsert(string message, Enums.LogType type, int? key1 = null, int? key2 = null, int? key3 = null, int? key4 = null, int? key5 = null)
        {
            using (var logService = new LogServices())
            {
                try
                {
                    Log log = new Log()
                    {
                        Message = message,
                        Type = type,
                        Key1 = key1,
                        Key2 = key2,
                        Key3 = key3,
                        Key4 = key4,
                        Key5 = key5,
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

    }
}