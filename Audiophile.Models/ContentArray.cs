using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("ContentArrays")]
    public class ContentArray
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int TitleID { get; set; }
        public int ContentID { get; set; }
        public int TypeID { get; set; } //1: yardım 2: kullanım koşulları 3: güvenli ödeme
        
        [NotMapped] public string Title { get; set; }
        [NotMapped] public string Content { get; set; }
    }
}