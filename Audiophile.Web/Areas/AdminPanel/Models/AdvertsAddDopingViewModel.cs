using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.Areas.AdminPanel.Models
{
    public class AdvertsAddDopingViewModel
    {
        public Advert Advert { get; set; }
        public List<DopingType> DopingTypes { get; set; }
    }

    public class AdvertEditModel
    {
        public string Title { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Id { get; set; }
    }
}