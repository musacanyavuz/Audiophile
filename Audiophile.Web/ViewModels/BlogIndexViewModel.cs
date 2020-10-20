using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class BlogIndexViewModel
    {
        public List<AdvertCategory> AdvertCategories { get; set; }
        public List<BlogPost> BlogPosts { get; set; }
        public int PostCount { get; set; }
        public string Title { get; set; }
    }
}