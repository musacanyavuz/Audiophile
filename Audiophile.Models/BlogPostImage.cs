using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("BlogPostImages")]
    public class BlogPostImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("BlogPost")]
        public int PostID { get; set; }
        public string Source { get; set; }
        public string Thumbnail { get; set; }
        public DateTime CreatedDate { get; set; }

        public BlogPost BlogPost { get; set; }
    }
}