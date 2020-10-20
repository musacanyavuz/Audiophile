using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("AdvertPublishRequests")]
    public class AdvertPublishRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("Advert")]
        public int AdvertID { get; set; }
        public int UserID { get; set; }
        public DateTime RequestDate { get; set; }
        public bool IsActive { get; set; }

        public Advert Advert { get; set; }
    }
}