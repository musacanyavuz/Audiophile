using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("UserAddresses")]
    public class UserAddress
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("User")]
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        [ForeignKey("City")] 
        public int CityID { get; set; }
        [ForeignKey("District")]
        public int DistrictID { get; set; }
        public int NeighborhoodID { get; set; }
        public string PostalCode { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Title { get; set; }
        public string Phone2 { get; set; }
        public bool IsDefault { get; set; }
        public string CityText { get; set; }
        public int CountryID { get; set; }

        public City City { get; set; }
        public District District { get; set; }
        public User User { get; set; }
        public Country Country { get; set; }
    }
}