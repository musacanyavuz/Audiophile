using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class FeaturedAdStep3ViewModel
    {
        public Advert Advert { get; set; }
        public List<DopingType> DopingTypes { get; set; }
        public List<AdvertDoping> AdvertDopings { get; set; }
        public string WarningAdverts { get; set; }
    }
}