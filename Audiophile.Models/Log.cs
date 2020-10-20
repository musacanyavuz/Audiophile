using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;

namespace Audiophile.Models
{
    [Table("Logs")]
    public class Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string Function { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Params { get; set; }
        public bool IsError { get; set; }
        
        public int? Key1 { get; set; }
        public int? Key2 { get; set; }
        public int? Key3 { get; set; }
        public int? Key4 { get; set; }
        public int? Key5 { get; set; }
        public Enums.LogType Type { get; set; }
    }
}