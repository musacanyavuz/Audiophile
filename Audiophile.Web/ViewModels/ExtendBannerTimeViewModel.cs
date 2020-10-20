using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class ExtendBannerTimeViewModel
    {
        public List<DopingType> Dopings { get; set; }
        public Banner Banner { get; set; }
    }
}