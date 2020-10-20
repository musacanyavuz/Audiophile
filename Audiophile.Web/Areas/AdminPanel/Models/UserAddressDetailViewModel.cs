using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.Areas.AdminPanel.Models
{
    public class UserAddressDetailViewModel
    {
        public UserAddress UserAddress { get; set; }
        public List<Country> Countries { get; set; }
        public List<City> Cities { get; set; }
        public List<District> Districts { get; set; }
    }
}