using System.Collections.Generic;
using Audiophile.Models;

namespace Audiophile.Web.Areas.AdminPanel.Models
{
    public class DashboardViewModel
    {
        public List<AdvertPublishRequest> AdvertPublishRequests { get; set; }
        public List<PaymentRequest> PaymentRequests { get; set; }
    }
}