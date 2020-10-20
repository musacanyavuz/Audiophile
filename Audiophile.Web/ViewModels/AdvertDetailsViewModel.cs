using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class AdvertDetailsViewModel
    {
        public Advert Advert { get; set; }
        public List<AdvertCategory> AdvertCategories { get; set; }
        public List<Advert> SimilarAdverts { get; set; }
        public CargoArea CargoArea { get; set; }
    }
}