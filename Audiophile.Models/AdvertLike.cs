using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("AdvertLikes")]
    public class AdvertLike
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("Advert")]
        public int AdvertID { get; set; }
        [ForeignKey("User")]
        public int UserID { get; set; }
        public DateTime CreatedDate { get; set; }

        public Advert Advert { get; set; }
        public User User { get; set; }
    }
}