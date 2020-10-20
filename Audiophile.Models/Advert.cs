using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Audiophile.Models
{
    [Table("Adverts")]
    public class Advert
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string Title { get; set; }
        public string Content { get; set; }
        public double Price { get; set; }
        public double NewProductPrice { get; set; }
        public bool IsAvailableForSwap { get; set; }
        public int MoneyTypeID { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string ProductStatus { get; set; }
        [ForeignKey("Category")]
        public int CategoryID { get; set; }
        //[ForeignKey("SubCategory")]
        //public int SubCategoryID { get; set; }
        public string CoverPhoto { get; set; }
        public bool UseSecurePayment { get; set; }
        public double StockAmount { get; set; } 
        public string AvailableInstallments { get; set; }
        
        public int ShippingTypeID { get; set; }
        public int PaymentMethodID { get; set; }
        public int CargoAreaID { get; set; }
        public bool FreeShipping { get; set; }
        public double ShippingPrice { get; set; }
        public bool OriginalBox { get; set; }
        public bool TermsOfUse { get; set; }
        public string WebSite { get; set; }
        public string ProductDefects { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public string Slug { get; set; }
        [ForeignKey("User")]
        public int UserID { get; set; }
        public bool IsActive { get; set; }
        public DateTime? PublishDate { get; set; }
        public bool IsAvailableBargain { get; set; }
        public string Thumbnail { get; set; }
        public int ViewCount { get; set; }
        public bool IsDeleted { get; set; }
        public string IpAddress { get; set; }
        public bool IsDraft { get; set; }

        public IEnumerable<AdvertDoping> AdvertDopings { get; set; }
        public AdvertCategory Category { get; set; }
        //public AdvertCategory SubCategory { get; set; }
        public User User { get; set; }
        public IEnumerable<AdvertPhoto> Photos { get; set; }
        public DopingType LabelDopingModel { get; set; }
        
        [NotMapped] public string CategorySlug { get; set; }
        [NotMapped] public string SubCategorySlug { get; set; }
        [NotMapped] public int LikesCount { get; set; }
        [NotMapped] public List<IFormFile> Files { get; set; }
        [NotMapped] public string SubCategorySlugTr { get; set; }
        [NotMapped] public string ParentCategorySlugTr { get; set; }
        [NotMapped] public string SubCategorySlugEn { get; set; }
        [NotMapped] public string ParentCategorySlugEn { get; set; }
        [NotMapped] public int LabelDoping { get; set; }
        [NotMapped] public int YellowFrameDoping { get; set; }
        [NotMapped] public int AdvertOrder { get; set; }
        [NotMapped] public bool IsPendingApproval { get; set; }
        [NotMapped] public bool IsILiked { get; set; }
        [NotMapped] public string CategoryNameTr { get; set; }
        [NotMapped] public string CategoryNameEn { get; set; }
        [NotMapped] public string UserName { get; set; }
    }
}