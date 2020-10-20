using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.Areas.AdminPanel.Models
{
    public class UserDetailViewModel
    {
        public User User { get; set; }
        public UserSecurePaymentDetail UserSecurePaymentDetail { get; set; }
        public List<UserAddress> UserAddress { get; set; }
    }
}