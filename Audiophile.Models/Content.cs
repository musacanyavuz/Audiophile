using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;

namespace Audiophile.Models
{
    [Table("Contents")]
    public class Content
    { 
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int ID { get; set; }
        public int LanguageID { get; set; }
        public string TextContent { get; set; }
        public string Name { get; set; }
        [Key]
        public int Key { get; set; }

        public Enums.TextType TextType { get; set; }

        [NotMapped] public string Title { get; set; }
    }
}