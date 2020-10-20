using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;

namespace Audiophile.Web.Controllers
{
    public class CategoryController : BaseController
    {
        // GET

        [Route("/Kategori/{masterSlug?}/{childSlug?}")]
        [Route("/Category/{masterSlug?}/{childSlug?}")]
        public IActionResult Index(string masterSlug, string childSlug)
        {
            using (var textService = new TextService())
            using (var advertService = new AdvertService())
            using (var advertCategoryService = new AdvertCategoryService())
            {
                var lang = GetLang();
                var t = new Localization();

                var masterCategory = advertCategoryService.Get(masterSlug, lang);
                if (masterCategory == null)
                {
                    lang = lang == 1 ? 2 : 1;
                    masterCategory = advertCategoryService.Get(masterSlug, lang);
                    if (masterCategory != null && lang == 1)
                    {
                        SetLang("tr");
                    }
                    else
                    {
                        SetLang("en");
                    }
                }
                var childCategory = advertCategoryService.Get(childSlug, lang);
                if (!string.IsNullOrEmpty(childSlug) && childCategory == null)
                {
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Kategori bulunamadı.", "Category not found.", lang));
                    return Redirect("/");
                }

                var selectedCategory = childCategory ?? masterCategory;

                if (masterCategory == null && childCategory == null)
                    return RedirectToAction("Index", "Home");

                var childId = childCategory?.ID ?? 0;
                var masterId = masterCategory?.ID ?? 0;

                var adverts = advertService.GetCategoryPageAdverts(childId, masterId, lang, 0, GetLoginID(), offset: 0);
                var model = new CategoryViewModel
                {
                    ParentCategory = masterCategory,
                    ChildCategory = childCategory,
                    AdvertCategories = advertCategoryService.GetAll(lang),
                    AdvertsCount = advertCategoryService.GetAdvertsCount(selectedCategory),
                    Adverts = Ads2HomePageItems(adverts, lang),
                    FilterTypes = new List<SelectOption> { new SelectOption { Text="All" , Value=0}, new SelectOption { Text = "OnlyActives", Value = 1 } }
                    
                };

                //kategori sayfasında sayfa içerisinde dil değiştirme işlemi için
                //kategorinin hedef dildeki slug değerlerini çekip translateUrl viewbag'ini doldurur
                // (kategorinin ve üst kategori var ise üst kategorinin)
                var translateLang = (lang == 1) ? 2 : 1;
                var translateUrl = "/" + new Localization().Get("Kategori/", "Category/", translateLang);
                string tMasterSlug = "", tChildSlug = "";
                if (!string.IsNullOrEmpty(masterSlug))
                {
                    if (masterCategory != null)
                        tMasterSlug = textService.GetText(masterCategory.SlugID, translateLang);
                    else
                        tMasterSlug = textService.GetText(childCategory.SlugID, translateLang);
                }
                if (!string.IsNullOrEmpty(childSlug))
                {
                    tChildSlug = textService.GetText(childCategory.SlugID, translateLang);
                }
                translateUrl += tMasterSlug + (tMasterSlug != "" ? "/" : "") + tChildSlug;

                ViewBag.TranslateUrl = translateUrl;
                return View(model);
            }
        }

        [HttpPost]
        [Route("Category/Filter")]
        public PartialViewResult Filter(FilterViewModel filterViewModel)
        {
            using (var service = new AdvertService())
            {
                try
                {
                     // filterViewModel.FilterID =Convert.ToInt16(  HttpContext.Request.Form["FilterID"]);
                    var lang = GetLang();
                    var list = service.FilterAdverts(filterViewModel.ParentCategoryID, filterViewModel.CategoryID,
                    filterViewModel.Content, filterViewModel.Brand, filterViewModel.Model, filterViewModel.AdvertID,
                    filterViewModel.PriceMin, filterViewModel.PriceMax, GetLang(), GetLoginID(), filterViewModel.FilterID);
                    return PartialView("~/Views/Shared/AdvertItem_4ColumnList.cshtml", Ads2HomePageItems(list, lang));
                }
                catch (System.Exception ex)
                {
                    return null;
                }
            }
        }




        [HttpPost]
        [Route("Category/GetAdverts")]
        public PartialViewResult GetAdverts(int id, int categoryId, int parentCategoryId, int offset)
        {
            var lang = GetLang();
            using (var service = new AdvertService())
            {
                var list = service.GetCategoryPageAdverts(categoryId, parentCategoryId, lang, 0, GetLoginID(),offset:offset);
                return PartialView("~/Views/Shared/AdvertItem_4ColumnList.cshtml", Ads2HomePageItems(list, lang));
            }
        }
        [HttpPost]
        [Route("Category/GetAdvertsList")]
        public List<HomePageItem> GetAdvertsList(int id, int categoryId, int parentCategoryId,int offset)
        {
            var lang = GetLang();
            using (var service = new AdvertService())
            {
                var list = service.GetCategoryPageAdverts(categoryId, parentCategoryId, lang, 0, GetLoginID(),offset:offset);
                var response = Ads2HomePageItems(list, lang);
                return response;
            }
        }
    }
}