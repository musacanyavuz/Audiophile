using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class CategoryViewModel
    {
        public AdvertCategory ParentCategory { get; set; }
        public AdvertCategory ChildCategory { get; set; }
        public List<AdvertCategory> AdvertCategories { get; set; }
        public int AdvertsCount { get; set; }
        public List<HomePageItem> Adverts { get; set; }

        public List<SelectOption> FilterTypes { get; set; }
        public int FilterID { get; set; }


    }
    public class SelectOption
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
}