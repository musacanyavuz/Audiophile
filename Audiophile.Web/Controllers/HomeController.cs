﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Audiophile.Common;
using Audiophile.Common.Extensions;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SimpleMvcSitemap;
using WilderMinds.RssSyndication; 
using Item = WilderMinds.RssSyndication.Item;

namespace Audiophile.Web.Controllers
{
    public class HomeController : BaseController
    {
        #region Anasayfa

        public IActionResult Index()
        {
            var lang = GetLang();

            //using (var mailService = new MailingService())
            //{
            //    if(GetLoginID() == 11544 )
            //    mailService.Send("AAA", "ilhan_aksu61@hotmail.com", "ilhan", "Title test");
            //}

            using (var advertCategoryService = new AdvertCategoryService())
            using (var blogService = new BlogPostService())
            using (var textService = new TextService())
            using (var bannerService = new BannerService())
            using (var service = new AdvertService())
            {
                
                var adverts = service.GetHomePageAdverts(lang, GetLoginID());
                //adverts?.Reverse();
                var banners = bannerService.GetBanners();
                var homepageSplitBanners = banners.Where(x => x.TypeID == Enums.BannerType.Banner).OrderBy(b => b.ID).ToList();
                //var homepageBottomAd = banners.SingleOrDefault(x => x.TypeID == Enums.BannerType.Slider);
                var posts = blogService.GetHomePagePosts();

                var model = new HomePageViewModel
                {
                    AdvertCategories = advertCategoryService.GetMasterCategories(GetLang()),
                    Banners = banners,
                    HomepageSplitBanners = homepageSplitBanners,
                    //HomepageBottomAd = homepageBottomAd,
                    Items = MergeAdsBlogs(adverts, posts, lang, true),
                    SeoValues = new SeoKeys
                    {
                        Title = textService.GetText(Enums.Texts.AnasayfaTitle, lang).RemoveHtmlTags(),
                        Description = textService.GetText(Enums.Texts.AnasayfaKapakBaslik, lang).RemoveHtmlTags(),
                        Keywords = textService.GetText(Enums.Texts.AnasayfaKapakBaslik, lang),
                        Separator = textService.GetText(Enums.Texts.AnasayfaKapakBaslik, lang)
                    }
                   
                };
                ViewBag.SeoKeywords = textService.GetText(Enums.Texts.MetaKeyWordsDefaultValue, lang).RemoveHtmlTags();
                ViewBag.SeoDescription = textService.GetText(Enums.Texts.MetaDescriptionDefaultValue, lang).RemoveHtmlTags();



                return View(model);
            }
        }

        /*[Route("{id:int}")]
        public IActionResult Index(int id)
        {   
            var lang = GetLang();
           
            using (var advertCategoryService = new AdvertCategoryService())
            using (var blogService = new BlogPostService())
            using (var bannerService = new BannerService())
            using (var service = new AdvertService())
            { 
                var adverts = service.GetHomePageAdverts(lang, id, GetLoginID(), 0, true);
                var banners = bannerService.GetBanners();
                var homepageSplitAds = banners.Where(x => x.TypeID == Enums.BannerType.Banner).ToList();
                var homepageBottomAd = banners.SingleOrDefault(x => x.TypeID == Enums.BannerType.Slider);
                var posts = blogService.GetHomePagePosts();
                var model = new HomePageViewModel
                {
                    AdvertCategories = advertCategoryService.GetMasterCategories(GetLang()),
                    Banners = banners,
                    HomepageSplitBanners = homepageSplitAds,
                    HomepageBottomAd = homepageBottomAd,
                    Items = MergeAdsBlogs(adverts, posts, lang, false)
                };
                ViewBag.ScrollToBottom=true;
                return View(model);
            }
        }*/

        //        public PartialViewResult GetAdverts(int id)
        //        {
        //            var result = new List<ViewResult>();
        //            var lang = GetLang();
        //            using (var service = new AdvertService())
        //            {
        //                var list = service.GetHomePageAdverts(lang, id, GetLoginID(), 10);
        //                return PartialView("~/Views/Shared/AdvertItem_5ColumnList.cshtml", list);
        //            }
        //        }

