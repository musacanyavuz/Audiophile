using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using Audiophile.Common;
using Audiophile.Models;

namespace Audiophile.Services
{
    public class MailingService : BaseService , IDisposable
    {
        //private const string baseUrl = "https://localhost:5001";
        private const string baseUrl = "https://www.audiophile.org";
        private const string username = "noreply@audiophile.org";
        private const string password = "Petek28051964SelamiKelle";
        private const string server = "srvm09.trwww.com";
        private const int port = 587;
        private const bool useSsl = false;
        
        private const string resetPasswordMessageTr = "Bir şifre sıfırlama bağlantısı oluşturuldu. Eğer bu bağlantıyı siz oluşturmadıysanız bu maili dikkate almayınız. Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayabilirsiniz.";
        private const string resetPasswordMessageEn = "A password reset link has been created. If you did not create this link, please ignore this mail. You can click the link below to reset your password.";
        
        private const string activationMessageTr = "Audiophile.org'a hoşgeldiniz. Audiophile.org'u kullanmaya başlamak için hesabınızı aktifleştirmeniz gerekiyor. Hesabınızı aktifleştirmek için aşağıdaki bağlantıya tıklayabilirsiniz.";
        private const string activationMessageEn = "Welcome to Audiophile.org. To start using Audiophile.org, you need to activate your account. You can click the link below to activate your account.";

        
        private string GetHtml(string file)
        {
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "MailTemplates", file);
                return File.ReadAllText(path);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public bool SendResetPasswordMail(User user, string token)
        {
            var template = GetHtml("ResetPassword.html");
            var tran = new Localization();
            var title = "Audiophile.org | " + tran.Get("Şifremi Sıfırla", "Reset Password", user.LanguageID);
            
            var hello = tran.Get("Merhaba ", "Hello ", user.LanguageID) + " " + user.Name + " ("+user.UserName+"),";
            
            var message = tran.Get(resetPasswordMessageTr, resetPasswordMessageEn, user.LanguageID);  
            var linkText =  tran.Get("Şifremi Sıfırla", "Reset Password", user.LanguageID);
            var link = baseUrl + Constants.GetURL((int) Enums.Routing.SifremiSifirla, user.LanguageID) + "/" + token;
            // baseurl/SifremiSifirla/xxxx  ///  baseurl/ResetPassword/xxxx
            
            var body = template.Replace("@title", title).Replace("@text", message).Replace("@linkhref", link)
                .Replace("@linktext", linkText).Replace("@hello",hello);

            return Send(body, user.Email, user.Name, title);
        }
        
        public bool SendActivationMail(User user)
        {
            var template = GetHtml("ResetPassword.html");
            var tran = new Localization();
            var title = "Audiophile.org | " + tran.Get("Hesabınızı Aktifleştirin", "Account Activation", user.LanguageID);
            var hello = tran.Get("Merhaba ", "Hello ", user.LanguageID) + " " + user.Name + ",";
            var message = tran.Get(activationMessageTr, activationMessageEn, user.LanguageID);  
            var linkText =  tran.Get("Hesabımı Aktifleştir", "Activation", user.LanguageID);
            var link = baseUrl + Constants.GetURL((int) Enums.Routing.HesabimiAktiflestir, user.LanguageID) + "/" + user.Token;
            
            var body = template.Replace("@title", title).Replace("@text", message).Replace("@linkhref", link)
                .Replace("@linktext", linkText).Replace("@hello",hello);

            return Send(body, user.Email, user.Name, title);
        }
        
        public bool SendSaleMail2Seller(User user, User buyer, Advert ad, string shippingAddress, string invoiceAddress, int amount)
        {
            var template = GetHtml("ResetPassword.html");
            var tran = new Localization();
            var title = "Audiophile.org | " + tran.Get("İlanınızdaki Ürün Satıldı", "A Product Has Been Sold", user.LanguageID);
            var hello = tran.Get("Merhaba ", "Hello ", user.LanguageID) + " " + user.Name + ",";

            var urlTr = baseUrl+"/Ilan/"+ad.ParentCategorySlugTr
                        +"/"+Localization.Slug(ad.Title)+"/"+ad.ID;
            var urlEn = baseUrl+"/Advert/"+ad.ParentCategorySlugEn
                        +"/"+Localization.Slug(ad.Title)+"/"+ad.ID;
            var cargoInfo = baseUrl + Constants.GetURL((int) Enums.Routing.CariHesabim, 1);
            
            var mTr = "www.audiophile.org da ilana çıkmış olduğunuz ürününüz satılmıştır. <br/>" +
                      "<a href=\""+urlTr+"\">"+urlTr+"</a> <br/>" +
                      "Öncelikle ürünü kargolayıp kargo bilgilerini girmeniz gerekmektedir. <br/>" +
                      "Kargo Bilgisi Girişi: <a href=\""+cargoInfo+"\">"+cargoInfo+"</a> <br/>" +
                      "Satılan Ürün: "+ad.Title+ " "+amount+" adet <br/> " +
                      "Alıcı Bilgileri: <br/> " +
                      "Kullanıcı: #"+buyer.ID+" "+buyer.Name+" <br/>" +
                      "İletişim: "+buyer.Email +" - "+buyer.MobilePhone+"<br/>" +
                      "Kargo Adresi: "+shippingAddress+" <br/>" +
                      "Fatura Adresi: "+invoiceAddress;
            
            cargoInfo = baseUrl + Constants.GetURL((int) Enums.Routing.CariHesabim, 2);
            var mEn = "Your product on the website www.audiophile.org has been sold. <br/>" +
                      "<a href=\""+urlEn+"\">"+urlEn+"</a> <br/>" +
                      "First, you must enter shipping information after shipping the product. <br/>" +
                      "Entry Cargo Info: <a href=\""+cargoInfo+"\">"+cargoInfo+"</a> <br/>" +
                      "Product: "+ad.Title+" "+amount+" pieces <br/> " +
                      "Buyer Information: <br/> " +
                      "User: #"+buyer.ID+" "+buyer.Name+" <br/>" +
                      "Contact: "+buyer.Email +" - "+buyer.MobilePhone+"<br/>" +
                      "Cargo Address: "+shippingAddress+" <br/>" +
                      "Invoice Address: "+invoiceAddress;
            var message = tran.Get(mTr, mEn, user.LanguageID);
            
            var linkText = "www.audiophile.org";
            var link = baseUrl;
            
            var body = template.Replace("@title", title).Replace("@text", message).Replace("@linkhref", link)
                .Replace("@linktext", linkText).Replace("@hello",hello);

            return Send(body, user.Email, user.Name, title);
        }
        
