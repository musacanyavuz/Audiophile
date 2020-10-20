using System.Collections.Generic;
using Audiophile.Models;
using Audiophile.Services;

namespace Audiophile.Web.ViewModels
{
    public class HomePageViewModel
    {
        public List<AdvertCategory> AdvertCategories { get; set; }   
        public List<Banner> Banners { get; set; }
        public List<Banner> HomepageSplitBanners { get; set; }
        public Banner HomepageBottomAd { get; set; }
        public List<HomePageItem> Items { get; set; }
    }
}