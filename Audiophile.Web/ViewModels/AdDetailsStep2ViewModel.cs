using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class AdDetailsStep2ViewModel
    {
        public Advert Advert { get; set; }
        public List<CargoArea> CargoAreas { get; set; }
        public string WarningAdverts { get; set; }

    }
}