        public bool SendSaleMail2Buyer(User user, User seller, Advert ad, int amount,string address)
        {
            var template = GetHtml("ResetPassword.html");
            var tran = new Localization();
            var title = "Audiophile.org | " + tran.Get("Bir ürün satın aldınız", "You have purchased a product", user.LanguageID);
            var hello = tran.Get("Merhaba ", "Hello ", user.LanguageID) + " " + user.Name + ",";

            var urlTr = baseUrl+"/Ilan/"+ad.ParentCategorySlugTr
                        +"/"+Localization.Slug(ad.Title)+"/"+ad.ID;
            var urlEn = baseUrl+"/Advert/"+ad.ParentCategorySlugEn
                        +"/"+Localization.Slug(ad.Title)+"/"+ad.ID;
            var cargoInfo = baseUrl + Constants.GetURL((int) Enums.Routing.CariHesabim, 1);

            var mTr = "www.audiophile.org dan bir ürün satın aldınız. <br/>" +
                      "<a href=\"" + urlTr + "\">" + urlTr + "</a> <br/>" +

                      "Alınan Ürün: " + ad.Title + " " + amount + " adet <br/> " +
                      "Satıcı Bilgileri: <br/> " +
                      "Kullanıcı: #" + seller.ID + " " + seller.Name + " <br/>" +
                      "İletişim: " + seller.Email + " - " + seller.MobilePhone + "<br/>" +
                      "Adresi: " + address + " <br/>";
            
            cargoInfo = baseUrl + Constants.GetURL((int) Enums.Routing.CariHesabim, 2);
            var mEn = "You have purchased a product from www.audiophile.org. <br/>" +
                      "<a href=\"" + urlEn + "\">" + urlEn + "</a> <br/>" +

                      "Product: " + ad.Title + " " + amount + " pieces <br/> " +
                      "Seller Information: <br/> " +
                      "User: #" + seller.ID + " " + seller.Name + " <br/>" +
                      "Contact: " + seller.Email + " - " + seller.MobilePhone + "<br/>" +
                      "Address: " + address + " <br/>";
            var message = tran.Get(mTr, mEn, user.LanguageID);
            
            var linkText = "www.audiophile.org";
            var link = baseUrl;
            
            var body = template.Replace("@title", title).Replace("@text", message).Replace("@linkhref", link)
                .Replace("@linktext", linkText).Replace("@hello",hello);

            return Send(body, user.Email, user.Name, title);
        }
        
        public bool SendNotificationMail(User user, Notification notification)
        {
            var template = GetHtml("ResetPassword.html");
            var tran = new Localization();
            var title = "Audiophile.org | " + tran.Get("Bir Bildiriminiz Var", "You Have a Notification", user.LanguageID);
            var hello = tran.Get("Merhaba ", "Hello ", user.LanguageID) + " " + user.Name + ", ";
            var message = notification.Message;
            
            var linkText =  tran.Get("Bildirimi İncele", "Notification Details", user.LanguageID);
            
            var link = baseUrl + notification.Url;
            
            var body = template.Replace("@title", title).Replace("@text", message).Replace("@linkhref", link)
                .Replace("@linktext", linkText).Replace("@hello",hello);

            return Send(body, user.Email, user.Name, title);
        }

        public void SendMail2Admins(string mailAddresses, string title, string message)
        {
            try
            { 
                var fromAddress = new MailAddress(username, "Audiophile.org");
                var smtp = new SmtpClient
                {
                    Host = server,
                    Port = port,
                    EnableSsl = useSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, password)
                };
                var mail = new MailMessage()
                {
                    From = fromAddress,
                    Subject = title,
                    Body = message,
                    IsBodyHtml = true
                };
                foreach (var mailAddress in mailAddresses.Split(';'))
                {
                     mail.To.Add(mailAddress);
                }
                   
                smtp.Send(mail);
            }
            catch (Exception e)
            {
            }
        }
        
        public bool Send(string body, string toMail, string toName, string title)
        {
            try
            {
                var toAddress = new MailAddress(toMail, toName);
                var fromAddress = new MailAddress(username, "Audiophile.org");
                var smtp = new SmtpClient
                {
                    Host = server,
                    Port = port,
                    EnableSsl = useSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, password)
                };
                using (var mail = new MailMessage(fromAddress, toAddress)
                {
                    Subject = title,
                    Body = body,
                    IsBodyHtml = true
                })
                smtp.Send(mail);
                return true;
            }
            catch (Exception e)
            {
                Log(new Log
                {
                    Function = "MailingService.Send",
                    CreatedDate = DateTime.Now,
                    Message = e.Message,
                    Detail = e.ToString(),
                    IsError = true
                   
                });
                return false;
            }
        }

        public void Dispose()
        {
            
        }
    }
}