using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("Cities")]
    public class City
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Name { get; set; }
        [ForeignKey("Country")]
        public int CountryID { get; set; }
        public int Order { get; set; }
        public Country Country { get; set; }
        public IEnumerable<District> Districts { get; set; }
    }
}