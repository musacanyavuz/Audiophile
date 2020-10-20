using System;
using System.Collections.Generic;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;

namespace Audiophile.Common.Iyzico
{
    public class IyzicoService
    {
        private const string ApiKey = "cZNlQc3WBD1bS93ICOK6fHH1pGZt7iU1";
        private const string SecretKey = "3IizovoTcqZ7qV8gUpylyJ55t1h4zMvg";
        private const string BaseUrl = "https://api.iyzipay.com";
        private const string SiteUrl = "https://www.audiophile.org/";
        //private const string SiteUrl = "https://test.audophile.org/";
        //private const string SiteUrl = "https://localhost:44348/";
        public const string Callback = "Callback";

        public static IyzicoResult<InstallmentInfo> GetInstallmentInfoAllBanks(double price)
        {
            var request = new RetrieveInstallmentInfoRequest
            {
                Locale = Locale.TR.ToString(),
                Price = price.ToString("0.##").Replace(",", "."),
            };

            var installmentInfoResult = InstallmentInfo.Retrieve(request, GetOptions());
            if (installmentInfoResult.Status == "success")
            {
                return new IyzicoResult<InstallmentInfo> { IsSuccess = true, Message = string.Empty, Data = installmentInfoResult };
            }
            return new IyzicoResult<InstallmentInfo> { IsSuccess = false, Message = installmentInfoResult.ErrorMessage };
        }

        public static IyzicoResult<InstallmentInfo> GetInstallmentInfoAllBanks(string binNumber, string totalPrice)
        {
            var request = new RetrieveInstallmentInfoRequest
            {
                Price = totalPrice,
                Locale = Locale.TR.ToString(),
                BinNumber = binNumber
            };

            var installmentInfoResult = InstallmentInfo.Retrieve(request, GetOptions());
            if (installmentInfoResult.Status == "success")
            {
                return new IyzicoResult<InstallmentInfo> { IsSuccess = true, Message = string.Empty, Data = installmentInfoResult };
            }
            return new IyzicoResult<InstallmentInfo> { IsSuccess = false, Message = installmentInfoResult.ErrorMessage };
        }

        public static Options GetOptions()
        {
            var d = new Options
            {
                ApiKey = ApiKey,
                SecretKey = SecretKey,
                BaseUrl = BaseUrl,
            };
            return d;
        }


        public static IyzicoResult<SubMerchant> CreateSubMerchant(SubMerchant submerchant)
        {
            var request = new CreateSubMerchantRequest
            {
                Locale = submerchant.Locale,
                ConversationId = submerchant.ConversationId,
                SubMerchantExternalId = submerchant.SubMerchantExternalId,
                SubMerchantType = submerchant.SubMerchantType,
                Address = submerchant.Address,
                Email = submerchant.Email,
                GsmNumber = submerchant.GsmNumber,
                Name = submerchant.Name,
                Iban = submerchant.Iban,
                Currency = "TRY",
                IdentityNumber = submerchant.IdentityNumber
            };

            if (submerchant.SubMerchantType == SubMerchantType.PERSONAL.ToString())
            {
                request.ContactName = submerchant.ContactName;
                request.ContactSurname = submerchant.ContactSurname;
                request.IdentityNumber = submerchant.IdentityNumber;
            }
            else if (submerchant.SubMerchantType == SubMerchantType.PRIVATE_COMPANY.ToString())
            {
                request.TaxOffice = submerchant.TaxOffice;
                request.LegalCompanyTitle = submerchant.LegalCompanyTitle;
                request.IdentityNumber = submerchant.IdentityNumber;
            }
            else if (submerchant.SubMerchantType == SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY.ToString())
            {
                request.TaxOffice = submerchant.TaxOffice;
                request.TaxNumber = submerchant.TaxNumber;
                request.LegalCompanyTitle = submerchant.LegalCompanyTitle;
            }
            var a = GetOptions();
            var subMerchantResult = SubMerchant.Create(request, GetOptions());
            if (subMerchantResult.Status == "success")
            {
                return new IyzicoResult<SubMerchant> { IsSuccess = true, Message = "Alt üye iş yeri başarıyla oluşturuldu!", Data = subMerchantResult };
            }
            else
            {
                return new IyzicoResult<SubMerchant> { IsSuccess = false, Message = subMerchantResult.ErrorMessage, ErrorCode = subMerchantResult.ErrorCode };
            }
        }


