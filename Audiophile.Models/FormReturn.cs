using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Audiophile.Models
{
    [Table("FormReturns")]
    public class FormReturn
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public int ReplyUserID { get; set; }
        public string ReplyMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReplyDate { get; set; }
        public string IpAddress { get; set; }
    }
}