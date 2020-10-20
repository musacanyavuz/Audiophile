using System;
using System.Collections.Generic;
using System.Linq;
using Audiophile.Common;
using Audiophile.Common.Iyzico;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.Helpers;
using Audiophile.Web.ViewModels;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InstallmentPrice = Audiophile.Web.ViewModels.InstallmentPrice;

namespace Audiophile.Web.Controllers
{
    public class CheckoutController : BaseController
    {
        // GET
        [Route("SatinAl/AdresSec")]
        [Route("Checkout/SelectAddress")]
        [Authorize]
        public IActionResult SelectAddress(int id, int amount)
        {

            using (var userService = new UserService())
            using (var logService = new LogServices())
            using (var adService = new AdvertService())
            {
                var result = userService.GetSecurePaymentDetail(GetLoginID());
                if (result == null)
                {
                    Notification = new UiMessage(NotyType.error, "Güvenli Ödeme Bilgilerinizi Doldurmadan Kredi Kartı ile İşlem Yapamazsınız", "You cannot trade with credit card without filling in your secure payment information.", GetLang());

                    LogInsert(GetLoginID() + " idli kullanıcı " + id + " idli ürünü alırken Güvenli Ödeme Bilgileriniz Doldurmadan Kredi Kartı ile İşlem Yapamazsınız hatasını aldı.", Enums.LogType.Payment, GetLoginID(), id);

                    return RedirectToAction("PersonalInformation", "Account");
                }
                else
                {
                    var model = new CheckoutSelectAddressViewModel
                    {
                        Advert = adService.Get(id),
                        Addresses = userService.GetUserAddresses(GetLoginID()),
                        Amount = amount
                    };
                    LogInsert(GetLoginID() + " idli kullanıcı " + id + " idli ürünü almak için adres sayfasına giriş yaptı.", Enums.LogType.Payment, GetLoginID(), id);

                    return View(model);
                }



            }
        }

        [Route("SatinAl/Odeme")]
        [Route("Checkout/Payment")]
        [Authorize]
        public IActionResult Payment(int id, int amount, int shippingAddressId, int invoiceAddressId)
        {
            var lang = GetLang();
            using (var userService = new UserService())
            using (var adService = new AdvertService())
            {
                try
                {
                    var advert = adService.Get(id);

                    var result = IyzicoService.GetInstallmentInfoAllBanks(advert.Price);
                    if (result.IsSuccess == false)
                    {
                        Notification = new UiMessage(NotyType.error, "Taksit bilgilerine erişilemedi. " + result.Message,
                            "Couldn't access to installment choices.", lang);

                        LogInsert(GetLoginID() + " idli kullanıcı " + id + " idli ürünü alırken ürün taksit bilgilerine erişilemedi", Enums.LogType.Payment, GetLoginID(), id);

                    }
                    var model = new CheckoutPaymentViewModel
                    {
                        ShippingAddressID = shippingAddressId,
                        InvoiceAddressID = invoiceAddressId,
                        InstallmentDetails = result.Data.InstallmentDetails,
                        Amount = amount,
                        Advert = advert
                    };
                    LogInsert(GetLoginID() + " idli kullanıcı " + id + " idli ürünü alırken adres girişini tamamladı.", Enums.LogType.Payment, GetLoginID(), id);

                    return View(model);
                }
                catch (Exception e)
                {
                    LogInsert(GetLoginID() + " idli kullanıcı " + id + " idli ürünü alırken " + e.Message + " hatasını aldı.", Enums.LogType.Payment, GetLoginID(), id);
                    throw;
                }

            }
        }

