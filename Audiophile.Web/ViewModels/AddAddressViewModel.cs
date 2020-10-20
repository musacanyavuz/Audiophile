using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.ViewModels
{
    public class AddAddressViewModel
    {
        public List<Country> Countries { get; set; }
        public List<City> Cities { get; set; }
        public List<District> Districts { get; set; }
    }
}