        private List<HomePageItem> MergeAdsBlogs(List<Advert> ads, List<BlogPost> posts, int lang, bool take)
        {
            var result = new List<HomePageItem>();

            foreach (var ad in ads)
            {
                result.Add(Ad2HomePageItem(ad, lang));
            }

            foreach (var post in posts)
            {
                result.Add(Blog2HomePageItem(post, lang));
            }

            if (take && result.Count > 57)
            {
                return result.OrderByDescending(x => x.LastUpdateDate).Take(57).ToList();
            }

            ////return result.OrderByDescending(x=>x.CreatedDate).ToList();
            // return result.OrderByDescending(x => x.LastUpdateDate).ToList();
            return result.ToList();
        }
        [Route("rss")]
        [Route("rss.xml")]
        public IActionResult Rss()
        {
            var lang = GetLang();

            using (var blogService = new BlogPostService())
            using (var service = new AdvertService())
            {
                var urlBase = "https://www.audiophile.org";
                var adverts = service.GetAllActiveAdverts(lang, GetLoginID()).Take(1000).ToList();
                var posts = blogService.GetHomePagePosts();
                var model = MergeAdsBlogs(adverts, posts, lang, false);

                var feed = new Feed
                {
                    Title = "Audiophile",
                    Description = "Audiophile Feed",
                    Link = new Uri("https://www.audiophile.org/rss.xml"),
                    Copyright = "(c) Audiophile " + DateTime.Now.Year
                };

                foreach (var item in model)
                {
                   var coll = new System.Collections.Specialized.NameValueCollection();
                   coll.Add("logo", urlBase+ item.ImageSource); 
                    Enclosure img = new Enclosure
                    {
                        Values = coll
                    }; 
                    var _enc = new List<Enclosure>();
                    _enc.Add(img);
                    
                 
                    feed.Items.Add(new Item
                    {

                        Title = item.Title,
                        Body = item.Description,
                        Link = new Uri(urlBase + item.Url),
                        Permalink = new Uri(urlBase + item.Url).AbsoluteUri,
                        PublishDate = item.LastUpdateDate?.ToUniversalTime() ?? item.CreatedDate.ToUniversalTime(),
                        Categories = !string.IsNullOrEmpty(item.CategoryName) ?
                            new List<string> { item.CategoryName } : new List<string> { "Audiphile" },
                        Enclosures = _enc
                    }) ; 
                }
                var srOpt = new SerializeOption { Encoding= Encoding.UTF8 };
                var rss = feed.Serialize(srOpt);
                    
                return Content(rss, "application/rss+xml", Encoding.UTF8);
            }
        }

        [Route("feed")]
        public IActionResult Feed()
        {
            var lang = GetLang();

            using (var blogService = new BlogPostService())
            using (var service = new AdvertService())
            {
                var urlBase = "https://www.audiophile.org";
                var adverts = service.GetAdvertsByTake(lang, GetLoginID(), 5).ToList();
                var posts = blogService.GetHomePagePosts();
                var model = MergeAdsBlogs(adverts, posts, lang, false);


                var feed = new Feed
                {
                    Title = "Audiophile",
                    Description = "Audiophile Feed",
                    Link = new Uri($"{urlBase}/feed"),
                    Copyright = "(c) Audiophile " + DateTime.Now.Year,
                };
                
                foreach (var item in model)
                {
                    var _item = new Item
                    {

                        Title = item.Title,
                        Body = item.Description,
                        Link = new Uri(urlBase + item.Url),
                        Permalink = new Uri(urlBase + item.Url).AbsoluteUri,
                        PublishDate = item.LastUpdateDate?.ToUniversalTime() ?? item.CreatedDate.ToUniversalTime(),
                        Categories = !string.IsNullOrEmpty(item.CategoryName) ?
                            new List<string> { item.CategoryName } : new List<string> { "Audiphile" }
                    };
                   

                    feed.Items.Add(_item);
                }
                var srOpt = new SerializeOption { Encoding = Encoding.UTF8 };
                var rss = feed.Serialize(srOpt);

                return Content(rss, "application/rss+xml", Encoding.UTF8);
            }
        }
        #endregion


