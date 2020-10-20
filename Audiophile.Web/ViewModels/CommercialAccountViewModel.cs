using System.Collections.Generic;
using Audiophile.Models;
using Audiophile.Services;

namespace Audiophile.Web.ViewModels
{
    public class CommercialAccountViewModel
    {
        public List<PaymentRequest> Buys { get; set; }
        public List<PaymentRequest> Sales { get; set; }
        public List<AdvertDoping> Dopings { get; set; }
        public List<Banner> LogoBanners { get; set; }
        public List<Banner> Banners { get; set; }
        public string Text { get; set; }
        public string SalesCount { get; set; }
        public string BuysCount { get; set; }
        public string BannersCount { get; set; }
        public string LogoBuysCount { get; set; }
        public string DopingCount { get; set; }

    }
}