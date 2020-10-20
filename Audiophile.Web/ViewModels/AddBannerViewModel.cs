using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class AddBannerViewModel
    {
        public List<DopingType> LogoDopings { get; set; }
        public List<DopingType> BannerDopings { get; set; }
    }
}