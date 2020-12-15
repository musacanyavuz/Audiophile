using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class AddListingStep1ViewModel 
    {
        public List<AdvertCategory> AdvertCategories { get; set; }
        public Advert Advert { get; set; }
        public bool CanUseSecurePayment { get; set; }
        public string WarningAdverts { get; set; }
        public decimal AdvertPrice { get; set; }
        public int FreeAdvertLimit { get; set; }

        public bool IsPaymentStepActive { get; set; }
    }
}