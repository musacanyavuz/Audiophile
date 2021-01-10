using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Audiophile.Common;

namespace Audiophile.Models
{
    public class PaymentRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int UserID { get; set; }
        public Enums.PaymentType Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public double Price { get; set; }
        
        public int SellerID { get; set; }
        public string IpAddress { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime? ResponseDate { get; set; }
        public string Description { get; set; }
        public bool SecurePayment { get; set; }
        public int ForeignModelID { get; set; }
        public int UpdatedUserID { get; set; } //ödemeyi onaylayan kullanıcı id
        public string PaymentId { get; set; } //iyzicodan dönen payment id
        public int InstallmentNumber { get; set; }
        [ForeignKey("Advert")]
        public int AdvertID { get; set; }
        public string ShippingAddress { get; set; }
        public string InvoiceAddress { get; set; }
        public int Amount { get; set; }
        public string CargoFirm { get; set; }
        public string CargoNo { get; set; }
        public DateTime? CargoDate { get; set; }
        public DateTime? CargoDeliveryDate { get; set; }
        public Enums.PaymentRequestStatus Status { get; set; }
        public bool BuyerApproval { get; set; }
        public DateTime? BuyerApprovalDate { get; set; }
        public string PaymentTransactionID { get; set; }
       
        public Advert Advert { get; set; }
        public User Seller { get; set; }
        public User Buyer { get; set; }
        
    }
}