using Audiophile.Common;
using Audiophile.Services;
using Audiophile.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Audiophile.Web.Controllers
{
    public class BlogController : BaseController
    {
        #region PostList

        [Route("Blog/Haberler-Kampanyalar")]
        [Route("Blog/News-Campaigns")]
        public IActionResult NewsCampaigns()
        { 
            var model = GetModelView((int) Enums.BlogCategory.HaberlerKampanyalar, "Haberler, Kampanyalar",
                "News, Campaings", false);
            ViewBag.CategoryID = (int) Enums.BlogCategory.HaberlerKampanyalar -1;
            return View("Index", model);   
        }
        
        [Route("Blog/Saticilar-Ithalatcilar")]
        [Route("Blog/Sellers-Importers")]
        public IActionResult SellersImporters()
        { 
            var model = GetModelView((int) Enums.BlogCategory.SaticilarIthalatcilar, "Satıcılar, İthalatçılar",
                "Sellers, Importers", false);
            ViewBag.CategoryID = (int) Enums.BlogCategory.SaticilarIthalatcilar -1;
            return View("Index", model);   
        }
        
        [Route("Blog/Odyofil-Sistemler")]
        [Route("Blog/Audiophile-Systems")]
        public IActionResult AudiophilesSystems()
        { 
            var model = GetModelView((int) Enums.BlogCategory.OdyofilSistemleri, "Odyofil Sistemler",
                "Audiophiles Systems", false);
            ViewBag.CategoryID = (int) Enums.BlogCategory.OdyofilSistemleri -1;
            return View("Index", model);   
        }
        
        [Route("Blog/Yazilar-Makaleler")]
        [Route("Blog/Articles")]
        public IActionResult Articles()
        { 
            var model = GetModelView((int) Enums.BlogCategory.YazilarMakaleler, "Yazılar, Makaleler",
                "Articles", false);
            ViewBag.CategoryID = (int) Enums.BlogCategory.YazilarMakaleler -1;
            return View("Index", model);   
        }


        private BlogIndexViewModel GetModelView(int id, string titleTr, string titleEn, bool dopingFilter)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var catService = new AdvertCategoryService())
            using (var service = new BlogPostService())
            { 
                var model = new BlogIndexViewModel
                {
                    BlogPosts = service.GetPosts(id, dopingFilter),
                    AdvertCategories = catService.GetAll(lang),
                    PostCount = service.GetPostCount(id, dopingFilter),
                    Title = t.Get(titleTr, titleEn, lang)
                };
                return model;
            }
        }

        [HttpPost]
        [Route("Blog/GetPosts")]
        public JsonResult GetPosts(int id, int categoryId, bool dopingFilter)
        { 
            using (var service = new BlogPostService())
            {
                var list = service.GetPosts(categoryId, dopingFilter, id);
                return Json(new {data = list });    
            }
        }

        #endregion

        [Route("blog/{categorySlug}/{postSlug}/{id}")]
        public IActionResult Post(string categorySlug, string postSlug, int id)
        {
            var lang = GetLang();
            using (var catService = new AdvertCategoryService())
            using (var service = new BlogPostService())
            {
                var post = service.Get(id);
                if (post == null)
                    return Redirect("/");

                var dopingFilter = post.CategoryID == (int)Enums.BlogCategory.SaticilarIthalatcilar || post.CategoryID == (int)Enums.BlogCategory.HaberlerKampanyalar;

                var model = new BlogPostDetailViewModel
                {
                    BlogPost = post,
                    AdvertCategories = catService.GetAll(lang),
                    PostCount = service.GetPostCount(post.CategoryID, dopingFilter),
                    Title = Constants.GetBlogCategoryTitle(post.CategoryID, lang)
                };
                service.UpdateViewCount(id);
                
                return View(model);
            }
            
        }
        
    }
}