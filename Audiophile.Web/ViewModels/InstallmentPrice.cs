using System.Collections.Generic;

namespace Audiophile.Web.ViewModels
{
    public class InstallmentPrice
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string CardName { get; set; }
        public List<InstallmentPriceDetail> Details { get; set; }
    }

    public class InstallmentPriceDetail
    {
        public int? InstallmentNumber { get; set; }
        public string Price { get; set; }
        public string TotalPrice { get; set; }
        public string SubmerchantPrice { get; set; }
    }
}