        [Authorize]
        public IActionResult SecurePayment(CheckoutPaymentPostViewModel model)
        {
            var lang = GetLang();
            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrEmpty(model.CardName) || string.IsNullOrEmpty(model.CardNumber) ||
                string.IsNullOrEmpty(model.CardExpireMonth) || string.IsNullOrEmpty(model.CardExpireYear) ||
                string.IsNullOrEmpty(model.CardCVC))
            {
                Notification = new UiMessage(NotyType.error, "Lütfen tüm kredi kartı bilgilerini doldurunuz.",
                    "Please fill in all credit card information.", lang);
                LogInsert(GetLoginID() + " idli kullanıcı " + model.id + " idli ürünü alırken kredi kartı bilgilerini doldurmadı.", Enums.LogType.Payment, GetLoginID(), model.id);
                return RedirectToAction("Payment", new
                {
                    id = model.id,
                    amount = model.Amount,
                    shippingAddressId = model.ShippingAddressId,
                    invoiceAddressId = model.InvoiceAddressId
                });
            }
            using (var uService = new UserService())
            using (var prService = new PaymentRequestService())
            using (var sysService = new SystemSettingService())
            using (var adService = new AdvertService())
            {
                try
                {
                    var ad = adService.Get(model.id);
                    var user = uService.Get(GetLoginID());
                    var getUserId = GetLoginID();
                    if (ad == null)
                    {
                        Notification = new UiMessage(NotyType.error, "İlana erişilemedi.", "Could not access to ad.", lang);

                        LogInsert(getUserId + " idli kullanıcı " + model.id + " ürünü almaya çalışırken ilana erişilemedi hatası aldı.", Enums.LogType.Payment, GetLoginID(), model.id);

                        return Redirect("/");
                    }
                    if (user == null)
                    {
                        Notification = new UiMessage(NotyType.error, "Kullanıcı bilgilerinize erişilemedi.", "Could not access to your user account.", lang);

                        LogInsert(getUserId + " idli kullanıcı " + model.id + " ürünü almaya çalışırken kullanıcı bilgilerine erişilemedi hatası aldı", Enums.LogType.Payment, GetLoginID(), model.id);

                        return Redirect("/");
                    }
                    var sellerInfo = uService.GetSecurePaymentDetail(ad.UserID);
                    if (sellerInfo == null)
                    {
                        Notification = new UiMessage(NotyType.error, "Satıcının satış bilgilerine erişilemedi. Lütfen bizimle irtibata geçiniz.",
                            "The vendor's sales information could not be accessed. Please contact us.", lang);

                        LogInsert(GetLoginID() + " idli kullanıcı " + model.id + " ürünü almaya çalışırken satıcının satış bilgilerine erişilemedi hatası aldı.", Enums.LogType.Payment, GetLoginID(), model.id);

                        return Redirect("/");
                    }
                    var totalPrice = ad.Price * model.Amount;

                    var result = IyzicoService.GetInstallmentInfoAllBanks(model.CardNumber.Substring(0, 6), totalPrice.ToString("0.##"));
                    if (!result.IsSuccess)
                    {
                        Notification = new UiMessage(NotyType.error, "Taksit bilgilerine erişilemedi.", "Could not access to installment details.", lang);

                        LogInsert(getUserId + " idli kullanıcı " + model.id + " ürünü almaya çalışırken taksit bilgierine erişlemedi hatası aldı." + result.Message, Enums.LogType.Payment, GetLoginID(), model.id);

                        return Redirect("/");
                    }

                    #region subMerchantPrice

                    var bankTotalPrice = result.Data.InstallmentDetails.First().InstallmentPrices
                        .Single(x => x.InstallmentNumber == model.InstallmentNumber).TotalPrice;
                    var bankComission = double.Parse(bankTotalPrice.Replace(".", ",")) - totalPrice;

                    var settings = sysService.GetSystemSettings();
                    var audioPhileCommission = double.Parse(settings
                        .Single(x => x.Name == Enums.SystemSettingName.AudiophileKomisyonuYuzde).Value);
                    var iyzicoCommission = double.Parse(settings
                        .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuYuzde).Value);
                    var iyzicoCommissionTL = double.Parse(settings
                        .Single(x => x.Name == Enums.SystemSettingName.IyzicoKomisyonuTL).Value); //iyzico sabit TL komisyon

                    var apComission = (totalPrice * audioPhileCommission) / 100; //audiophile komisyonu                
                    var iyzCommission = 0.0;
                    if (model.InstallmentNumber == 1)
                    {   //taksitli işlemlerde taksit farkına iyzico komisyonu dahil geliyor
                        iyzCommission = (totalPrice * iyzicoCommission) / 100; // iyzico yüzdelik komisyon
                        iyzCommission += iyzicoCommissionTL;
                    }

                    var subMerchantPrice = totalPrice - bankComission - apComission - iyzCommission;

                    #endregion

                    user.Addresses = uService.GetUserAddresses(GetLoginID());
                    var shippingAddress = user.Addresses.Single(x => x.ID == model.ShippingAddressId);
                    var invoiceAddress = user.Addresses.Single(x => x.ID == model.InvoiceAddressId);

                    var shippingAddressLine = shippingAddress.Address + " " + shippingAddress.City?.Name + " " +
                                              shippingAddress.District?.Name + shippingAddress.CityText + " " + shippingAddress?.Country?.Name;
                    var invoiceAddressLine = invoiceAddress.Address + " " + invoiceAddress.City?.Name + " " +
                                             shippingAddress.District?.Name + invoiceAddress.CityText + " " + invoiceAddress?.Country?.Name;

                    var paymentRequest = new PaymentRequest
                    {
                        Price = totalPrice,
                        InstallmentNumber = model.InstallmentNumber,
                        Description = "#" + GetLoginID() + " " + user.Name + " isimli kullanıcı ürün satın alma işlemi başlattı: #" + ad.ID + " " + ad.Title,
                        CreatedDate = DateTime.Now,
                        AdvertID = ad.ID,
                        IsSuccess = false,
                        IpAddress = ip,
                        SellerID = ad.UserID,
                        SecurePayment = true,
                        Type = Enums.PaymentType.Ilan,
                        UserID = GetLoginID(),
                        ShippingAddress = shippingAddressLine,
                        InvoiceAddress = invoiceAddressLine,
                        Amount = model.Amount,
                        Status = Enums.PaymentRequestStatus.Bekleniyor,
                        


                    };
                    LogInsert(getUserId + " idli kullanıcı " + model.id + " ürünü alma işlemini başlattı.", Enums.LogType.Payment, GetLoginID(), model.id);

                    var prInsert = prService.Insert(paymentRequest);
                    if (!prInsert)
                    {
                        Notification = new UiMessage(NotyType.error, "Online ödeme kaydınız oluşturulamadı.", "Your online payment registration could not be saved..", lang);

                        LogInsert(getUserId + " idli kullanıcı " + model.id + " ürünü almaya çalışırken online ödeme kaydı oluşturulmadı hatası aldı.", Enums.LogType.Payment, GetLoginID(), model.id);

                        return Redirect("/");
                    }

                    var card = new PaymentCard
                    {
                        CardNumber = model.CardNumber,
                        CardHolderName = model.CardName,
                        Cvc = model.CardCVC,
                        ExpireMonth = model.CardExpireMonth,
                        ExpireYear = model.CardExpireYear
                    };
                    user.Name = user.Name.Trim();
                    var name = user.Name;
                    var surName = "";
                    if (user.Name.Contains(" "))
                    {
                        name = user.Name.Substring(0, user.Name.LastIndexOf(' ') + 1);
                        surName = user.Name.Substring(user.Name.LastIndexOf(' ') + 1);
                    }
                    var buyer = new Buyer
                    {
                        Name = name,
                        Surname = surName ?? "",
                        Email = user.Email,
                        Id = user.ID.ToString(),
                        IdentityNumber = sellerInfo.TC,
                        GsmNumber = user.MobilePhone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""),
                        Ip = ip,
                        City = shippingAddress.City?.Name ?? shippingAddress.CityText,
                        Country = shippingAddress?.Country?.Name ?? "Türkiye",
                        RegistrationAddress = shippingAddressLine
                    };

