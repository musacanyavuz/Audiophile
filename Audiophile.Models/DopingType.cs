using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;

namespace Audiophile.Models
{
    [Table("DopingTypes")]
    public class DopingType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string Name { get; set; }
        public int Day { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }
        //public string Group { get; set; }
        public Enums.DopingGroup Group { get; set; }
        public string TitleTr { get; set; }
        public string TitleEn { get; set; }
    }
}