        public static IyzicoResult<SubMerchant> UpdateSubMerchant(SubMerchant submerchant)
        {

            string subMerchantKey = GetSubMerchantKey(submerchant);

            var request = new UpdateSubMerchantRequest()
            {
                Locale = submerchant.Locale,
                SubMerchantKey = subMerchantKey,
                ConversationId = submerchant.ConversationId,
                Address = submerchant.Address,
                Email = submerchant.Email,
                GsmNumber = submerchant.GsmNumber,
                Name = submerchant.Name,
                Iban = submerchant.Iban,
                Currency = "TRY",
                IdentityNumber = submerchant.IdentityNumber

            };
            if (submerchant.SubMerchantType == SubMerchantType.PERSONAL.ToString())
            {
                request.ContactName = submerchant.ContactName;
                request.ContactSurname = submerchant.ContactSurname;
                request.IdentityNumber = submerchant.IdentityNumber;
            }
            else if (submerchant.SubMerchantType == SubMerchantType.PRIVATE_COMPANY.ToString())
            {
                request.TaxOffice = submerchant.TaxOffice;
                request.LegalCompanyTitle = submerchant.LegalCompanyTitle;
                request.IdentityNumber = submerchant.IdentityNumber;
            }
            else if (submerchant.SubMerchantType == SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY.ToString())
            {
                request.TaxOffice = submerchant.TaxOffice;
                request.TaxNumber = submerchant.TaxNumber;
                request.LegalCompanyTitle = submerchant.LegalCompanyTitle;
            }

            var subMerchantResult = SubMerchant.Update(request, GetOptions());
            if (subMerchantResult.Status == "success")
            {
                return new IyzicoResult<SubMerchant> { IsSuccess = true, Message = "Alt üye iş yeri başarıyla güncellendi!", Data = subMerchantResult };
            }
            else
            {
                return new IyzicoResult<SubMerchant> { IsSuccess = false, Message = subMerchantResult.ErrorMessage, ErrorCode = subMerchantResult.ErrorCode };
            }
        }

        public static string GetSubMerchantKey(SubMerchant submerchant)
        {
            var SubMerchantKeyRequest = new RetrieveSubMerchantRequest()
            {
                Locale = submerchant.Locale,
                ConversationId = submerchant.ConversationId,
                SubMerchantExternalId = submerchant.SubMerchantExternalId
            };
            var subKeyResult = SubMerchant.Retrieve(SubMerchantKeyRequest, GetOptions());
            return subKeyResult.SubMerchantKey;
        }





        public static ThreedsInitialize PayBanner(int requestId, int price, PaymentCard paymentCard, Buyer buyer, Address shippingAddress,
            Address billingAddress, int dopingId, string dopingName, string callback)
        {//banner ödemeleri, doping ödemeleri, blog ödemeleri bu fonksiyon ile yapılıyor
            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = requestId.ToString();
            request.Price = price.ToString();
            request.PaidPrice = price.ToString();
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.SUBSCRIPTION.ToString();
            request.CallbackUrl = SiteUrl + callback;
            request.PaymentCard = paymentCard;
            request.Buyer = buyer;
            request.ShippingAddress = shippingAddress;
            request.BillingAddress = billingAddress;

            var basketItems = new List<BasketItem>();
            var firstBasketItem = new BasketItem
            {
                Id = dopingId.ToString(),
                Name = dopingName,
                Category1 = dopingName,
                ItemType = BasketItemType.VIRTUAL.ToString(),
                Price = price.ToString(),
                //SubMerchantKey = "hlvmB2LSjkZALCL/Zfb9qfJhX9o=",
                //SubMerchantPrice = subMerchantPrice.ToString("F2").Replace(",",".")
            };
            basketItems.Add(firstBasketItem);
            request.BasketItems = basketItems;

            var threedsInitialize = ThreedsInitialize.Create(request, IyzicoService.GetOptions());
            return threedsInitialize;
        }

        public static ThreedsInitialize SecurePayment(int requestId, double price, PaymentCard paymentCard, Buyer buyer, Address shippingAddress,
            Address billingAddress, int adId, string callback, string subMerchantKey, string subMerchantPrice, int installment, string name,
            string category1, string category2)
        {
            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = requestId.ToString();
            request.Price = price.ToString("0.##");
            request.PaidPrice = price.ToString("0.##");
            request.Currency = Currency.TRY.ToString();
            request.Installment = installment;
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();
            request.CallbackUrl = SiteUrl + callback;

            request.PaymentCard = paymentCard;
            request.Buyer = buyer;
            request.ShippingAddress = shippingAddress;
            request.BillingAddress = billingAddress;

            var basketItems = new List<BasketItem>();
            var firstBasketItem = new BasketItem
            {
                Id = adId.ToString(),
                Name = name,
                Category1 = category1,
                Category2 = category2,
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = price.ToString("0.##"),
                SubMerchantKey = subMerchantKey,
                SubMerchantPrice = subMerchantPrice.Replace(",", ".")
            };
            basketItems.Add(firstBasketItem);
            request.BasketItems = basketItems;


            var threadsInitialize = ThreedsInitialize.Create(request, IyzicoService.GetOptions());
            return threadsInitialize;
        }

        public static Approval PaymentApproval(int paymentRequestId, string paymentTransactionId)
        {//ödeme onayı, parayı satıcıya aktarır
            var request = new CreateApprovalRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = paymentRequestId.ToString();
            request.PaymentTransactionId = paymentTransactionId;

            var approval = Approval.Create(request, GetOptions());
            return approval;
        }
        public static Cancel PaymentCancel(int paymentRequestId, string paymentId, string ipAddress)
        {//ödeme iptali, alıcının parası iade edilir
            var request = new CreateCancelRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = paymentRequestId.ToString();
            request.PaymentId = paymentId;
            request.Ip = ipAddress;

            var cancel = Cancel.Create(request, GetOptions());
            return cancel;
        }
        public static Refund PaymentRefund(int paymentRequestId, string paymentTransactionId, string ipAddress, string price)
        {//ödeme iadesi, alıcının parası iade edilir
            var request = new CreateRefundRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = paymentRequestId.ToString();
            request.PaymentTransactionId = paymentTransactionId;
            request.Ip = ipAddress;
            request.Price = price;

            var refund = Refund.Create(request, GetOptions());
            return refund;
        }
    }
}