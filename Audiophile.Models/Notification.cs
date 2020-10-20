using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;

namespace Audiophile.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int SenderUserID { get; set; }
        public int UserID { get; set; }
        public Enums.NotificationType TypeID { get; set; }
        public string Message { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReadedDate { get; set; }
    }
}