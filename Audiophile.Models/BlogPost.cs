using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("BlogPosts")]
    public class BlogPost
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("User")]
        public int UserID { get; set; }
        public int CategoryID { get; set; }
        public string Title { get; set; }
        public string ShortContent { get; set; }
        public string Content { get; set; }
        public string Tags { get; set; }
        public DateTime? DopingEndDate { get; set; }
        public bool CreditCard { get; set; }
        public int PaymentAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Slug { get; set; }
        public string CoverImage { get; set; }
        public string Thumbnail { get; set; }
        public int ViewCount { get; set; }
        
        public IEnumerable<BlogPostImage> Images { get; set; }
        public User User { get; set; }
    }
}