using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;

namespace Audiophile.Models
{
    [Table("SystemSettings")]
    public class SystemSetting
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public Enums.SystemSettingName Name { get; set; }
        public string Value { get; set; }
        public int Type { get; set; }
    }
}