        #region Like_Unlike

        [HttpPost]
        [Route("LikeAd")]
        public JsonResult LikeAd(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            if (!User.Identity.IsAuthenticated)
                return Json(new { isSuccess = false, message = t.Get("Bu işlem için giriş yapmanız gerekiyor.", "You need to login for this process.", lang) });

            using (var userService = new UserService())
            using (var service = new AdvertService())
            {
                var ad = service.Get(id);
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("İlana erişilemedi.", "Ad was not reached.", lang) });

                var user = userService.Get(GetLoginID());
                if (user == null)
                    return Json(new { isSuccess = false, message = t.Get("Kullanıcı bilgilerinize erişilemedi.", "Your user information could not be accessed.", lang) });

                var isLiked = service.GetAdvertLike(id, GetLoginID());
                if (isLiked != null)
                    return Json(new { isSuccess = false, message = t.Get("Bu ilanı zaten beğenmişsiniz.", "You like this ad already.", lang) });

                var insert = service.InsertAdvertLike(new AdvertLike
                {
                    AdvertID = id,
                    UserID = GetLoginID(),
                    CreatedDate = DateTime.Now
                });
                if (!insert)
                    return Json(new { isSuccess = false, message = t.Get("İlan beğenilirken beklenmedik bir sorun oluştu.", "An unexpected problem occurred while posting.", lang) });

