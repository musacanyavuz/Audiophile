using System;
using Audiophile.Common;

namespace Audiophile.Web.ViewModels
{
    public class HomePageItem
    {
        public int ID { get; set; }
        public Enums.HomePageItemType Type { get; set; }
        public string Url { get; set; }
        public string ImageSource { get; set; }
        public string Title { get; set; }
        public DateTime CreatedDate { get; set; }
        public int LikesCount { get; set; }
        public string Price { get; set; }
        public string PriceCurrency { get; set; }
        public string OldPrice { get; set; }
        public bool ILiked { get; set; }
        public string LabelDopingType { get; set; }
        public bool YellowDoping { get; set; }
        public string SecurePayment { get; set; }
        public int AdvertOrder { get; set; }
        public int ViewCount { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public string ProductStatus { get; set; }
        public string StockAmount { get; set; }
        public string CategoryName { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        public string UserName { get; set; }
        public string Content { get; set; }
        public string Model { get; set; }
        public string NewProductPrice { get; set; }

        public string DecPrice { get; set; }
        public int MoneyType { get; set; }

        public bool IsActive { get; set; }

        public bool IsApproved { get; set; }

    }
}