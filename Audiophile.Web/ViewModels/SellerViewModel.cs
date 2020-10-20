using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class SellerViewModel
    {
        public User Seller { get; set; }
        public List<HomePageItem> Ads { get; set; }
    }
}