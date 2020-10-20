using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class CheckoutSelectAddressViewModel
    {
        public List<UserAddress> Addresses { get; set; }
        public Advert Advert { get; set; }
        public int Amount { get; set; }
    }
}