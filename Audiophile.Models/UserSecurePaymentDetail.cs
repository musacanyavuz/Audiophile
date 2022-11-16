using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;

namespace Audiophile.Models
{
    [Table("UserSecurePaymentDetails")]
    public class UserSecurePaymentDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("User")]
        public int UserID { get; set; }
        public Iyzipay.Model.SubMerchantType Type { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string IBAN { get; set; }
        public string MobilePhone { get; set; }
        [NotMapped] public int MobilePhoneDialCode { get; set; }
        public string Email { get; set; }
        public string SenderAddress { get; set; }
        public string InvoiceAddress { get; set; }
        public Enums.CompanyType CompanyTypeID { get; set; }
        public string CompanyName { get; set; }
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyFax { get; set; }
        public string CompanyEmail { get; set; }
        public string IyzicoSubMerchantKey { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string TC { get; set; }
        public int OldIycizoId { get; set; }
        public User User { get; set; }
    }
}