                    var _shippingAddress = new Address
                    {
                        City = shippingAddress.City?.Name ?? shippingAddress.CityText,
                        Country = shippingAddress.Country?.Name ?? "Turkey",
                        ContactName = user.Name,
                        Description = shippingAddress.Address + " " + shippingAddress.District?.Name + " " + shippingAddress.City?.Name +
                            shippingAddress.CityText + " " + shippingAddress.Country?.Name,
                    };
                    var _invoiceAddress = new Address()
                    {
                        City = invoiceAddress.City?.Name ?? invoiceAddress.CityText,
                        Country = invoiceAddress.Country?.Name ?? "Turkey",
                        ContactName = user.Name,
                        Description = invoiceAddress.Address + " " + invoiceAddress.District?.Name + " " + shippingAddress.City?.Name
                            + invoiceAddress.CityText + " " + invoiceAddress.Country?.Name,
                    };

                    var securePayment = IyzicoService.SecurePayment(paymentRequest.ID, totalPrice, card, buyer,
                        _shippingAddress,
                        _invoiceAddress, ad.ID, "Checkout/SecurePaymentCallback", sellerInfo.IyzicoSubMerchantKey,
                        subMerchantPrice.ToString("0.##"),
                        model.InstallmentNumber, ad.Title, ad.ParentCategorySlugTr, ad.SubCategorySlugTr);
                    if (securePayment.Status != "success")
                    {
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme hatası." + securePayment.ErrorMessage,
                            "Secure Payment Error: " + securePayment.ErrorMessage, lang);

                        LogInsert(getUserId + " idli kullanıcı " + model.id + " ürünü almaya çalışırken güvenli ödeme hatası aldı. " + securePayment.ErrorMessage, Enums.LogType.Payment, GetLoginID(), model.id);

                        return Redirect("/");
                    }
                    LogInsert(user.Name + " kullanıcısı " + ad.Title + " ürününü almak için güvenli ödeme sayfasına yönlendirildi.", Enums.LogType.Payment, GetLoginID(), model.id);

