using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("AdvertPhotos")]
    public class AdvertPhoto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("Advert")]
        public int AdvertID { get; set; }
        public string Thumbnail { get; set; }
        public string Source { get; set; }
        public DateTime CreatedDate { get; set; }
        public int TypeID { get; set; }
        public int OrderNumber { get; set; }

        public Advert Advert { get; set; }
    }
}