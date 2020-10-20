using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class PersonalInformationViewModel
    {
        public User User { get; set; }
        public List<Country> Countries { get; set; }
        public List<City> Cities { get; set; }
        public List<District> Districts { get; set; }
        public UserSecurePaymentDetail UserSecurePaymentDetail { get; set; }
    }
}