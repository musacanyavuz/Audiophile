namespace Audiophile.Web.ViewModels
{
    public class CheckoutPaymentPostViewModel
    {
        public int id { get; set; }
        public int Amount { get; set; }
        public int InstallmentNumber { get; set; }
        public int ShippingAddressId { get; set; }
        public int InvoiceAddressId { get; set; }
        public string CardName { get; set; }
        public string CardNumber { get; set; }
        public string CardExpireMonth { get; set; }
        public string CardExpireYear { get; set; }
        public string CardCVC { get; set; }
    }
}