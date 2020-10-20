using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("AdvertDopings")]
    public class AdvertDoping
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("Advert")]
        public int AdvertID { get; set; }
        [ForeignKey("DopingType")]
        public int TypeID { get; set; }
        public int Price { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsPendingApproval { get; set; }

        public Advert Advert { get; set; }
        public DopingType DopingType { get; set; }
        public int PaymentID { get; set; }
    }
}