                    return View("PayRedirect", securePayment.HtmlContent);
                }
                catch (Exception e)
                {
                    LogInsert(GetLoginID() + " idli kullanıcı " + model.id + " ürünü almaya çalışırken " + e.Message + " hatasını aldı.", Enums.LogType.Payment, GetLoginID(), model.id);
                    return Redirect("/");
                }

            }
        }


        [Route("Checkout/SecurePaymentCallback")]
        public IActionResult SecurePaymentCallback(Iyzico3dCallback callback)
        {

            try
            {
                var lang = GetLang();
                var route = Constants.GetURL((int)Enums.Routing.CariHesabim, lang);
                var getUserId = GetLoginID();
                if (string.IsNullOrEmpty(callback.PaymentId) || callback.PaymentId == "0")
                {
                    Notification = new UiMessage(NotyType.error, new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                        10000);
                    if (callback.Status != null && callback.PaymentId != null)
                    {
                        LogInsert(getUserId + " idli kullanıcı  satın alırken 3D Güvenlik hatasını aldı." + callback.Status, Enums.LogType.Payment, GetLoginID(), int.Parse(callback.PaymentId));

                    }
                    else
                    {
                        LogInsert(callback.ConversationId + " idli  ödeme (Conversation) satın alırken 3D Güvenlik hatasını aldı.", Enums.LogType.Payment, GetLoginID());

                    }
                    return Redirect(route);
                }
                var request = new CreateThreedsPaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = callback.ConversationId.ToString(),
                    PaymentId = callback.PaymentId,
                    ConversationData = callback.ConversationData
                };

                var threedsPayment = ThreedsPayment.Create(request, IyzicoService.GetOptions());
                if (threedsPayment.Status == "success")
                {
                    using (var mailing = new MailingService())
                    using (var adService = new AdvertService())
                    using (var uService = new UserService())
                    using (var notifService = new NotificationService())
                    using (var service = new PaymentRequestService())
                    {
                        var payReq = service.Get(Convert.ToInt32(callback.ConversationId));
                        if (payReq != null)
                        {
                            payReq.PaymentId = threedsPayment.PaymentId;
                            payReq.ResponseDate = DateTime.Now;
                            payReq.IsSuccess = true;
                            payReq.PaymentTransactionID = threedsPayment.PaymentItems?.First().PaymentTransactionId;
                            var update = service.Update(payReq);
                            if (update)
                            {
                                var ad = adService.Get(payReq.AdvertID);
                                if (ad != null)
                                {
                                    ad.StockAmount = ad.StockAmount - payReq.Amount;
                                    ad.IsActive = (ad.StockAmount > 0);
                                    adService.Update(ad);
                                }

                                var seller = uService.Get(payReq.SellerID);
                                var buyer = uService.Get(payReq.UserID);
                                var sellerInfo = uService.GetSecurePaymentDetail(seller.ID);

                                SendSaleMail2Seller(seller, buyer, ad, payReq.InvoiceAddress, payReq.Amount, payReq.ID);
                                SendSaleMail2Buyer(seller, buyer, ad, payReq.ShippingAddress, payReq.Amount, payReq.ID);

                                var notif = new Notification()
                                {
                                    UserID = seller.ID,
                                    CreatedDate = DateTime.Now,
                                    TypeID = Enums.NotificationType.IlanSatildi,
                                    Message = new Localization().Get(
                                    "Sattığınız " + ad.ID + " nolu ürünün ödemesi yapıldı, kargoya vermeniz gerekiyor.",
                                    "Item " + ad.ID + " has been sold, you need to shipping to buyer.", seller.LanguageID),
                                    Image = ad.Thumbnail,
                                    Url = Constants.GetURL((int)Enums.Routing.CariHesabim, seller.LanguageID),
                                    SenderUserID = buyer.ID
                                };
                                notifService.Insert(notif);
                                var notifyBuyer = new Notification
                                {
                                    UserID = buyer.ID,
                                    CreatedDate = DateTime.Now,
                                    Url = Constants.GetURL((int)Enums.Routing.CariHesabim, seller.LanguageID),
                                    TypeID = Enums.NotificationType.SistemMesaji,
                                    Message = new Localization().Get("Audiophile'dan bir ürün satın aldınız. Alıcının kargolaması bekleniyor.", "You purchased a product and waiting for delivery", lang)
                                };
                                notifService.Insert(notifyBuyer);
                                LogInsert(getUserId + " idli kullanıcı " + ad.ID + " idli ürünün ödeme işlemi tamamlandı.", Enums.LogType.Payment, GetLoginID(), ad.ID, null, null, null);

                                Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı", "Payment completed", lang),
                                    10000);
                                return Redirect(route);
                            }
                        }
                        Notification = new UiMessage(NotyType.success, new Localization().Get(
                                "Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin."
                                , "Payment completed but could not be saved in the system. Please contact the site administration.", lang),
                            10000);
                        LogInsert(getUserId + " idli kullanıcı " + callback.PaymentId + " ödeme idli alırken Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin hatasını aldı", Enums.LogType.Payment, GetLoginID(), int.Parse(callback.PaymentId));

                        return Redirect(route);
                    }

                }

                Notification = new UiMessage(NotyType.error, threedsPayment.ErrorMessage, 10000);
                LogInsert(getUserId + " idli kullanıcı " + callback.PaymentId + " ödeme idli ürünü alırken " + threedsPayment.ErrorMessage + " hatasını aldı.", Enums.LogType.Payment, GetLoginID(), int.Parse(callback.PaymentId), null, null, null);

                return Redirect(route);

            }
            catch (Exception e)
            {
                return Redirect("/");
            }



        }
        private void SendSaleMail2Seller(User seller, User buyer, Advert ad, string payReqInvoiceAddress, int payReqAmount, int id)
        {
            using (var mail = new MailingService())
            using (var setting = new SystemSettingService())
            using (var text = new TextService())
            {
                var day = setting.GetSystemSettings()
                    .FirstOrDefault(p => p.Name == Enums.SystemSettingName.OtomatikOdemeOnayi)
                    ?.Value;
                var lang = GetLang();
                var content = text.GetContent(Enums.Texts.SaticiyaUrunSatildi, lang);
                var html = content.TextContent;
                var title = content.Name;
                var replacedHtml = html.Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@urunAdi", ad.Title)
                    .Replace("@urunAdet", payReqAmount.ToString())
                    .Replace("@siparisNo", id.ToString())
                    .Replace("@aliciAdSoyad", buyer.Name)
                    .Replace("@aliciMail", buyer.Email)
                    .Replace("@aliciTel", buyer.MobilePhone)
                    .Replace("@aliciAdres", payReqInvoiceAddress)
                    .Replace("@onaySuresi", day)
                    .Replace("@prId", id.ToString());
                mail.Send(replacedHtml, seller.Email, seller.Name, title);
            }
        }
        private void SendSaleMail2Buyer(User seller, User buyer, Advert ad, string payReqShippingAddress,
            int reqAmount,
            int id)
        {
            using (var mail = new MailingService())
            using (var text = new TextService())
            {
                var lang = GetLang();
                var content = text.GetContent(Enums.Texts.AliciyaUrunSatinAldiniz, lang);
                var html = content.TextContent;
                var title = content.Name;
                var replacedHtml = html.Replace("@aliciAdSoyad", buyer.Name)
                    .Replace("@urunAdi", ad.Title)
                    .Replace("@urunAdet", reqAmount.ToString())
                    .Replace("@siparisNo", id.ToString())
                    .Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@saticiMail", seller.Email)
                    .Replace("@saticiTel", seller.MobilePhone)
                    .Replace("@saticiAdres", payReqShippingAddress)
                    .Replace("@prId", id.ToString());
                mail.Send(replacedHtml, buyer.Email, buyer.Name, title);
            }
        }


        public PartialViewResult GetInstallmentOptions(string cardNumber, string totalPrice, int id)
        {
            if (cardNumber == null || cardNumber.Length < 6)
                return null;

            var result = IyzicoService.GetInstallmentInfoAllBanks(cardNumber.Substring(0, 6), totalPrice);
            if (result.IsSuccess == false)
                return null;

            var data = result.Data.InstallmentDetails.First();
            var model = new InstallmentPrice
            {
                BankName = data.BankName,
                CardName = data.CardFamilyName,
                Details = new List<InstallmentPriceDetail>()
            };
            using (var service = new AdvertService())
            {
                var ad = service.Get(id);
                if (ad == null)
                    return null;

                foreach (var installmentPrice in data.InstallmentPrices)
                {
                    if (ad.AvailableInstallments.Contains(installmentPrice.InstallmentNumber?.ToString()))
                    {
                        var detail = new InstallmentPriceDetail
                        {
                            TotalPrice = totalPrice,
                            InstallmentNumber = installmentPrice.InstallmentNumber,
                            Price = (double.Parse(totalPrice) / installmentPrice.InstallmentNumber)?.ToString("0.##")
                        };
                        model.Details.Add(detail);
                    }
                }
            }

            return PartialView("~/Views/Partials/CheckoutInstallmentOptions.cshtml", model);
        }
    }
}