                return Json(new { isSuccess = true, message = "" });
            }
        }

        [HttpPost]
        [Route("UnlikeAd")]
        public JsonResult UnlikeAd(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            if (!User.Identity.IsAuthenticated)
                return Json(new { isSuccess = false, message = t.Get("Bu işlem için giriş yapmanız gerekiyor.", "You need to login for this process.", lang) });

            using (var userService = new UserService())
            using (var service = new AdvertService())
            {
                var ad = service.Get(id);
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("İlana erişilemedi.", "Ad was not reached.", lang) });

                var user = userService.Get(GetLoginID());
                if (user == null)
                    return Json(new { isSuccess = false, message = t.Get("Kullanıcı bilgilerinize erişilemedi.", "Your user information could not be accessed.", lang) });

                var isLiked = service.GetAdvertLike(id, GetLoginID());
                if (isLiked == null)
                    return Json(new { isSuccess = false, message = t.Get("Bu ilanı zaten beğenmemişsiniz.", "You don't like this ad already.", lang) });

                var delete = service.DeleteAdvertLike(isLiked);
                if (!delete)
                    return Json(new { isSuccess = false, message = t.Get("İlan beğenmekten vazgeçilirken beklenmedik bir sorun oluştu.", "There was an unexpected problem while the advertisement was unliked.", lang) });

                return Json(new { isSuccess = true, message = "" });
            }
        }

        #endregion

        [Authorize]
        [Route("DistanceSalesContract/{id}")]
        public IActionResult DistanceSalesContract(int id)
        {
            using (var userService = new UserService())
            using (var service = new PaymentRequestService())
            {
                var pr = service.GetOrder(id);
                if (pr == null)
                {
                    return Redirect("/");
                }

                var address = userService.GetUserAddresses(pr.SellerID);
                if (address == null || !address.Any()) return View(pr);
                var sellerAddress = address.SingleOrDefault(x => x.IsDefault) ?? address.First();
                ViewBag.SellerAddress = sellerAddress.Address + " " +
                                        (sellerAddress.City?.Name ?? sellerAddress.CityText) + " "
                                        + sellerAddress.District?.Name + " "
                                        + sellerAddress.Country?.Name;
                return View(pr);
            }
        }

        [Route("Arama/{query}")]
        [Route("Search/{query}")]
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
                return RedirectToAction("Index");

            var lang = GetLang(false);

            using (var advertCategoryService = new AdvertCategoryService())
            using (var blogService = new BlogPostService())
            using (var adService = new AdvertService())
            {
                var ads = adService.Search(query, GetLoginID(), lang);
                var posts = blogService.Search(query);

                var model = new HomePageViewModel()
                {
                    Items = MergeAdsBlogs(ads, posts, lang, false),
                    AdvertCategories = advertCategoryService.GetMasterCategories(GetLang()),
                };

                return View(model);
            }
        }


        [Route("EN")]
        [Route("English")]
        public IActionResult English()
        {
            HttpContext.Session.SetString("lang", Enums.Languages.en.ToString());
            
            return RedirectToAction("Index");
        }

        [Route("TR")]
        [Route("Turkce")]
        public IActionResult Turkce()
        {
            HttpContext.Session.SetString("lang", Enums.Languages.tr.ToString());
            return RedirectToAction("Index");
        }

        [Route("Bildirim")]
        [Route("Notification")]
        public IActionResult Notification()
        {
            return View();
        }

        [Route("BannerClick")]
        [HttpPost]
        public void BannerClick(int id)
        {
            using (var service = new BannerService())
            {
                if (id > 0)
                    service.IncraseClickCount(id);
            }
        }

        public JsonResult AddNewsletter(string email)
        {
            var lang = GetLang(false);
            var t = new Localization();
            if (string.IsNullOrEmpty(email))
                return Json(new { isSuccess = false, message = t.Get("Geçersiz bir giriş yaptınız.", "Invalid email.", lang) });
            using (var service = new NewsletterService())
            {
                var insert = service.Insert(new NewsletterSubscriber { Email = email, CreatedDate = DateTime.Now });
                if (!insert)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message =
                    t.Get("Kayıt işlemi başarısız oldu. İletişim sayfasından bize ulaşabilirsiniz.",
                    "Registration failed. You can contact us from the contact page.", lang)
                    });
                }
                return Json(new { isSuccess = true, message = t.Get("E-bülten kaydınız oluşturuldu.", "Your newsletter registration has been created.", lang) });
            }
        }

        [Route("404")]
        public IActionResult Error()
        {
            return View();
        }

        [Route("sitemap.xml")]
        public IActionResult Sitemap()
        {
            var nodes = new List<SitemapNode>
            {
                new SitemapNode(Url.Action("Index","Home")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Help","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("AboutUs","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Videos","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("TermsOfUse","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("SecurePayment","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Prices","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
                new SitemapNode(Url.Action("Contact","Support")){ Priority = 0.5M, ChangeFrequency = ChangeFrequency.Monthly},
            };
            using (var catService = new AdvertCategoryService())
            using (var blogService = new BlogPostService())
            using (var service = new AdvertService())
            {
                var urlBase = "https://www.audiophile.org";
                var lang = GetLang();
                var adverts = service.GetAllActiveAdverts(lang, GetLoginID());
                var posts = blogService.GetHomePagePosts();
                var models = MergeAdsBlogs(adverts, posts, lang, false);
                var categories = catService.GetAll(lang);

                foreach (var cat in categories)
                {
                    nodes.Add(new SitemapNode(urlBase + "/Kategori/" + cat.Slug)
                    {
                        Priority = 0.5M,
                        ChangeFrequency = ChangeFrequency.Monthly,
                    });
                }
                foreach (var item in models)
                {
                    var lta = item.LastUpdateDate ?? item.CreatedDate;
                    var w3CDateTime = new DateTime(lta.Year, lta.Month, lta.Day, lta.Hour, lta.Minute, lta.Second, DateTimeKind.Local);
                    nodes.Add(new SitemapNode(urlBase + item.Url)
                    {
                        LastModificationDate = w3CDateTime,
                        Priority = 0.5M,
                        ChangeFrequency = ChangeFrequency.Weekly,
                    });
                }
            }
            var sitemap = new SitemapModel(nodes);

            return new SitemapProvider().CreateSitemap(sitemap);
        }
    }
}