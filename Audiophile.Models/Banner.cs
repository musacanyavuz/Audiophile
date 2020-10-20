using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;
using Audiophile.Models;
using Microsoft.AspNetCore.Http;

namespace Audiophile.Models
{
    [Table("Banners")]
    public class Banner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public DateTime CreatedDate { get; set; }
        [ForeignKey("User")]
        public int UserID { get; set; }
        public bool IsApproved { get; set; }
        public int ApprovedUserID { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string ImageSource { get; set; }
        public int Order { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Enums.BannerType TypeID { get; set; }
        public bool IsActive { get; set; }
        public int ClickCount { get; set; }
        public int Price { get; set; }

        public User User { get; set; }
        [NotMapped] public IFormFile ImageSourceFile { get; set; }
    }
}