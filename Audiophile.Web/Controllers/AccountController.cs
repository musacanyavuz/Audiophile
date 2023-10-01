using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Audiophile.Common;
using Audiophile.Common.Extensions;
using Audiophile.Common.Iyzico;
using Audiophile.Models;
using Audiophile.Services;
using Audiophile.Web.Helpers;
using Audiophile.Web.ViewModels;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Utilities;
using Remotion.Linq.Clauses.ResultOperators;

namespace Audiophile.Web.Controllers
{
    public class AccountController : BaseController
    {
       
        string stTr = "Facebook ile giriş işlemi için e-posta erişim izni vermelisiniz ! ";
        string stEn = "Email address is mandatory to complete login via facebook  !";
       
        #region Login

        [Route("GirisYap")]
        [Route("Login")]
        public IActionResult Login()
        {
            ViewBag.FacebookLoginError = new Localization().Get(stTr, stEn, GetLang());           
            return View();
        }

        [HttpPost]
        [Route("GirisYap")]
        [Route("Login")]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe)
        {
            var lang = GetLang(false);
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Notification = new UiMessage(NotyType.error, "Kullanıcı adı ya da şifre geçersiz.",
                "Username or password is not valid.", lang);
                return RedirectToAction("Login");
            }
            using (var service = new UserService())
            {
                // var pass = Encryptor.DecryptData("f3DUsWKsCyWLjS4DCp3WuA==");
                password = Encryptor.EncryptData(password);
                var user = service.Get(username, password);
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Hatalı kullanıcı adı ya da şifre",
                        "Incorrect username or password", lang);
                    return RedirectToAction("Login");
                }
                else if (!user.IsActive)
                {
                    Notification = new UiMessage(NotyType.error, "E-posta adresinize gönderilen aktifleştirme bağlantısı ile hesabınızı aktifleştirmelisiniz.",
                        "You must activate your account with the activation link sent to your e-mail address.", lang);
                    return RedirectToAction("Login");
                }
                await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr", rememberMe: rememberMe);
                SetLang(user.LanguageID == 1 ? "tr" : "en");
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task AddCookieAuth(string userName, string name, string email, string photo, string role, string userId, string lang, bool rememberMe = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.UserData, userId),
                new Claim(ClaimTypes.Locality, lang),
                new Claim(ClaimTypes.Thumbprint, photo ?? "/Content/img/avatar-no-image.png"),
                new Claim("Username",userName)
            };
            var identity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
           
            var authProperties = new AuthenticationProperties
            {
                
                ExpiresUtc = DateTime.UtcNow.AddDays(7),
                IsPersistent = true,
            };
            await HttpContext.SignInAsync(
                scheme: CookieAuthenticationDefaults.AuthenticationScheme,
                principal: principal, properties: authProperties
            );

        }

        [Route("CikisYap")]
        [Route("SignOut")]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Route("SifremiUnuttum")]
        [Route("ForgottenPassword")]
        public IActionResult ForgottenPassword()
        {
            GetLang();
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> FBLogin(short oauth_Provider, ExternalLoginViewModel userData)
        {
            using (var service = new UserService())
            {
                var userRegisteredSystem = service.Get(userData.Email);
                if (userRegisteredSystem != null)
                {
                    userRegisteredSystem.OAuth_Provider = oauth_Provider;
                    userRegisteredSystem.OAuth_Uid = userData.OAuth_Uid;
                    await AddCookieAuth(userRegisteredSystem.UserName, userRegisteredSystem.Name, userRegisteredSystem.Email, userRegisteredSystem.ProfilePicture, userRegisteredSystem.Role, userRegisteredSystem.ID.ToString(), userRegisteredSystem.LanguageID == 2 ? "en" : "tr");
                    SetLang(userRegisteredSystem.LanguageID == 1 ? "tr" : "en");
                    return Json(service.Update(userRegisteredSystem));
                }
                else
                {
                    var user = service.GetFromExternalLoginId(oauth_Provider, userData.OAuth_Uid);

                    if (user != null)
                    {
                        await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr");
                        SetLang(user.LanguageID == 1 ? "tr" : "en");
                        return Json(true);
                    }
                    else
                    {
                        user = new User
                        {
                            Name = userData.Name,
                            Email = userData.Email,
                            ProfilePicture = userData.PictureUrl,
                            OAuth_Provider = oauth_Provider,
                            OAuth_Uid = userData.OAuth_Uid,
                            Role = "User",
                            IsActive = true,
                            LanguageID = 1,
                            UserName = userData.Email,
                            CreatedDate = DateTime.Now
                        };
                        var result = service.Insert(user);
                        await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr");
                        SetLang(user.LanguageID == 1 ? "tr" : "en");
                        return Json(result);
                    }
                }
            }
        }
        #endregion

        #region ResetPassword

        [Route("SifremiUnuttum")]
        [Route("ForgottenPassword")]
        [HttpPost]
        public IActionResult ForgottenPassword(string user)
        {
            var lang = GetLang();
            using (var mailing = new MailingService())
            using (var service = new UserService())
            using (var textService = new TextService())
            {
                var account = service.Get(user);
                if (account == null)
                {
                    var title = new Localization().Get("Hata", "Error", lang);
                    var message = new Localization().Get("Kullanıcı Bulunamadı. Kullanıcı Adı veya E-Posta Adresinizi Doğru Yazdığınızdan Emin Olunuz. ", "User not found. Please be sure to write your username or e-mail address correctly.", lang);
                    return View(new UiMessage { Message = message, Type = NotyType.error });
                }
                var token = Encryptor.GenerateToken();
                account.Token = token;
                var update = service.Update(account);
                var msgTr = update ? Constants.messageSuccessTr : Constants.messageDangerTr;
                var msgEn = update ? Constants.messageSuccessEn : Constants.messageDangerEn;
                if (!update)
                {
                    return View(new UiMessage
                    {
                        Type = NotyType.error,
                        Message = new Localization().Get(msgTr, msgEn, lang)
                    });
                }


                var content = textService.GetContent(387, lang);
                var link = Constants.GetURL((int)Enums.Routing.SifremiSifirla, account.LanguageID) + "/" + token;
                content.TextContent = content.TextContent.Replace("@adSoyad", account.Name).Replace("@Email", account.Email).Replace("@link", link);
                var send = mailing.Send(content.TextContent, account.Email, account.Name, content.Name);
                msgTr = send ? Constants.messageSuccessTr : Constants.messageDangerTr;
                msgEn = send ? Constants.messageSuccessEn : Constants.messageDangerEn;
                return View(new UiMessage
                {
                    Type = send ? NotyType.success : NotyType.error,
                    Message = new Localization().Get(msgTr, msgEn, lang)
                });
            }
        }

        [Route("SifremiSifirla/{token}")]
        [Route("ResetPassword/{token}")]
        public IActionResult ResetPassword(string token)
        {
            GetLang();
            try
            {
                using (var service = new UserService())
                {
                    User user = null;
                    if (string.IsNullOrEmpty(token))
                        return RedirectToAction("Index", "Home");

                    user = token.Length < 10 ? service.Get(int.Parse(token)) : service.GetFromToken(token);
                    if (user != null)
                        return View(user);
                }
            }
            catch (Exception)
            { }
            return RedirectToAction("Index", "Home");
        }

        [Route("SifremiSifirla/{token}")]
        [Route("ResetPassword/{token}")]
        [HttpPost]
        public IActionResult ResetPassword(string token, string password, string password2)
        {
            var lang = GetLang();
            if (password == null || password2 == null)
            {
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("Şifre geçersiz.", "Password is invalid.", lang) });
                return RedirectToAction("ResetPassword", new { token });
            }
            if (password != password2)
            {
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("Şifreler aynı değil.", "Please check if passwords are not the same.", lang) });
                return RedirectToAction("ResetPassword", new { token });
            }
            if (password.Length < 6)
            {
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = new Localization().Get("Şifreniz en az 6 karakter olmalıdır.", "Your password must be at least 6 characters.", lang) });
                return RedirectToAction("ResetPassword", new { token });
            }

            using (var service = new UserService())
            {
                var user = service.GetFromToken(token);
                password = Encryptor.EncryptData(password);
                user.Password = password;
                user.Token = null;
                var update = service.Update(user);
                if (update)
                {
                    TempData.Put("UiMessage", new UiMessage
                    {
                        Class = "success",
                        Message = new Localization().Get("Şifreniz güncellendi.", "Your password has been updated.",
                            lang)
                    });
                    return RedirectToAction("ResetPassword", new { token = user.ID });
                }
                else
                {
                    TempData.Put("UiMessage", new UiMessage
                    {
                        Class = "danger",
                        Message = new Localization().Get("Şifre güncelleme işlemi başarısız oldu.", "Password update failed.",
                            lang)
                    });
                }
            }

            return RedirectToAction("ResetPassword", new { token });
        }

        #endregion

        #region Register

        [Route("UyeOl")]
        [Route("Register")]
        public IActionResult Register()
        {
            GetLang();
            using (var publicService = new PublicService())
            {
                var model = new RegisterViewModel
                {
                    Countries = publicService.GetCountries(),
                    Cities = publicService.GetCities(),
                    Districts = publicService.GetDistricts()
                };
                return View(model);
            }
        }

        [Route("UyeOl")]
        [Route("Register")]
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
           
                var t = new Localization();
            var lang = GetLang();
            var match = Regex.Match(user.MobilePhone, @"^[0-9\s]*$", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = t.Get(Messages.RegisterPhoneFail_tr, Messages.RegisterPhoneFail_en, lang) });
                return RedirectToAction("Register");
            }
            bool validation = false;
           
                var response = Request.Form["g-recaptcha-response"];
                const string secret = Constants.RecaptchaSecretKey;
                var client = new System.Net.WebClient();
                var reply =
                    client.DownloadString(
                        $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={response}");
                var captchaResponse = JsonConvert.DeserializeObject<CaptchaResponse>(reply);
                validation = captchaResponse.Success;
            
            if (!validation)
            {               
                TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = t.Get(Messages.RegisterReCaptchaError_tr, Messages.RegisterReCaptchaError_en, lang) });
                return RedirectToAction("Register");
            }
                
            
            using (var service = new UserService())
            {
                var usernameIsUsable = service.UsernameIsUsable(user.UserName, GetLoginID());
                var emailIsUsable = service.EmailIsUsable(user.Email, GetLoginID());
                if (!usernameIsUsable || !emailIsUsable)
                {
                    TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = t.Get(Messages.RegisterUniqFail_tr, Messages.RegisterUniqFail_en, lang) });
                    return RedirectToAction("Register");
                }
                user.Role = "User";
                user.CreatedDate = DateTime.Now;

                user.Token = Encryptor.GenerateToken();
                user.Password = Encryptor.EncryptData(user.Password);
                var insert = service.Insert(user);
                if (!insert)
                {
                    TempData.Put("UiMessage", new UiMessage { Class = "danger", Message = t.Get(Messages.InsertError_tr, Messages.InsertError_en, lang) });
                    return RedirectToAction("Register");
                }
                //TEST
                if (user.ProfilePictureFile != null && user.ProfilePictureFile.Length > 0)
                {
                    var fileName = user.UserName + "_" + user.ID;
                    var extension = System.IO.Path.GetExtension(user.ProfilePictureFile.FileName);
                    fileName += extension;
                    var filePath = new FileService().FileUpload(GetLoginID(), user.ProfilePictureFile, "/Upload/Users/", fileName);
                    new ImageService().Resize(filePath, 300);
                    user.ProfilePicture = "/Upload/Users/" + fileName;
                    service.Update(user);
                }
                var mail = SendActivationMail(user);
                //await AddCookieAuth(user.Name, user.Role, user.ID.ToString());
                TempData.Put("UiMessage",
                    mail
                        ? new UiMessage
                        {
                            Class = "success",
                            Message = t.Get(Messages.RegisterSuccess_tr, Messages.RegisterSuccess_en, lang)
                        }
                        : new UiMessage
                        {
                            Class = "danger",
                            Message = t.Get(Messages.RegisterMailError_tr, Messages.RegisterMailError_en, lang)
                        });
                var route = user.LanguageID == 1 ? "/Bildirim" : "/Notification";
                return Redirect(route);
            }
        }

        private bool SendActivationMail(User user)
        {
            var lang = GetLang();
            var tran = new Localization();
            using (var mailing = new MailingService())
            using (var content = new TextService())
            {
                var link = Constants.GetURL((int)Enums.Routing.HesabimiAktiflestir, user.LanguageID) + "/" + user.Token;
                var mailHtml = content.GetText(Enums.Texts.MailAktivasyon, lang);
                mailHtml = mailHtml
                    .Replace("@adSoyad", $"{user.Name}")
                    .Replace("@link", link);
                var title = $"Audiophile.org | {tran.Get("Hesabınızı Aktifleştirin", "Account Activation", lang)}";
                return mailing.Send(mailHtml, user.Email, user.Name, title);
            }
        }

        [Route("HesabimiAktiflestir/{token}")]
        [Route("AccountActivation/{token}")]
        public async Task<IActionResult> Activation(string token)
        {
            GetLang();
            try
            {
                using (var service = new UserService())
                {
                    User user = null;
                    if (string.IsNullOrEmpty(token))
                        return RedirectToAction("Index", "Home");

                    user = service.GetFromToken(token);
                    if (user == null)
                        return RedirectToAction("Index", "Home");
                    user.IsActive = true;
                    user.Token = null;
                    var update = service.Update(user);
                    if (update)
                    {
                        await AddCookieAuth(user.UserName, user.Name, user.Email, user.ProfilePicture, user.Role, user.ID.ToString(), user.LanguageID == 2 ? "en" : "tr");
                        TempData.Put("UiMessage", new UiMessage { Class = "success", Message = new Localization().Get("Hesabınız aktifleştirildi.", "Your account has been activated.", user.LanguageID) });
                        ActivationApprove(user);
                    }
                    else
                    {
                        TempData.Put("UiMessage", new UiMessage { Class = "success", Message = new Localization().Get("Hesap aktifleştirme işlemi başarısız oldu.", "Account activation failed.", user.LanguageID) });
                    }
                    return Redirect(user.LanguageID == 1 ? "/Bildirim" : "/Notification");
                }
            }
            catch (Exception)
            { }
            return RedirectToAction("Index", "Home");
        }

        private static void ActivationApprove(User user)
        {
            using (var mailing = new MailingService())
            using (var text = new TextService())
            {
                var content = text.GetContent(Enums.Texts.AktivasyonOnay, user.LanguageID);
                var html = content.TextContent.Replace("@adSoyad", user.Name);
                mailing.Send(html, user.Email, user.Name, content.Name);
            }
        }


        [HttpPost]
        public JsonResult UserNameIsUsable(string username)
        {
            var t = new Localization();
            var lang = GetLang();           

            if (string.IsNullOrEmpty(username))
                return Json(new
                { isSuccess = false, message = t.Get("Geçersiz kullanıcı adı.", "Invalid user name.", lang) });
            using (var service = new UserService())
            {
                var usernameIsUsable = service.UsernameIsUsable(username, GetLoginID());
                if (!usernameIsUsable)
                    return Json(new
                    { isSuccess = false, message = t.Get(Messages.RegisterUserNameFail_tr, Messages.RegisterUserNameFail_en, lang) });
                return Json(new { isSuccess = true });
            }
        }

        [HttpPost]
        public JsonResult EmailIsUsable(string email)
        {
            var t = new Localization();
            var lang = GetLang();
            if (string.IsNullOrEmpty(email))
                return Json(new
                { isSuccess = false, message = t.Get("Geçersiz e-posta adresi.", "Invalid email.", lang) });
            using (var service = new UserService())
            {
                var usernameIsUsable = service.EmailIsUsable(email, GetLoginID());
                if (!usernameIsUsable)
                    return Json(new
                    { isSuccess = false, message = t.Get(Messages.RegisterEmailFail_tr, Messages.RegisterEmailFail_en, lang) });
                return Json(new { isSuccess = true });
            }
        }


        #endregion

        #region UyelikBilgilerim

        [Route("Hesabim/Uyelik-Bilgilerim")]
        [Route("MyAccount/Personal-Information")]
        [Authorize]
        public IActionResult PersonalInformation()
        {
            using (var publicService = new PublicService())
            using (var textService = new TextService())
            using (var service = new UserService())
            {
                var me = service.Get(GetLoginID());
                if (me == null)
                    return Redirect("/");
                if (me.MobilePhone.StartsWith("+"))
                {
                //    me.MobilePhoneDialCode = 
                }
                var model = new PersonalInformationViewModel
                {
                    User = me,
                    Cities = publicService.GetCities(),
                    Countries = publicService.GetCountries(),
                    Districts = publicService.GetDistricts(),
                    UserSecurePaymentDetail = service.GetSecurePaymentDetail(GetLoginID()),
                    PersonalWebSite  = textService.GetText(Enums.Texts.PersonalWebSite, GetLang()),

                };
                //if(model.User.CountryID != 1)
                //{
                //    model.Cities = new List<City>();
                //    model.Districts = new List<District>();
                //}
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("Hesabim/Uyelik-Bilgilerim")]
        [Route("MyAccount/Personal-Information")]
        public async Task<IActionResult> PersonalInformation(User user)
        {
            var route = Request.Path;
            var userId = GetLoginID();
            if (user == null)
                return Redirect(route);

            using (var service = new UserService())
            {
                var u = service.Get(userId);
                if (u == null) return Redirect(route);

                if (u.Email.ToLower() != user.Email?.ToLower())
                {
                    var emailIsAvailable = service.EmailIsUsable(user.Email, userId);
                    if (!emailIsAvailable)
                    {
                        Notification = new UiMessage(NotyType.error, "E-mail adresi geçersiz.", "E-Mail address is fail.", user.LanguageID);
                        return Redirect(route);
                    }
                }

                if (u.UserName.ToLower() != user.UserName?.ToLower())
                {
                    var usernameIsAvailable = service.UsernameIsUsable(user.UserName, userId);
                    if (!usernameIsAvailable)
                    {
                        Notification = new UiMessage(NotyType.error, "Kullanıcı adı adresi geçersiz.", "Username is fail.", user.LanguageID);
                        return Redirect(route);
                    }
                }

                user.Name = user.Name.Modify();
                user.UserName = user.UserName;

                u.UserName = user.UserName;
                u.Name = user.Name;
                u.Email = user.Email;
                if (user.MobilePhone != null && user.MobilePhoneDialCode > 0)
                u.MobilePhone =(!user.MobilePhone.StartsWith("+") )?  $"+{user.MobilePhoneDialCode}{user.MobilePhone}" : user.MobilePhone;
                u.GenderID = user.GenderID;
                u.WebSite = user.WebSite;
                u.CountryID = user.CountryID;
                u.CityID = user.CityID;
                u.DistrictID = user.DistrictID;
                u.LanguageID = user.LanguageID;    
                if(user.WorkPhone != null && user.WorkPhoneDialCode > 0)
                u.WorkPhone = (!user.WorkPhone.StartsWith("+")) ? $"+{user.WorkPhoneDialCode}{user.WorkPhone}" :user.WorkPhone ;                
                u.BirthDate = user.BirthDate;
                u.InMailing = user.InMailing;
                u.TC = string.IsNullOrEmpty(user.TC) ? u.TC : user.TC;
                u.About = user.About;
                var update = service.Update(u);
                if (!update)
                {
                    Notification = new UiMessage(NotyType.success, "Bilgileriniz güncellenirken bir hata oluştu.", "There was an error updating your information.", user.LanguageID);
                    return Redirect(route);
                }

                Notification = new UiMessage(NotyType.success, "Bilgileriniz güncellendi.", "Your user information has been updated.", user.LanguageID);
                HttpContext.Session.SetString("lang", user.LanguageID == 1 ? "tr":"en");
               
                if (user.ProfilePictureFile != null && user.ProfilePictureFile.Length > 0 && user.ProfilePictureFile.Length < 10000000)
                {
                    try
                    {
                        using (var fileService = new FileService())
                        {
                            var oldFileName = u.ProfilePicture.Replace("/Upload/Users/", "");
                            fileService.DeleteFile("/Upload/Users/", oldFileName);
                            var fileName = $"{u.UserName}_{u.ID}_{Guid.NewGuid()}.jpg";
                            var filePath = fileService.FileUpload(GetLoginID(), user.ProfilePictureFile, "/Upload/Users/", fileName);
                            new ImageService().Resize(filePath, 300);
                            u.ProfilePicture = "/Upload/Users/" + fileName;
                            var pp = service.Update(u);
                        }

                    }
                    catch (Exception e)
                    {
                    }
                }


            }

            return Redirect(route);
        }
        #endregion

        #region AdresBilgilerim

        [Route("Hesabim/Adres-Bilgilerim")]
        [Route("MyAccount/Postage-Address")]
        [Authorize]
        public IActionResult AddressInformation()
        {
            using (var service = new UserService())
            {
                var list = service.GetUserAddresses(GetLoginID());
                return View(list);
            }
        }

        [Route("Hesabim/Adres-Bilgilerim/Adres-Ekle")]
        [Route("MyAccount/Postage-Address/Add-Address")]
        [Authorize]
        public IActionResult AddAddress()
        {
            using (var service = new PublicService())
            {
                var model = new AddAddressViewModel
                {
                    Countries = service.GetCountries(),
                    Cities = service.GetCities(),
                    Districts = service.GetDistricts()
                };
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult SaveAddAddress(UserAddress userAddress)
        {
            var lang = GetLang();
            var route = Request.Path;
            if (userAddress == null)
                return Redirect(route);
            using (var service = new UserService())
            {
                userAddress.UserID = GetLoginID();
                if (userAddress.CountryID > 1)
                {
                    userAddress.CityID = 0;
                    userAddress.DistrictID = 0;
                }
                else
                    userAddress.CityText = null;

                var insert = service.InsertAddress(userAddress);
                if (insert)
                {
                    Notification = new UiMessage(NotyType.success, "Adres eklendi.", "Address has been added.", lang);
                    return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, lang));
                }

                Notification = new UiMessage(NotyType.error, "Adres eklenemedi.", "Address add failed.", lang);
                return Redirect(route);
            }

        }

        [Route("Hesabim/Adres-Bilgilerim/Adres-Duzenle/{id}")]
        [Route("MyAccount/Postage-Address/Edit-Address/{id}")]
        [Authorize]
        public IActionResult EditAddress(int id)
        {
            using (var addressService = new UserService())
            using (var service = new PublicService())
            {
                var address = addressService.GetUserAddress(GetLoginID(), id);
                if (address == null)
                    return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, GetLang(false)));
                var model = new EditAddressViewModel
                {
                    UserAddress = address,
                    Countries = service.GetCountries(),
                    Cities = service.GetCities(),
                    Districts = service.GetDistricts()
                };
                return View(model);
            }
        }

        [Route("Hesabim/Adres-Bilgilerim/Adres-Duzenle/{id}")]
        [Route("MyAccount/Postage-Address/Edit-Address/{id}")]
        [HttpPost]
        [Authorize]
        public IActionResult EditAddress(UserAddress userAddress)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var addressService = new UserService())
            using (var service = new PublicService())
            {
                var address = addressService.GetUserAddress(GetLoginID(), userAddress.ID);
                if (address == null)
                    return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, GetLang(false)));

                userAddress.UserID = GetLoginID();
                if (userAddress.CountryID > 1)
                {
                    userAddress.CityID = 0;
                    userAddress.DistrictID = 0;
                }
                else
                    userAddress.CityText = null;
                var update = addressService.UpdateAddress(userAddress);
                if (update)
                    Notification = new UiMessage(NotyType.success,
                        t.Get("Adres bilgileriniz güncellendi.", "Address has been updated.", lang));
                else
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Adres bilgileriniz güncellenemedi.", "Could not update your address information.", lang));

                return Redirect(Constants.GetURL((int)Enums.Routing.AdresBilgierim, GetLang(false)));
            }
        }

        [Authorize]
        [HttpPost]
        [Route("UserAddress/Delete")]
        public JsonResult Delete(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(GetLoginID());
                if (user == null)
                    return Json(new { isSuccess = false, message = new Localization().Get("Adres silme işlemi başarısız.", "Address delete failed.", user.LanguageID) });
                var delete = service.DeleteAddress(id, user.ID);
                if (delete)
                {
                    return Json(new
                    {
                        isSuccess = true,
                        message = new Localization().Get("Adres silindi.", "Address has been deleted.", user.LanguageID)
                    });
                }
                return Json(new
                {
                    isSuccess = false,
                    message = new Localization().Get("Adres silme işlemi başarısız.", "Address delete failed.", user.LanguageID)
                });
            }
        }

        #endregion

        #region SifremiDegistir

        [Route("Hesabim/Sifre-Degistir")]
        [Route("MyAccount/Change-Password")]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Route("Hesabim/Sifre-Degistir")]
        [Route("MyAccount/Change-Password")]
        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(string password, string newPassword, string newPassword2)
        {
            var lang = GetLang();
            var route = Request.Path;
            if (newPassword == null || newPassword != newPassword2)
            {
                Notification = new UiMessage(NotyType.error, "Şifreler aynı değil.", "Passwords are not the same.", lang);
                return Redirect(route);
            }
            if (newPassword.Length < 6)
            {
                Notification = new UiMessage(NotyType.error, "Şifreniz 6 karakter veya daha uzun olmalı.", "Your password must be 6 characters or longer.", lang);
                return Redirect(route);
            }
            using (var service = new UserService())
            {
                var user = service.Get(GetLoginID());
                if (user.Password == Encryptor.EncryptData(password))
                {
                    user.Password = Encryptor.EncryptData(newPassword);
                    var update = service.Update(user);
                    Notification = update ? new UiMessage(NotyType.success, "Şifreniz güncellendi.", "Your password has been updated.", lang) : new UiMessage(NotyType.error, "Şifre güncellenemedi.", "Password update fail.", lang);
                }
                else
                {
                    Notification = new UiMessage(NotyType.error, "Mevcut şifre doğru değil.", "Current password is not correct.", lang);
                }
            }
            return Redirect(route);
        }

        #endregion

        #region GuvenliOdemeBilgilerim

        [Authorize]
        public IActionResult SecurePayment1(UserSecurePaymentDetail userSecurePaymentDetail)
        {
            userSecurePaymentDetail.IBAN = userSecurePaymentDetail.IBAN.ToUpper().Replace(" ", string.Empty);
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.UyelikBilgilerim, lang);

            using (var service = new UserService())
            {

                userSecurePaymentDetail.UserID = GetLoginID();
                var details = service.GetSecurePaymentDetail(GetLoginID());
                var user = service.Get(GetLoginID());



                if (string.IsNullOrEmpty(user.TC) && string.IsNullOrEmpty(userSecurePaymentDetail.TC) == false)
                {
                    user.TC = userSecurePaymentDetail.TC;
                    service.Update(user);
                }
                else if (string.IsNullOrEmpty(user.TC) == false && string.IsNullOrEmpty(userSecurePaymentDetail.TC) == false)
                {
                    if (user.TC != userSecurePaymentDetail.TC)
                    {
                        user.TC = userSecurePaymentDetail.TC;
                        service.Update(user);
                    }
                }


                string operation;
                if (details == null)
                {
                    operation = "create";
                }
                else
                {

                    // if(details.Type!=(int)SubMerchantType.PERSONAL)
                    // {

                    //     details.OldIycizoId=details.ID;
                    //     var resultUpdate=service.UpdateSecurePaymentDetail(details);
                    //     if(!resultUpdate)
                    //     {
                    //         Notification = new UiMessage(NotyType.error, "Gerçek Şahıs bilgileriniz düzeltilebilmesi için site yönetimiyle irtibata geçiniz.", lang);
                    //           return Redirect(route);
                    //     }
                    // }

                    details.Name = userSecurePaymentDetail.Name;
                    details.Surname = userSecurePaymentDetail.Surname;
                    details.TC = userSecurePaymentDetail.TC;
                    if (userSecurePaymentDetail.MobilePhone != null && userSecurePaymentDetail.MobilePhoneDialCode > 0)
                    {
                        details.MobilePhone = (!userSecurePaymentDetail.MobilePhone.StartsWith("+")) ? $"+{userSecurePaymentDetail.MobilePhoneDialCode}{userSecurePaymentDetail.MobilePhone}" : userSecurePaymentDetail.MobilePhone;
                       // details.MobilePhone = userSecurePaymentDetail.MobilePhone;
                    }
                     
                    details.IBAN = userSecurePaymentDetail.IBAN.Replace(" ", "");
                    details.Email = userSecurePaymentDetail.Email;
                    details.InvoiceAddress = userSecurePaymentDetail.InvoiceAddress;
                    details.SenderAddress = userSecurePaymentDetail.SenderAddress;
                    details.Type = (int)SubMerchantType.PERSONAL;
                    operation = "update";


                }

                var type = SubMerchantType.PERSONAL;
                var request = new Iyzipay.Model.SubMerchant
                {
                    ConversationId = GetLoginID().ToString(),
                    SubMerchantExternalId = GetLoginID().ToString(),
                    SubMerchantType = type.ToString(),
                    Address = userSecurePaymentDetail.InvoiceAddress,
                    Email = userSecurePaymentDetail.Email,
                    GsmNumber = userSecurePaymentDetail.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                    Name = userSecurePaymentDetail.CompanyName,
                    Iban = userSecurePaymentDetail.IBAN?.Replace(" ", ""),
                    Locale = "TR",
                    Currency = "TRY",
                    ContactName = userSecurePaymentDetail.Name,
                    ContactSurname = userSecurePaymentDetail.Surname,
                    IdentityNumber = userSecurePaymentDetail.TC
                };


                if (operation == "create")
                {
                    var result = IyzicoService.CreateSubMerchant(request);
                    if (result.IsSuccess || result.ErrorCode == "2002")
                    {
                        details = userSecurePaymentDetail;
                        details.IyzicoSubMerchantKey = result.ErrorCode == "2002" ? IyzicoService.GetSubMerchantKey(request).Replace(" ", "").Replace("/r/n", "") : result.Data.SubMerchantKey.Replace(" ", "").Replace("/r/n", "");
                        details.CreatedDate = DateTime.Now;
                        var insert = service.InsertSecurePaymentDetail(details);

                        if (insert)
                        {
                            Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz bireysel kategorisinden kayıt edilmiştir.",
                            "Your secure payment information has been recorded in the individual category.", lang);
                            if (result.ErrorCode == "2002")
                            {
                                operation = "update";
                            }
                        }
                        else
                        {
                            Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                            "#2 Secure payment options save failed.", lang);
                        }
                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                        "#3 Secure payment options save failed." + result.Message, lang);
                    }

                }

                if (operation == "update")
                {
                    var result = IyzicoService.UpdateSubMerchant(request);
                    if (result.IsSuccess)
                    {
                        if (details == null)
                        {
                            Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                                "#2 Secure payment options save failed.", lang);
                            return Redirect(route);
                        }
                        details.IyzicoSubMerchantKey = IyzicoService.GetSubMerchantKey(request).Replace(" ", "").Replace("/r/n", "");
                        var update = service.UpdateSecurePaymentDetail(details);
                        if (update)
                        {
                            Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz bireysel kategorisinden kayıt edilmiştir.",
                            "Your secure payment information has been recorded in the individual category.", lang);
                        }
                        else
                        {
                            Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                            "#2 Secure payment options save failed.", lang);
                        }
                    }
                    else if (result.ErrorCode == "2003")//sistemde kayıtlı ama iyzicoda yoksa hatası
                    {
                        var resultCreate = IyzicoService.CreateSubMerchant(request);
                        if (resultCreate.IsSuccess)
                        {
                            if (details == null)
                            {
                                Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                                    "#2 Secure payment options save failed.", lang);
                                return Redirect(route);
                            }
                            details.IyzicoSubMerchantKey = resultCreate.Data.SubMerchantKey.Replace("/r/n", "");

                            var update = service.UpdateSecurePaymentDetail(details);
                            if (update)
                            {
                                Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz bireysel kategorisinden kayıt edilmiştir.",
                                "Your secure payment information has been recorded in the individual category.", lang);
                            }
                            else
                            {
                                Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. Güncelleme sırasında hata oluşmuştur..",
                                "#2 Secure payment options save failed.", lang);
                            }

                        }
                        else
                        {
                            Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                                "#3 Secure payment options save failed." + result.Message, lang);
                        }

                    }
                    else if (result.ErrorCode == "2018")//Kullanıcı önce kurumsal şirket kayıtı yaptıysa hatası.
                    {
                        if (details == null)
                        {
                            Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                                "#2 Secure payment options save failed.", lang);
                            return Redirect(route);
                        }
                        request.SubMerchantExternalId = details.ID.ToString();
                        var resultErrorUpdate = IyzicoService.UpdateSubMerchant(request);
                        if (resultErrorUpdate.IsSuccess)
                        {
                            details.IyzicoSubMerchantKey = IyzicoService.GetSubMerchantKey(request).Replace("/r/n", "");
                            var update = service.UpdateSecurePaymentDetail(details);
                            if (update)
                            {
                                Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz bireysel kategorisinden kayıt edilmiştir.",
                                "Your secure payment information has been recorded in the individual category.", lang);
                            }
                            else
                            {
                                Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                                "#2 Secure payment options save failed.", lang);
                            }
                        }
                        else if (resultErrorUpdate.ErrorCode == "2003")
                        {
                            var resultCreate = IyzicoService.CreateSubMerchant(request);
                            if (resultCreate.IsSuccess)
                            {
                                details.IyzicoSubMerchantKey = resultCreate.Data.SubMerchantKey.Replace("/r/n", "");
                                var update = service.UpdateSecurePaymentDetail(details);
                                if (update)
                                {
                                    Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz bireysel kategorisinden kayıt edilmiştir.",
                                    "Your secure payment information has been recorded in the individual category.", lang);
                                }
                                else
                                {
                                    Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. Güncelleme sırasında hata oluşmuştur..",
                                    "#2 Secure payment options save failed.", lang);
                                }

                            }
                            else
                            {
                                Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                                    "#3 Secure payment options save failed." + result.Message, lang);
                            }

                        }
                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                        "#3 Secure payment options save failed." + result.Message, lang);
                    }
                }





            }
            return Redirect(route);
        }




        [Authorize]
        public IActionResult SecurePayment2(UserSecurePaymentDetail userSecurePaymentDetail)
        {
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.UyelikBilgilerim, lang);
            if (userSecurePaymentDetail == null)
                return Redirect(route);
            using (var service = new UserService())
            {
                int userId = GetLoginID();
                userSecurePaymentDetail.UserID = userId;
                var details = service.GetSecurePaymentDetail(userId);
                var user = service.Get(userId);
                string operation;
                if (string.IsNullOrEmpty(user.TC) && string.IsNullOrEmpty(userSecurePaymentDetail.TC) == false)
                {
                    user.TC = userSecurePaymentDetail.TC;
                    service.Update(user);
                }
                else if (string.IsNullOrEmpty(user.TC) == false && string.IsNullOrEmpty(userSecurePaymentDetail.TC) == false)
                {
                    if (user.TC != userSecurePaymentDetail.TC)
                    {
                        user.TC = userSecurePaymentDetail.TC;
                        service.Update(user);
                    }
                }
                if (details == null)
                {
                    operation = "create";
                    details = userSecurePaymentDetail;
                }
                else
                {
                    operation = "update";


                }

                details.Type = userSecurePaymentDetail.Type;
                var type = details.Type;
                var request = new Iyzipay.Model.SubMerchant
                {
                    ConversationId = GetLoginID().ToString(),
                    SubMerchantExternalId = GetLoginID().ToString(),//aşağıda değişitiyor.
                    SubMerchantType = type.ToString(),
                    Address = userSecurePaymentDetail.InvoiceAddress,
                    Email = userSecurePaymentDetail.Email,
                    GsmNumber = userSecurePaymentDetail.MobilePhone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", ""),
                    Name = details.CompanyName,
                    Iban = details.IBAN?.Replace(" ", ""),
                    Locale = "TR",
                    Currency = "TRY",
                    TaxOffice = userSecurePaymentDetail.TaxOffice,
                    LegalCompanyTitle = userSecurePaymentDetail.CompanyName,
                };
                if (type == Iyzipay.Model.SubMerchantType.PRIVATE_COMPANY)
                    request.IdentityNumber = details.TC;
                else if (type == Iyzipay.Model.SubMerchantType.LIMITED_OR_JOINT_STOCK_COMPANY)
                    request.TaxNumber = userSecurePaymentDetail.TaxNumber?.Trim().Replace(" ", "");

                if (operation == "create")
                {
                    details = userSecurePaymentDetail;
                    var insert = service.InsertSecurePaymentDetail(details);
                    if (insert)
                    {
                        Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileri kayıt edilmiştir.",
                        "Secure payment options save failed.", lang);
                        operation = "update";
                        request.SubMerchantExternalId = service.GetSecurePaymentDetail(GetLoginID()).ID.ToString();
                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error, "Güvenli ödeme bilgileri sisteme kayıt edilemedi.",
                        "Secure payment options save failed.", lang);
                        return Redirect(route);
                    }



                    var result = IyzicoService.CreateSubMerchant(request);
                    if (result.IsSuccess || result.ErrorCode == "2002")
                    {
                        // if(result.ErrorCode=="2002")
                        // {
                        //     details.IyzicoSubMerchantKey = IyzicoService.GetSubMerchantKey(request);
                        // }
                        // else
                        // {
                        //     details.IyzicoSubMerchantKey = result.Data.SubMerchantKey.Replace(" ", "").Replace("/r/n", "");
                        // }
                        operation = "update";

                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                            "#3 Secure payment options save failed." + result.Message, lang);
                    }

                }
                if (operation == "update")
                {
                    request.SubMerchantExternalId = details.ID.ToString();
                    details = userSecurePaymentDetail;
                    request.SubMerchantKey = IyzicoService.GetSubMerchantKey(request);

                    var result = IyzicoService.UpdateSubMerchant(request);

                    if (result.IsSuccess || result.ErrorCode == "2003")
                    {

                        if (result.ErrorCode == "2003")
                        {
                            var resultInsert = IyzicoService.CreateSubMerchant(request);
                            if (!resultInsert.IsSuccess)
                            {
                                Notification = new UiMessage(NotyType.error, "Güvenli ödeme bilgileri sisteme kayıt edilemedi.",
                                "Secure payment options save failed.", lang);
                                return Redirect(route);
                            }

                        }
                        details.IyzicoSubMerchantKey = IyzicoService.GetSubMerchantKey(request);
                        details.ID = service.GetSecurePaymentDetail(GetLoginID()).ID;
                        var update = service.UpdateSecurePaymentDetail(details);
                        if (update)
                        {
                            var typeCategory = (type == SubMerchantType.PRIVATE_COMPANY)
                            ? new Localization().Get("Şahıs Şirketi", "Private Company", lang)
                            : new Localization().Get("Kurumsal Şirket", "Corporate Company", lang);
                            Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz " + typeCategory + " kategorisinden kayıt edilmiştir.",
                                "Your secure payment information has been recorded in the " + typeCategory + " category.", lang);
                        }
                        else
                        {
                            Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                               "#2 Secure payment options save failed.", lang);
                        }
                    }
                    else if (result.ErrorCode == "2012")//ters veri kayıtı sağlanmışsa
                    {
                        request.SubMerchantExternalId = details.UserID.ToString();
                        details = userSecurePaymentDetail;
                        request.SubMerchantKey = IyzicoService.GetSubMerchantKey(request);


                        var resultErrorUpdate = IyzicoService.UpdateSubMerchant(request);

                        if (resultErrorUpdate.IsSuccess || resultErrorUpdate.ErrorCode == "2003")
                        {

                            if (resultErrorUpdate.ErrorCode == "2003")
                            {
                                var resultInsert = IyzicoService.CreateSubMerchant(request);
                                if (!resultInsert.IsSuccess)
                                {
                                    Notification = new UiMessage(NotyType.error, "Güvenli ödeme bilgileri sisteme kayıt edilemedi.",
                                    "Secure payment options save failed.", lang);
                                    return Redirect(route);
                                }

                            }
                            details.IyzicoSubMerchantKey = IyzicoService.GetSubMerchantKey(request);
                            details.ID = service.GetSecurePaymentDetail(GetLoginID()).ID;
                            var update = service.UpdateSecurePaymentDetail(details);
                            if (update)
                            {
                                var typeCategory = (type == SubMerchantType.PRIVATE_COMPANY)
                                ? new Localization().Get("Şahıs Şirketi", "Private Company", lang)
                                : new Localization().Get("Kurumsal Şirket", "Corporate Company", lang);
                                Notification = new UiMessage(NotyType.success, "Güvenli ödeme bilgileriniz " + typeCategory + " kategorisinden kayıt edilmiştir.",
                                    "Your secure payment information has been recorded in the " + typeCategory + " category.", lang);
                            }
                            else
                            {
                                Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi.",
                                   "#2 Secure payment options save failed.", lang);
                            }
                        }
                    }
                    else
                    {
                        Notification = new UiMessage(NotyType.error, "#2 Güvenli ödeme bilgileri kayıt edilemedi. " + result.Message,
                            "#3 Secure payment options save failed." + result.Message, lang);
                    }
                }

            }
            return Redirect(route);
        }


        #endregion

        #region ProfilSayfam

        [Route("Hesabim/Profilim")]
        [Route("MyAccount/Profile")]
        [Authorize]
        public IActionResult MyProfile()
        {
            using (var adService = new AdvertService())
            using (var service = new UserService())
            {
                var user = service.Get(GetLoginID());
                if (user == null)
                    return Redirect("/");

                user.Adverts = adService.GetUserAdverts(user.ID);
                return View(user);
            }
        }

        #endregion

        #region CariHesabim

        [Route("Hesabim/Cari-Hesabim")]
        [Route("MyAccount/Commercial-Account")]
        [Authorize]
        public IActionResult CommercialAccount()
        {
            using (var sUser = new UserService())
            using (var sDoping = new DopingService())
            using (var sBanner = new BannerService())
            using (var textService = new TextService())
            {
                var lang = GetLang();
                var text = textService.GetText(Enums.Texts.AliciOnaylamamaNedeni, lang);
                var banners = sBanner.GetBannerHistroy(GetLoginID());
                var doping = sDoping.GetDopingHistory(GetLoginID());
                var dopingCount = doping?.Count(p => p.IsActive && (p.EndDate.HasValue && p.EndDate.Value > DateTime.Now));
                var bannerCount = banners?.Count(p => p.TypeID == Enums.BannerType.Banner && p.IsActive);
                var logoCount = banners?.Count(p => p.TypeID == Enums.BannerType.Logo && p.IsActive);
                var salesCount = ViewBag.PendingCargo ?? 0;
                var buysCount = ViewBag.Buys ?? 0;
                var model = new CommercialAccountViewModel
                {
                    Buys = sUser.GetBuys(GetLoginID()),
                    Sales = sUser.GetSales(GetLoginID()).ToList(),
                    Dopings = sDoping.GetDopingHistory(GetLoginID()),
                    Banners = banners?.Where(x => x.TypeID == Enums.BannerType.Banner).ToList(),
                    LogoBanners = banners?.Where(x => x.TypeID == Enums.BannerType.Logo).ToList(),
                    Text = text,
                    DopingCount = $"({dopingCount})",
                    SalesCount = $"({salesCount})",
                    BuysCount = $"({buysCount})",
                    BannersCount = $"({bannerCount})",
                    LogoBuysCount = $"({logoCount})"
                };
                return View(model);
            }
        }

        [Authorize]
        public IActionResult UpdateCargoDetails(int id, string cargoFirm, string cargoNo, DateTime cargoDate, Enums.PaymentRequestStatus status)
        {
            var t = new Localization();
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.CariHesabim, lang);

            using (var prService = new PaymentRequestService())
            using (var service = new UserService())
            using (var mailing = new MailingService())
            using (var setting = new SystemSettingService())
            using (var ad = new AdvertService())
            using (var text = new TextService())
            {
                var pr = service.GetSale(id, GetLoginID());
                if (pr != null && pr.SellerID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, t.Get("Erişim hatası.", "Access error.", lang));
                    return Redirect(route);
                }
                if (pr == null || pr.Status == Enums.PaymentRequestStatus.TeslimEdildi ||
                    pr.Status == Enums.PaymentRequestStatus.AliciIptalEtti)
                {
                    Notification = new UiMessage(NotyType.error, t.Get("Erişim hatası.", "Access error.", lang));
                    return Redirect(route);
                }
                if (status == Enums.PaymentRequestStatus.Iptal)
                {
                    status = pr.SellerID == GetLoginID() ? Enums.PaymentRequestStatus.SaticiIptalEtti : Enums.PaymentRequestStatus.AliciIptalEtti;
                }

                pr.CargoFirm = cargoFirm;
                pr.CargoNo = cargoNo;
                pr.CargoDate = cargoDate;
                pr.CargoDeliveryDate = null;
                pr.Status = status;
                var update = prService.Update(pr);
                if (!update)
                {
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Güncelleme sırasında beklenmedik bir hata oluştu.",
                            "An unexpected error occurred during the update.", lang));
                    return Redirect(route);
                }
                Notification = new UiMessage(NotyType.success, t.Get("Teslimat bilgileri güncellendi.",
                        "Shipment information has been updated.", lang));

                if (status == Enums.PaymentRequestStatus.KargoyaVerildi)
                {
                    var content = text.GetContent(Enums.Texts.UruneOnayVermenizGereklidir, lang);
                    var buyer = service.Get(pr.UserID);
                    var seller = service.Get(pr.SellerID);
                    var advert = ad.GetAdvert(pr.AdvertID);
                    var day = setting.GetSetting(Enums.SystemSettingName.OtomatikOdemeOnayi).Value;
                    var html = content.TextContent
                        .Replace("@aliciAdSoyad", buyer.Name)
                        .Replace("@otomotikOnay", day)
                        .Replace("@urunAdi", advert.Title)
                        .Replace("@urunAdet", pr.Amount.ToString())
                        .Replace("@siparisNo", pr.ID.ToString())
                        .Replace("@saticiAdSoyad", seller.Name)
                        .Replace("@saticiMail", seller.Email)
                        .Replace("@saticiTel", seller.MobilePhone)
                        .Replace("@saticiAdres", pr.ShippingAddress)
                        .Replace("@prId", pr.ID.ToString());
                    mailing.Send(html, buyer.Email, buyer.Name, content.Name);
                    PaymentRequestCargoInfoUpdated(pr, t);

                }
                else if (status == Enums.PaymentRequestStatus.SaticiIptalEtti)
                    PaymentRequestSellerCanceled(pr, t);
                else if (status == Enums.PaymentRequestStatus.AliciIptalEtti)
                    PaymentRequestBuyerCanceled(pr, t);
                else if (status == Enums.PaymentRequestStatus.TeslimEdildi)
                    PaymentRequestDelivered(pr, t, pr.Seller.LanguageID);

                return Redirect(route);
            }
        }

        [Authorize]
        [HttpPost]
        public JsonResult ReceivedCargo(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var settingService = new SystemSettingService())
            using (var mailing = new MailingService())
            using (var userService = new UserService())
            using (var service = new PaymentRequestService())
            {
                var settings = settingService.GetSystemSettings();
                var pr = service.Get(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Access Error" });
                if (pr.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = "Access Error" });

                pr.Status = Enums.PaymentRequestStatus.TeslimEdildi;
                pr.BuyerApproval = true;
                pr.IsSuccess = true;
                pr.BuyerApprovalDate = DateTime.Now;
                pr.CargoDeliveryDate = DateTime.Now;
                var update = service.Update(pr);
                if (update)
                {
                    PaymentRequestDelivered(pr, t, lang);
                    var adminMails = settings.Single(s => s.Name == Enums.SystemSettingName.YoneticiMailleri).Value;
                    var seller = userService.Get(pr.SellerID);
                    var moneyTransfer = MoneyTransfer2Seller(pr);
                    if (moneyTransfer != "success")
                    {
                        mailing.SendMail2Admins(adminMails, "Para Transferi Hatası.",
                            "Ödeme ID: " + pr.ID + " Kullanıcı ürünü teslim aldım dedi ancak satıcıya para transferi yapılamadı. " +
                            "Iyzico Payment Id: " + pr.PaymentId + " Iyzico PaymentTransactionId: " + pr.PaymentTransactionID +
                            " Hata Mesajı: " + moneyTransfer);

                    }
                    else
                    {
                        mailing.SendMail2Admins(adminMails, "Satıcıya Para Transferi",
                            $"#{seller.ID} {seller.Name} satıcıya ödeme aktarıldı." +
                            $" Sipariş No: #<a href=\"https://www.audiophile.org/AdminPanel/PaymentRequests/Details/{pr.ID}\" target=\"_blank\">{pr.ID}</a> <br /> Sipariş Tarihi: {pr.CreatedDate:dd.MM.yyyy HH:mm}");
                        SendApproveOrderMailToSeller(pr, seller);
                    }
                    return Json(new { isSuccess = true, message = t.Get("Sipariş bilgileri güncellendi.", "Order information updated.", lang) });
                }
                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Sipariş bilgileri güncellenirken hata oluştu. Lütfen tekrar deneyiniz.",
                    "Error updating order information. Please try again.", lang)
                });
            }
        }

        private string MoneyTransfer2Seller(PaymentRequest pr)
        {
            var approval = IyzicoService.PaymentApproval(pr.ID, pr.PaymentTransactionID);
            return approval.Status == "success" ? "success" : approval.ErrorMessage;
        }

        [Authorize]
        [HttpPost]
        public JsonResult OrderCancel(int id, string message)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var settingService = new SystemSettingService())
            using (var mailing = new MailingService())
            using (var service = new PaymentRequestService())
            {
                var settings = settingService.GetSystemSettings();
                var pr = service.GetOrder(id);
                if (pr == null)
                    return Json(new { isSuccess = false, message = "Access Error" });
                if (pr.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = "Access Error" });

                pr.Status = Enums.PaymentRequestStatus.AliciIptalTalebiOlusturdu;
                pr.BuyerApproval = false;
                pr.BuyerApprovalDate = DateTime.Now;
                var update = service.Update(pr);
                if (update)
                {
                    PaymentRequestBuyerCanceled(pr, t);
                    //var refund = IyzicoService.PaymentRefund(pr.ID, pr.PaymentTransactionID, GetIpAddress(),
                    //    pr.Price.ToString());
                    SendOrderCancelMailToSeller(pr, message);
                    return Json(new
                    {
                        isSuccess = true,
                        message = t.Get("Siparişiniz için iptal talebiniz oluşturuldu.",
                        "Your cancellation request has been created for your order.", lang)
                    });
                }
                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Sipariş bilgileri güncellenirken hata oluştu. Lütfen tekrar deneyiniz.",
                    "Error updating order information. Please try again.", lang)
                });
            }
        }
        private static void SendApproveOrderMailToSeller(PaymentRequest pr, User seller)
        {
            using (var mailing = new MailingService())
            using (var userService = new UserService())
            using (var text = new TextService())
            using (var adService = new AdvertService())

            {
                var ad = adService.GetAdvert(pr.AdvertID);

                var buyer = userService.Get(pr.UserID);
                var content = text.GetContent(Enums.Texts.AliciOnay, seller.LanguageID);
                var html = content.TextContent
                    .Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@siparisNo", pr.ID.ToString())
                    .Replace("@urunAdi", ad.Title)
                    .Replace("@urunAdet", pr.Amount.ToString())
                    .Replace("@aliciAdSoyad", buyer.Name)
                    .Replace("@aliciMail", buyer.Email)
                    .Replace("@aliciTel", buyer.MobilePhone)
                    .Replace("@aliciAdres", pr.InvoiceAddress)
                    .Replace("@prId", pr.ID.ToString());

                mailing.Send(html, seller.Email, seller.Name, content.Name);
            }
        }
        private static void SendOrderCancelMailToSeller(PaymentRequest pr, string message)
        {
            using (var mailing = new MailingService())
            using (var settingService = new SystemSettingService())
            using (var userService = new UserService())
            using (var text = new TextService())
            {
                var settings = settingService.GetSystemSettings();
                var seller = userService.Get(pr.SellerID);
                var buyer = userService.Get(pr.UserID);
                var content = text.GetContent(Enums.Texts.AliciRed, seller.LanguageID);
                var html = content.TextContent
                    .Replace("@saticiAdSoyad", seller.Name)
                    .Replace("@redMesaj", message)
                    .Replace("@siparisNo", pr.ID.ToString())
                    .Replace("@buyeraliciAdSoyad", buyer.Name)
                    .Replace("@aliciMail", buyer.Email)
                    .Replace("@aliciTel", buyer.MobilePhone)
                    .Replace("@aliciAdres", pr.InvoiceAddress)
                    .Replace("@prId", pr.ID.ToString());

                mailing.Send(html, seller.Email, seller.Name, content.Name);
                var adminMails = settings.Single(s => s.Name == Enums.SystemSettingName.YoneticiMailleri).Value;
                mailing.SendMail2Admins(adminMails, "Para İadesi Talebi",
                    $"Ödeme ID:<a href=\"https://www.audiophile.org/AdminPanel/PaymentRequests/Details/{pr.ID}\" target=\"_blank\">{pr.ID} </a> <br /> Kullanıcı siparişi iptal talebi oluşturdu. <br />  Iyzico Payment Id: {pr.PaymentId} <br /> Iyzico PaymentTransactionId: {pr.PaymentTransactionID} <br /> Neden : {message}");
            }

        }


        #region PaymentRequest_Update_Notification

        private void PaymentRequestCargoInfoUpdated(PaymentRequest pr, Localization t)
        {
            var notify = new Notification()
            {
                UserID = pr.UserID,
                Image = "/Content/img/cargo.png",
                Message = t.Get(
                    "Satın aldığınız " + pr.AdvertID + " ilan numaralı ürün kargoya verildi. " +
                    "Kargo Firması: " + pr.CargoFirm + " Takip Numarası: " + pr.CargoNo,
                    "The product you purchased is numbered " + pr.AdvertID + ". " +
                    "Cargo Firm: " + pr.CargoFirm + " Cargo Tracking Number: " + pr.CargoNo, pr.Buyer.LanguageID),
                CreatedDate = DateTime.Now,
                TypeID = Enums.NotificationType.IlanKargoBilgisiGuncellemesi,
                SenderUserID = GetLoginID(),
                Url = Constants.GetURL((int)Enums.Routing.CariHesabim, pr.Buyer.LanguageID)
            };
            using (var service = new NotificationService())
            {
                service.Insert(notify);
            }
        }

        private void PaymentRequestSellerCanceled(PaymentRequest pr, Localization t)
        {
            var notify = new Notification()
            {
                UserID = pr.UserID,
                Image = "/Content/img/cancel.png",
                Message = t.Get(
                    pr.ID + " numaralı siparişiniz satıcı tarafından iptal edildi.",
                    "Your order number " + pr.ID + " has been canceled by the seller.",
                    pr.Buyer.LanguageID),
                CreatedDate = DateTime.Now,
                TypeID = Enums.NotificationType.IlanKargoBilgisiGuncellemesi,
                SenderUserID = GetLoginID(),
                Url = "#"
            };
            using (var service = new NotificationService())
            {
                service.Insert(notify);
            }
        }

        private void PaymentRequestBuyerCanceled(PaymentRequest pr, Localization t)
        {
            var notify = new Notification()
            {
                UserID = pr.SellerID,
                Image = "/Content/img/cancel.png",
                Message = t.Get(
                    pr.ID + " numaralı siparişiniz alıcı tarafından iptal edildi.",
                    "Your order number " + pr.ID + " has been canceled by the buyer.",
                    pr.Seller.LanguageID),
                CreatedDate = DateTime.Now,
                TypeID = Enums.NotificationType.IlanIptal,
                SenderUserID = pr.UserID,
                Url = Constants.GetURL(Enums.Routing.CariHesabim, pr.Seller.LanguageID)
            };
            using (var service = new NotificationService())
            {
                service.Insert(notify);
            }
        }

        private void PaymentRequestDelivered(PaymentRequest pr, Localization t, int lang)
        {
            var notify = new Notification()
            {
                UserID = pr.SellerID,
                Image = "/Content/img/success-128.png",
                Message = t.Get(
                    pr.ID + " numaralı sipariş alıcıya teslim edildi.",
                    "Order " + pr.ID + " was delivered to the buyer.",
                    lang),
                CreatedDate = DateTime.Now,
                TypeID = Enums.NotificationType.IlanKargoBilgisiGuncellemesi,
                SenderUserID = 0,
                Url = "#"
            };
            using (var service = new NotificationService())
            {
                service.Insert(notify);
            }
        }

        #endregion





        [Authorize]
        [HttpPost]
        public JsonResult GetPaymentRequest(int id)
        {
            using (var service = new UserService())
            {
                var pr = service.GetSale(id, GetLoginID());
                if (pr == null)
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Erişim hatası.", "Access error.", GetLang())
                    });
                else
                    return Json(new { isSuccess = true, data = pr });
            }
        }
        [Authorize]
        [HttpPost]
        public JsonResult GetPaymentRequestCargoInfo(int id)
        {
            using (var service = new PaymentRequestService())
            {

                var pr = service.Get(id);
                if (pr == null)
                    return Json(new
                    {
                        isSuccess = false,
                        message = new Localization().Get("Erişim hatası.", "Access error.", GetLang())
                    });
                else
                    return Json(new { isSuccess = true, data = pr });
            }
        }
        #endregion

        #region Ilanlarim

        [Route("Hesabim/Ilanlarim")]
        [Route("MyAccount/My-Listing")]
        [Authorize]
        public IActionResult MyListing()
        {
            using (var service = new AdvertService())
            {
                var list = service.GetUserAdverts(GetLoginID(),false);
                if (list != null)
                {
                    return View(list.OrderByDescending(x => x.CreatedDate).ToList());
                }
                return View(list);
            }
        }

        [Route("MyAccount/PublishRequest")]
        [HttpPost]
        [Authorize]
        public JsonResult PublishRequest(int id)
        {
            var lang = GetLang(false);
            using (var service = new AdvertService())
            using (var mailing = new MailingService())
            using (var settingService = new SystemSettingService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null)
                    return Json(new { isSuccess = false, message = new Localization().Get("İlana erişilemedi.", "Ad was not reached.", lang) });
                var request = new AdvertPublishRequest
                {
                    IsActive = true,
                    RequestDate = DateTime.Now,
                    AdvertID = id,
                    UserID = GetLoginID()
                };
                var insert = service.InsertPublishRequest(request);
                if (!insert)
                    return Json(new { isSuccess = false, message = new Localization().Get("İlan yayınlanırken bir sorun oluştu.", "Ad publish failed.", lang) });

              
                    ad.IsDeleted = false;
                    ad.IsActive = true;
                   // ad.ApprovalStatus = Enums.ApprovalStatusEnum.WaitingforApproval;
                    service.Update(ad);

                var admins = settingService.GetSystemSettings()?.SingleOrDefault(s => s.Name == Enums.SystemSettingName.YoneticiMailleri)?.Value;
                string message = string.Empty;
                string mailSubject = string.Empty;                
                const string href = "https://audiophile.org/AdminPanel";
                if (admins != null)
                {


                    mailSubject = "Kullanıcı tarafından ilan güncellemesi yapıldı.";
                    message = $"{ad.ID} id'li ilan güncellenmiştir .<br/>Başlık: {ad.Title}<br/><a href={href}>İlanı Onaylamak İçin Tıkla</a>";
                    try
                    {
                        mailing.SendMail2Admins(admins, mailSubject, message);
                    }
                    catch (Exception ex)
                    {
                        using (var logService = new LogServices())
                        {
                            Log log = new Log()
                            {
                                Function = "AccountController.MyListing",
                                Message = GetLoginID() + "id li kullanici AccountController.MyListing metot içerisindeki try'da hata aldi.",
                                Params = JsonConvert.SerializeObject(new List<string>
                                        {
                                            id.ToString(),
                                            JsonConvert.SerializeObject(ad)
                                        }),
                                Detail =ex.ToString(),
                                CreatedDate = DateTime.Now
                            };
                            logService.Insert(log);
                        }
                        
                    }

                }



                return Json(new
                {
                    isSuccess = true,
                    message = new Localization().Get(
                    "Talebiniz oluşturuldu. Yönetici onayından sonra ilanınız yayınlanacak."
                    , "Your request has been created. Your ad will be published after the administrator's approval.", lang)
                });
            }
        }

        [Route("MyAccount/Unpublish")]
        [HttpPost]
        [Authorize]
        public JsonResult Unpublish(int id)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new AdvertService())
            {
                var ad = service.GetMyAdvert(id, GetLoginID());
                if (ad == null || ad.UserID != GetLoginID())
                {
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası.", "Access error.", lang) });
                }
                ad.IsActive = false;
                //ad.ApprovalStatus = Enums.ApprovalStatusEnum.WaitingforApproval;
                
                var update = service.Update(ad);
                if (!update)
                {
                    return Json(new { isSuccess = false, message = t.Get("Güncelleme işlemi sırasında bir hata oluştu.", "Unpublish is failed.", lang) });
                }

                service.DeletePublishRequests(id);

                return Json(new { isSuccess = true, message = t.Get("İlanınız  yayından alındı.", "Your ad has been hidden.", lang) });
            }
        }


        #endregion

        #region Bannerlarim

        [Route("Hesabim/Bannerlarim")]
        [Route("MyAccount/MyBanners")]
        [Authorize]
        public IActionResult MyBanners()
        {
            using (var sBanner = new BannerService())
            {
                var banners = sBanner.GetBannerHistroy(GetLoginID());
                return View(banners);
            }
        }

        [Route("Hesabim/Banner-Ekle")]
        [Route("MyAccount/Add-Banner")]
        [Authorize]
        public IActionResult AddBanner()
        {
            using (var service = new PublicService())
            {
                var dopings = service.GetDopingTypes();
                if (dopings != null && dopings.Any())
                {
                    var model = new AddBannerViewModel()
                    {
                        LogoDopings = dopings.Where(x => x.Group == Enums.DopingGroup.LogoBanner).ToList(),
                        BannerDopings = dopings.Where(x => x.Group == Enums.DopingGroup.AnasayfaBanner).ToList()
                    };
                    return View(model);
                }
            }
            return View();
        }

        [Route("MyAccount/AddLogoBanner")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddLogoBanner(int dopingId, string title, string url, bool paymentType, string fullname,
            string number, string month, string year, string cvc, string file64, IFormFile gifFile, int fileType, 
            IFormFile ImageSourceFile )
        {

            using (var memoryStream = new MemoryStream())
            { 
                ImageSourceFile.CopyTo(memoryStream);
                var arr = memoryStream.ToArray();
                file64 = Convert.ToBase64String(arr); 
                memoryStream.Dispose(); 
            }
             
            var t = new Localization(); ;
            var lang = GetLang(false);
            var route = Constants.GetURL((int)Enums.Routing.BannerEkle, lang);
            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();
            try
            {
                using (var mailingService = new MailingService())
                using (var settingService = new SystemSettingService())
                using (var userService = new UserService())
                using (var payRequestService = new PaymentRequestService())
                using (var bannerService = new BannerService())
                using (var publicService = new PublicService())
                {
                    var dopings = publicService.GetDopingTypes();
                    var doping = dopings.SingleOrDefault(x => x.ID == dopingId);

                    var adminMails = settingService.GetSystemSettings().SingleOrDefault(s => s.Name == Enums.SystemSettingName.YoneticiMailleri)?.Value;

                    #region ValidationsVeFileUpload

                    if (doping == null)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Seçtiğiniz banner tipi tespit edilemedi.", "The banner type you selected could not be detected.", lang));
                        return Redirect(route);
                    }
                    if ((fileType == 1 || fileType == 2) && string.IsNullOrEmpty(file64))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Dosya bulunamadı", "File is not found", lang));
                        return Redirect(route);
                    }
                    else if (fileType == 3 && string.IsNullOrEmpty(file64))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Logo dosyası bulunamadı", "Logo File is not found", lang));
                        return Redirect(route);
                    }
                    else if (fileType == 4 && gifFile == null)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Gif Dosyası bulunamadı", "Gif File is not found", lang));
                        return Redirect(route);
                    }
                    else if (fileType != 1 && fileType != 2 && fileType != 3 && fileType != 4)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Banner türü algılanamadı.", "Banner type could not be detected.", lang));
                        return Redirect(route);
                    }
                    var fileResult = "";
                    if (fileType == 1 || fileType == 2 || fileType == 3) //slider resim banner veya resim logo
                    {
                        file64 = file64.Replace("data:image/png;base64,", "");
                        file64 = file64.Replace("data:image/jpg;base64,", "");
                        file64 = file64.Replace("data:image/jpeg;base64,", "");
                        Byte[] bytes = Convert.FromBase64String(file64);
                        var src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".jpg";
                        var upload = new FileService().FileUpload(bytes, src, fileName);
                        if (!upload)
                        {
                            Notification = new UiMessage(NotyType.error, t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }
                        fileResult = new ImageService().Optimize75(src + fileName, src, fileName, addExt: false);
                    }
                    else if (fileType == 4) //gif logo
                    {
                        var src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".gif";
                        var upload = new FileService().FileUpload(GetLoginID(), gifFile, src, fileName);
                        if (string.IsNullOrEmpty(upload))
                        {
                            Notification = new UiMessage(NotyType.error, t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }
                        fileResult = new ImageService().OptimizeGif(src + fileName);
                    }
                    if (string.IsNullOrEmpty(fileResult))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Resim optimizasyon sorunu. Geçerli bir resim yüklediğinizden emin olunuz.",
                            "Image optimization problem. Be sure to upload a valid image.", lang), 10000);
                        return Redirect(route);
                    }
                    var user = userService.Get(GetLoginID());
                    if (user == null)
                    {
                        Notification = new UiMessage(NotyType.error, t.Get("Kullanıcı bilginize erişilemedi. Lütfen giriş yaptıktan sonra tekrar deneyin.",
                            "Your user information could not be accessed. Please try again after login.", lang));
                        return Redirect(route);
                    }
                    #endregion

                    var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);

                    var bannerType = Enums.BannerType.Banner;
                    var paymentRequestType = Enums.PaymentType.LogoBanner;

                    if (fileType == 1) //anasayfa banner
                    {
                        bannerType = Enums.BannerType.Banner;
                    }
                    else if (fileType == 2)//slider banner
                    {
                        bannerType = Enums.BannerType.Slider;
                        paymentRequestType = Enums.PaymentType.SliderBanner;
                    }
                    else if (fileType == 3 || fileType == 4) //logo veya gif
                    {
                        bannerType = Enums.BannerType.Logo;

                        paymentRequestType = Enums.PaymentType.GifBanner;
                    }
                    var banner = new Banner
                    {
                        Title = title,
                        CreatedDate = DateTime.Now,
                        UserID = GetLoginID(),
                        ImageSource = fileResult,
                        Url = url,
                        TypeID = bannerType,
                        Price = doping.Price,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddDays(doping.Day)
                    };
                    var bannerInsert = bannerService.Insert(banner);
                    if (!bannerInsert)
                    {
                        Notification = new UiMessage(NotyType.info, t.Get(
                            "Banner kaydedildi ancak onay talebi oluşturulamadı. Lütfen site yönetimine bilgi veriniz.",
                            "The banner was saved but the confirmation request could not be created. Please inform the website management.", lang));
                        return Redirect(route);
                    }

                    var paymentRequest = new PaymentRequest
                    {
                        ForeignModelID = banner.ID,
                        UserID = GetLoginID(),
                        Price = doping.Price,
                        Description = User.Identity.Name + " " + doping.Name + " " + doping.Day + " gün satın alıyor. ",
                        Type = paymentRequestType,
                        CreatedDate = DateTime.Now,
                        SellerID = 0,
                        IpAddress = ip,
                        SecurePayment = paymentType,
                        Amount = doping.Day,
                        Status = Enums.PaymentRequestStatus.Bekleniyor
                    };
                    var insertRequest = payRequestService.Insert(paymentRequest);

                    if (insertRequest && Production)
                    {
                        var adminMailContent = "#" + user.ID + " ID'li " + user.Name + " isimli kullanıcı banner girişi yaptı.";
                        mailingService.SendMail2Admins(adminMails, user.Name + " isimli kullanıcı banner girişi yaptı.", adminMailContent);
                    }

                    if (insertRequest && paymentType)
                    {
                        var init = BuildPayment(user, userAddress, ip, cvc, fullname, month, year, number,
                            paymentRequest.ID, doping, IyzicoService.Callback);
                        if (init.Status != "success")
                        {
                            Notification = new UiMessage(NotyType.error, init.ErrorMessage);
                            return Redirect(route);
                        }
                       // user.AdvertPaymentRequired = true;
                        userService.Update(user);
                        return View("Blank", init.HtmlContent);
                    }
                    else if (insertRequest)
                    {
                        Notification = new UiMessage(NotyType.success, t.Get(
                            "İşleminiz tamamlandı. Yönetici onayından sonra banner yayınlanacak.",
                            "Your transaction is complete. The banner will be published after the administrator's approval.", lang));
                        return Redirect(route);
                    }
                }
            }
            catch (Exception e)
            {
                Notification = new UiMessage(NotyType.success, t.Get(
                    "Beklenmedik bir hata oluştu. Lütfen tekrar deneyin.",
                    "An unexpected error has occurred. Please try again.", lang));
            }
            return Redirect(route);
        }


        [Route("Hesabim/Banner-Duzenle/{id}")]
        [Route("MyAccount/Edit-Banner/{id}")]
        [Authorize]
        public IActionResult EditBanner(int id)
        {
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner != null && banner.UserID == GetLoginID())
                    return View(banner);

                return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, GetLang(false)));
            }
        }

        [Route("Hesabim/Banner-Duzenle/{id}")]
        [Route("MyAccount/Edit-Banner/{id}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditBanner(int id, string title, string url, string file64, IFormFile gifFile, int fileType)
        {
            var lang = GetLang(false);
            var route = Constants.GetURL(Enums.Routing.Bannerlarim, lang);
            var t = new Localization();
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null || banner.UserID != GetLoginID())
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));

                banner.Title = title;
                banner.Url = url;

                #region FileUpload

                if (!string.IsNullOrEmpty(file64))
                {
                    var fileResult = "";
                    if (fileType == 1) //slider resim banner veya resim logo
                    {
                        file64 = file64.Replace("data:image/png;base64,", "");
                        file64 = file64.Replace("data:image/jpg;base64,", "");
                        file64 = file64.Replace("data:image/jpeg;base64,", "");
                        Byte[] bytes = Convert.FromBase64String(file64);
                        var src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".jpg";
                        var upload = new FileService().FileUpload(bytes, src, fileName);
                        if (!upload)
                        {
                            Notification = new UiMessage(NotyType.error,
                                t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }

                        fileResult = new ImageService().Optimize75(src + fileName, src, fileName, addExt: false);
                    }
                    else if (fileType == 2) //gif logo
                    {
                        const string src = "/Upload/Dopings/";
                        var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".gif";
                        var upload = new FileService().FileUpload(GetLoginID(), gifFile, src, fileName);
                        if (string.IsNullOrEmpty(upload))
                        {
                            Notification = new UiMessage(NotyType.error,
                                t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                            return Redirect(route);
                        }
                        fileResult = new ImageService().OptimizeGif(src + fileName);
                    }
                    if (string.IsNullOrEmpty(fileResult))
                    {
                        Notification = new UiMessage(NotyType.error, t.Get(
                            "Resim optimizasyon sorunu. Geçerli bir resim yüklediğinizden emin olunuz.",
                            "Image optimization problem. Be sure to upload a valid image.", lang), 10000);
                        return Redirect(route);
                    }

                    banner.ImageSource = fileResult;
                }

                if (gifFile != null)
                {
                    const string src = "/Upload/Dopings/";
                    var fileName = Common.Localization.Slug(title) + "_" + new Random().Next(100, 999) + ".gif";
                    var upload = new FileService().FileUpload(GetLoginID(), gifFile, src, fileName);
                    if (string.IsNullOrEmpty(upload))
                    {
                        Notification = new UiMessage(NotyType.error,
                            t.Get("Dosya yükleme hatası", "File upload failed.", lang));
                        return Redirect(route);
                    }
                    var fileResult = new ImageService().OptimizeGif(src + fileName);
                    banner.ImageSource = fileResult;

                }

                #endregion


                var update = service.Update(banner);
                if (!update)
                {
                    Notification = new UiMessage(NotyType.error,
                        t.Get("Banner güncellenirken bir hata oluştu.", "Banner update failed.", lang));
                    return Redirect(route);
                }

                Notification = new UiMessage(NotyType.success,
                    t.Get("Banner güncellendi.", "Banner has been updated.", lang));
                return Redirect(route);
            }
        }

        [Route("MyAccount/UnpublishBanner")]
        [HttpPost]
        [Authorize]
        public JsonResult UnpublishBanner(int id)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new BannerService())
            {
                var ad = service.Get(id);
                if (ad == null || ad.UserID != GetLoginID())
                {
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası.", "Access error.", lang) });
                }
                ad.IsActive = false;
                var update = service.Update(ad);
                if (!update)
                {
                    return Json(new { isSuccess = false, message = t.Get("Güncelleme işlemi sırasında bir hata oluştu.", "Unpublish is failed.", lang) });
                }

                return Json(new { isSuccess = true, message = t.Get("Bannerınız yayından alındı.", "Your banner has been hidden.", lang) });
            }
        }

        [Route("MyAccount/ExtendBannerTime/{id}")]
        [Authorize]
        public IActionResult ExtendBannerTime(int id)
        {
            var lang = GetLang(false);
            using (var pubService = new PublicService())
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null || banner.UserID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, "Erişim hatası.", "Access error.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }
                var dopings = new List<DopingType>();
                if (banner.TypeID == Enums.BannerType.Logo)
                    dopings = pubService.GetDopingTypes().Where(x => x.Group == Enums.DopingGroup.LogoBanner).OrderBy(x => x.Day).ToList();
                else
                    dopings = dopings.Where(x => x.Group == Enums.DopingGroup.AnasayfaBanner).ToList();

                var model = new ExtendBannerTimeViewModel
                {
                    Banner = banner,
                    Dopings = dopings
                };
                return View(model);
            }
        }

        [Route("MyAccount/ExtendBannerTime/{id}")]
        [HttpPost]
        [Authorize]
        public IActionResult ExtendBannerTime(int id, bool paymentType, int DopingID, string CardName, string CardNumber,
            string Month, string Year, string CVC)
        { //paymentType: true = kredi kartı ile ödeme
            var lang = GetLang(false);

            using (var prService = new PaymentRequestService())
            using (var userService = new UserService())
            using (var pubService = new PublicService())
            using (var service = new BannerService())
            {
                var banner = service.Get(id);
                if (banner == null || banner.UserID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, "Erişim hatası.", "Access error.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var doping = pubService.GetDopingTypes().SingleOrDefault(d => d.ID == DopingID);
                if (doping == null)
                {
                    Notification = new UiMessage(NotyType.error, "Geçersiz doping seçimi yaptınız.", "You have chosen an invalid doping.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var user = userService.Get(GetLoginID());
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Üyelik bilgilerinize erişilemedi, lütfen tekrar giriş yapınız.",
                        "Your membership information could not be accessed, please login again.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                if (!paymentType)
                {
                    var pr = new PaymentRequest()
                    {
                        Amount = doping.Day,
                        UserID = GetLoginID(),
                        ForeignModelID = banner.ID,
                        Type = Enums.PaymentType.BannerSureUzatma,
                        Price = doping.Price,
                        SecurePayment = false,
                        Description = user.Name + " (" + user.UserName + ") isimli kullanıcı banka havalesi ile banner süresini uzatma talebi oluşturdu.",
                        CreatedDate = DateTime.Now,
                        IpAddress = GetIpAddress(),
                        Status = Enums.PaymentRequestStatus.Bekleniyor,
                    };
                    var insert = prService.Insert(pr);
                    if (!insert)
                    {
                        Notification = new UiMessage(NotyType.error, "Ödeme kaydınız oluşturulamadı. Lütfen tekrar deneyiniz.",
                            "Your payment registration process failed. Please try again.", lang);
                        return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                    }

                    Notification = new UiMessage(NotyType.success, "Ödeme kaydınız oluşturuldu. Yönetici onayından sonra yazınız güncellenecek.",
                        "Your payment registration has been created. Will be updated after the approval of the administrator.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                if (string.IsNullOrEmpty(CardName) || string.IsNullOrEmpty(CardNumber) || string.IsNullOrEmpty(Month) ||
                    string.IsNullOrEmpty(Year) || string.IsNullOrEmpty(CVC))
                {
                    Notification = new UiMessage(NotyType.error, "Lütfen kredi kartı bilgilerinizi doldurunuz.",
                        "Please fill in your credit card information.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var userAddresses = userService.GetUserAddresses(user.ID);
                if (userAddresses == null || userAddresses.Count == 0)
                {
                    Notification = new UiMessage(NotyType.error, "Satın alım yapmak için bir adres kaydı oluşturmalısınız.",
                        "You must create an address record to make a purchase.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.AdresBilgierim, lang));
                }
                var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);

                var ip = GetIpAddress();
                var _pr = new PaymentRequest
                {
                    Amount = doping.Day,
                    UserID = GetLoginID(),
                    ForeignModelID = banner.ID,
                    Type = Enums.PaymentType.BannerSureUzatma,
                    Price = doping.Price,
                    SecurePayment = true,
                    Description = user.Name + " (" + user.UserName + ") isimli kullanıcı kredi kartı ile banner süresini uzatma talebi oluşturdu.",
                    CreatedDate = DateTime.Now,
                    IpAddress = GetIpAddress(),
                    Status = Enums.PaymentRequestStatus.Bekleniyor,
                };
                var _insert = prService.Insert(_pr);
                if (!_insert)
                {
                    Notification = new UiMessage(NotyType.error, "Güncelleme işlemi yapılamadı.",
                        "Update failed.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }

                var payment = BuildPayment(user, userAddress, ip, CVC, CardName, Month, Year, CardNumber, _pr.ID, doping,
                    IyzicoService.Callback);

                if (payment.Status != "success")
                {
                    Notification = new UiMessage(NotyType.success, "Güvenli Ödeme başlatılamadı. " + payment.ErrorMessage,
                        "Secure payment cannot started. " + payment.ErrorMessage, lang);
                    return Redirect(Constants.GetURL(Enums.Routing.Bannerlarim, lang));
                }
                else
                {
                    return View("Blank", payment.HtmlContent);
                }

            }

            //return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
        }


        #endregion

        #region Blog

        [Route("Hesabim/Blog-Ekle")]
        [Route("MyAccount/Add-Blog")]
        [Authorize]
        public IActionResult AddBlog()
        {
            using (var service = new PublicService())
            {
                var model = new AddBlogViewModel()
                {
                    Dopings = service.GetDopingTypes()
                };
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("MyAccount/UploadBlogImage")]
        public async Task<IActionResult> UploadBlogImage(IFormFile formFile)
        {
            using (var fileService = new FileService())
            using (var service = new BlogPostService())
            using (var imageService = new ImageService())
            {
                var form = HttpContext.Request?.Form;
                var files = form?.Files;
                var fileIds = JsonConvert.DeserializeObject<List<Guid>>(form?["fileIds"]);
                var ids = new List<object>();
                string path = "";
                if (files == null)
                {
                    return BadRequest();
                }
                for (var index = 0; index < files.Count; index++)
                {
                    try
                    {
                        var directory = "/Upload/Blog/";
                        var file = files[index];
                        if (file.Length < 10000000) //10mb
                        {
                            var ext = Path.GetExtension(file.FileName);
                            var imageGuid = Guid.NewGuid();
                            path = fileService.FileUpload(GetLoginID(), file, "/Upload/Blog/", imageGuid + "_" + (index + 1) + "" + ext);


                            var thumbName = imageGuid + "_" + (index + 1) + "_thumb" + ".jpg";
                            imageService.CreateThumbnail(GetLoginID(), path, directory, thumbName);

                            var id = service.InsertImage(new BlogPostImage()
                            {
                                PostID = 0,
                                Source = path,
                                Thumbnail = directory + thumbName,
                                CreatedDate = DateTime.Now
                            });
                            ids.Add(new { uuid = fileIds[index], imageId = id });
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("HATA " + e.Message);
                    }

                }
                return Ok(ids);
            }
        }
        [Authorize]
        [HttpPost]
        [Route("MyAccount/SaveBlog")]
        public async Task<IActionResult> SaveBlog(BlogPost blogPost, bool useCreditCard, string fullName,
            string month, string year, string cvc, string number, int dopingId, string MainImage, List<int> imageIds)
        {
            var lang = GetLang();
            var t = new Localization();
            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();

            using (var userService = new UserService())
            using (var publicService = new PublicService())
            using (var fileService = new FileService())
            using (var imageService = new ImageService())
            using (var payService = new PaymentRequestService())
            using (var mailing = new MailingService())
            using (var settingService = new SystemSettingService())
            using (var service = new BlogPostService())
            {
                var user = userService.Get(GetLoginID());
                var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);

                #region Validations

                if (blogPost == null)
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("İçerik anlaşılamadı. Lütfen tüm alanları doldurunuz.",
                        "Content could not be understood. Please fill in all fields.", lang)
                    });
                if (blogPost.CategoryID <= 0)
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("İçerik anlaşılamadı. Lütfen tüm alanları doldurunuz.",
                        "Content could not be understood. Please fill in all fields.", lang)
                    });
                if (dopingId > 0 && useCreditCard)
                {
                    if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year) || string.IsNullOrEmpty(number) || string.IsNullOrEmpty(cvc))
                    {
                        return Json(new
                        {
                            isSuccess = false,
                            message = t.Get("Kredi kartı ile ödeme yapmak için tüm kart bilgilerini doldurmalısınız.",
                            "To pay by credit card, you must fill in all card details.", lang)
                        });
                    }
                }
                #endregion

                var dopings = publicService.GetDopingTypes();
                var doping = dopingId > 0 ? dopings.SingleOrDefault(x => x.ID == dopingId) : null;
                blogPost.Title = blogPost.Title.Modify();

                blogPost.UserID = GetLoginID();
                blogPost.CreatedDate = DateTime.Now;

                blogPost.CreditCard = useCreditCard;
                if (doping != null)
                {
                    blogPost.PaymentAmount = doping.Price;
                    blogPost.DopingEndDate = DateTime.Now.AddDays(doping.Day);
                }
                if (!string.IsNullOrEmpty(blogPost.Content))
                {
                    blogPost.Content = blogPost.Content.Replace("../", "/");
                }
                var insert = service.Insert(blogPost);
                if (!insert)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Yazınız kaydedilemedi. Lütfen tüm bilgileri doldurup tekrar deneyiniz.",
                        "Your post could not be saved. Please fill in all information and try again..", lang)
                    });
                }
                var admin = settingService.GetSystemSettings().SingleOrDefault(x => x.Name == Enums.SystemSettingName.YoneticiMailleri)?.Value;
                if (admin != null && Production)
                {
                    mailing.SendMail2Admins(admin, "Yeni Blog Girişi", "#" + user.ID + " kodlu " + user.Name + " (" + user.UserName + ") isimli kullanıcı yeni bir blog girişi yaptı, onaylamak için admin paneline giriniz.");
                }

                #region PostImages

                foreach (var imageId in imageIds)
                {
                    var image = service.GetBlogPostImage(imageId);
                    if (image != null)
                    {
                        if (Convert.ToInt32(MainImage) == imageId)
                        {
                            blogPost.CoverImage = image.Source;
                            blogPost.Thumbnail = image.Thumbnail;
                            service.Update(blogPost);
                        }
                        service.UpdateImage(imageId, blogPost.ID);
                    }
                }
                #endregion

                var prDesc = user.Name + " ücretsiz blog girişi yaptı. Başlık: " + blogPost.Title;
                if (dopingId > 0)
                {
                    prDesc = user.Name + " ücretli blog girişi yaptı. Başlık: " + blogPost.Title;
                }
                var payRequest = new PaymentRequest()
                {
                    Type = Enums.PaymentType.BlogGirisi,
                    UserID = GetLoginID(),
                    CreatedDate = DateTime.Now,
                    Description = prDesc,
                    ForeignModelID = blogPost.ID,
                    Price = doping?.Price ?? 0,
                    SecurePayment = useCreditCard,
                    Amount = doping != null ? doping.Day : 0,
                    IpAddress = ip,
                    Status = Enums.PaymentRequestStatus.Bekleniyor
                };
                var insertPayRequest = payService.Insert(payRequest);

                if (!useCreditCard)
                {
                    if (insertPayRequest)
                        return Json(new
                        {
                            isSuccess = true,
                            message = t.Get("İşleminiz tamamlandı. Yönetici onayından sonra yayınlanacak.",
                            "Your transaction is complete. Will be published after administrator approval.", lang)
                        });
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Blog girişiniz yapıldı ancak ödeme kaydı oluşturulamadı. Lütfen bizimle iletişime geçiniz.",
                        "Your blog post was registered but the payment record could not be created. Please contact us.", lang)
                    });
                }
                else
                {
                    if (insertPayRequest)
                    {
                        var securePay = BuildPayment(user, userAddress, ip, cvc, fullName, month, year, number,
                            payRequest.ID, doping, IyzicoService.Callback);
                        if (securePay.Status == "success")
                        {
                            return Json(new { isSuccess = true, html = securePay.HtmlContent });
                        }
                        else
                        {
                            return Json(new { isSuccess = false, message = "3D Secure Error | " + securePay?.ErrorMessage });
                        }
                    }
                    else
                    {
                        return Json(new
                        {
                            isSuccess = true,
                            message = t.Get(
                            "Yazınız kayıt edildi ancak ödeme işlemi yapılamıyor. Lütfen site yönetimi ile iletişime geçin.",
                            "Your post has been registered but cannot be processed. Please contact the site administration.", lang)
                        });
                    }
                }

            }
        }


        [Route("Account/Blog/UploadImage")]
        [Authorize]
        public async Task<JsonResult> UploadImage(IFormFile file)
        {
            if (file != null && file.ContentType.Contains("image"))
            {
                var path = "/Upload/Blog/";
                var ext = System.IO.Path.GetExtension(file.FileName);
                var hex = Encryptor.GenerateToken();
                var newName = hex + ext;

                var fullPath = new FileService().FileUpload(GetLoginID(), file, path, newName);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var optimize = new ImageService().Optimize75(fullPath, path, hex);
                    if (!string.IsNullOrEmpty(optimize))
                    {
                        return Json(new { isSuccess = true, location = optimize });
                    }
                }
            }
            return Json(new { isSuccess = false });
        }

        [Route("MyAccount/EditBlog/{id}")]
        [Authorize]
        public IActionResult EditBlog(int id)
        {
            var lang = GetLang(false);
            using (var service = new BlogPostService())
            {
                var post = service.Get(id);
                if (post.UserID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, "Erişim hatası.", "Access error.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }
                return View(post);
            }
        }

        [Route("MyAccount/EditBlog/{id}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditBlog(BlogPost blogPost, string MainImage)
        {
            var lang = GetLang(false);
            var t = new Localization();
            using (var imageService = new ImageService())
            using (var fileService = new FileService())
            using (var service = new BlogPostService())
            {
                var post = service.Get(blogPost.ID);
                if (post.UserID != GetLoginID())
                {
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası.", "Access error.", lang) });
                }

                post.Title = blogPost.Title;
                post.ShortContent = blogPost.ShortContent;
                post.Tags = blogPost.Tags;
                post.Content = blogPost.Content;
                var update = service.Update(post);
                if (!update)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Yazı güncellenirken bir hata oluştu.",
                        "There was an error updating the article.", lang)
                    });
                }

                #region PostImages

                var files = HttpContext.Request.Form.Files;
                string path = "";
                for (var index = 0; index < files.Count; index++)
                {
                    try
                    {
                        var random = new Random().Next(100, 999);
                        var directory = "/Upload/Blog/";
                        var file = files[index];
                        if (file.Length < 10000000) //10mb
                        {
                            var ext = Path.GetExtension(file.FileName);
                            path = fileService.FileUpload(GetLoginID(), file, "/Upload/Blog/", post.ID + "_" + random + "" + ext);

                            var fileName = post.ID + "_" + random;
                            var optimize75 = imageService.Optimize75(path, directory, fileName);

                            var thumbName = post.ID + "_" + random + "_thumb" + ".jpg";
                            imageService.CreateThumbnail(GetLoginID(), optimize75, directory, thumbName);

                            if (file.FileName == MainImage)
                            {
                                post.CoverImage = optimize75;
                                post.Thumbnail = directory + thumbName;
                                service.Update(post);
                            }
                            service.InsertImage(new BlogPostImage()
                            {
                                PostID = post.ID,
                                Source = optimize75,
                                Thumbnail = directory + thumbName,
                                CreatedDate = DateTime.Now
                            });
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }

                #endregion

                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Yazı güncellendi.",
                    "Post has been updated.", lang)
                });
            }
        }

        [Route("DeleteBlogImage")]
        [Authorize]
        public JsonResult DeleteBlogImage(int id)
        {
            var lang = GetLang(false);
            var t = new Localization();
            using (var service = new BlogPostService())
            {
                var image = service.GetBlogPostImage(id);
                if (image.BlogPost.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = t.Get("Erişim Hatası.", "Access Error.", lang) });

                var delete = service.DeleteImage(image);
                if (delete)
                    return Json(new { isSuccess = true, message = t.Get("Görsel silindi.", "Image has been deleted.", lang) });

                return Json(new { isSuccess = false, message = t.Get("Silme işlemi yapılamadı.", "Deletion failed.", lang) });
            }
        }

        [Route("DeleteBlog")]
        [HttpPost]
        [Authorize]
        public JsonResult DeleteBlog(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var service = new BlogPostService())
            {
                var post = service.Get(id);
                if (post == null)
                    return Json(new { isSuccess = false, message = t.Get("Yazı bulunamadı.", "Post cannot be found.", lang) });

                if (post.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = t.Get("Erişiminiz engellendi.", "Access denied.", lang) });

                post.IsActive = false;
                if (!service.Update(post))
                    return Json(new { isSuccess = false, message = t.Get("Yazı kaldırılırken bir hata oluştu.", "An error occurred during unpublishing.", lang) });

                return Json(new { isSuccess = true, message = t.Get("İşleminiz tamamlandı!", "Post hidden!", lang) });
            }
        }

        [Route("MyAccount/ExtendPostTime/{id}")]
        [Authorize]
        public IActionResult ExtendPostTime(int id)
        {
            var lang = GetLang();
            using (var pubService = new PublicService())
            using (var service = new BlogPostService())
            {
                var dopings = pubService.GetDopingTypes().Where(x => x.Group == Enums.DopingGroup.HaberKampanya).OrderBy(x => x.Day).ToList();
                var post = service.Get(id);
                if (post == null || post.UserID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, "Post bulunamadı.", "Post not found.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }
                var model = new BlogExtendTimeViewModel()
                {
                    Dopings = dopings,
                    Post = post
                };
                return View(model);
            }
        }

        [Route("MyAccount/ExtendPostTime/{id}")]
        [HttpPost]
        [Authorize]
        public IActionResult ExtendPostTime(int id, bool paymentType, int DopingID, string CardName, string CardNumber,
            string Month, string Year, string CVC)
        { //paymentType: true = kredi kartı ile ödeme
            var lang = GetLang(false);

            using (var prService = new PaymentRequestService())
            using (var userService = new UserService())
            using (var pubService = new PublicService())
            using (var service = new BlogPostService())
            {
                var post = service.Get(id);
                if (post == null || post.UserID != GetLoginID())
                {
                    Notification = new UiMessage(NotyType.error, "Erişim hatası.", "Access error.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }

                var doping = pubService.GetDopingTypes().SingleOrDefault(d => d.ID == DopingID);
                if (doping == null)
                {
                    Notification = new UiMessage(NotyType.error, "Geçersiz doping seçimi yaptınız.", "You have chosen an invalid doping.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }

                var user = userService.Get(GetLoginID());
                if (user == null)
                {
                    Notification = new UiMessage(NotyType.error, "Üyelik bilgilerinize erişilemedi, lütfen tekrar giriş yapınız.",
                        "Your membership information could not be accessed, please login again.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }


                if (!paymentType)
                {
                    var pr = new PaymentRequest()
                    {
                        Amount = doping.Day,
                        UserID = GetLoginID(),
                        ForeignModelID = post.ID,
                        Type = Enums.PaymentType.BlogSureUzatma,
                        Price = doping.Price,
                        SecurePayment = false,
                        Description = user.Name + " (" + user.UserName + ") isimli kullanıcı banka havalesi ile blog süresini uzatma talebi oluşturdu.",
                        CreatedDate = DateTime.Now,
                        IpAddress = GetIpAddress(),
                        Status = Enums.PaymentRequestStatus.Bekleniyor
                    };
                    var insert = prService.Insert(pr);
                    if (!insert)
                    {
                        Notification = new UiMessage(NotyType.error, "Ödeme kaydınız oluşturulamadı. Lütfen tekrar deneyiniz.",
                            "Your payment registration process failed. Please try again.", lang);
                        return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                    }
                    Notification = new UiMessage(NotyType.success, "Ödeme kaydınız oluşturuldu. Yönetici onayından sonra yazınız güncellenecek.",
                        "Your payment registration has been created. Will be updated after the approval of the administrator.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }

                if (string.IsNullOrEmpty(CardName) || string.IsNullOrEmpty(CardNumber) || string.IsNullOrEmpty(Month) ||
                    string.IsNullOrEmpty(Year) || string.IsNullOrEmpty(CVC))
                {
                    Notification = new UiMessage(NotyType.error, "Lütfen kredi kartı bilgilerinizi doldurunuz.",
                        "Please fill in your credit card information.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }

                var userAddress = userService.GetUserAddresses(GetLoginID())?.SingleOrDefault(x => x.IsDefault);

                var ip = GetIpAddress();
                var _pr = new PaymentRequest
                {
                    Amount = doping.Day,
                    UserID = GetLoginID(),
                    ForeignModelID = post.ID,
                    Type = Enums.PaymentType.BlogSureUzatma,
                    Price = doping.Price,
                    SecurePayment = true,
                    Description = user.Name + " (" + user.UserName + ") isimli kullanıcı kredi kartı ile blog süresini uzatma talebi oluşturdu.",
                    CreatedDate = DateTime.Now,
                    IpAddress = GetIpAddress(),
                    Status = Enums.PaymentRequestStatus.Bekleniyor,
                };
                var _insert = prService.Insert(_pr);
                if (!_insert)
                {
                    Notification = new UiMessage(NotyType.success, "Güvenli Ödeme kaydınız oluşturuldu. Yönetici onayından sonra yazınız güncellenecek.",
                        "Your secure payment registration has been created. Will be updated after the approval of the administrator.", lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }

                var payment = BuildPayment(user, userAddress, ip, CVC, CardName, Month, Year, CardNumber, _pr.ID, doping,
                    IyzicoService.Callback);

                if (payment.Status != "success")
                {
                    Notification = new UiMessage(NotyType.success, "Güvenli Ödeme başlatılamadı. " + payment.ErrorMessage,
                        "Secure payment cannot started. " + payment.ErrorMessage, lang);
                    return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
                }
                else
                {
                    return View("Blank", payment.HtmlContent);
                }

            }

            //return Redirect(Constants.GetURL(Enums.Routing.HaberVeFirmalarim, lang));
        }


        [Authorize]
        [HttpPost]
        [Route("UpdateBlogMainPhoto")]
        public JsonResult UpdateBlogMainPhoto(int id, int photoId)
        {
            var t = new Localization();
            var lang = GetLang(false);
            using (var service = new BlogPostService())
            {
                var ad = service.Get(id);
                if (ad == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });
                if (ad.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });

                var photo = service.GetBlogPostImage(photoId);
                if (photo == null)
                    return Json(new { isSuccess = false, message = t.Get("Erişim hatası", "Access error", lang) });

                ad.Thumbnail = photo.Thumbnail;
                var update = service.Update(ad);
                if (!update)
                {
                    return Json(new
                    {
                        isSuccess = false,
                        message = t.Get("Blog güncellenirken bir hata oluştu",
                        "Blog update failed", lang)
                    });
                }

                return Json(new
                {
                    isSuccess = true,
                    message = t.Get("Blog güncellendi", "Blog updated", lang)
                });
            }
        }

        #endregion

        #region Begendiklerim

        [Route("Hesabim/Begendigim-Ilanlar")]
        [Route("MyAccount/MyLikes")]
        [Authorize]
        public IActionResult MyLikes()
        {
            using (var service = new UserService())
            {
                var list = service.GetMyLikes(GetLoginID());
                return View(list);
            }
        }

        [Route("MyLikes/Unfollow")]
        [HttpPost]
        [Authorize]
        public JsonResult Unfollow(int id)
        {
            var t = new Localization();
            var lang = GetLang();
            using (var service = new UserService())
            {
                var like = service.GetAdvertLike(id);
                if (like == null)
                    return Json(new { isSuccess = false, message = t.Get("Bu ilanı zaten takip etmiyorsunuz.", "You are not already following this ad.", lang) });

                if (like.UserID != GetLoginID())
                    return Json(new { isSuccess = false, message = t.Get("Erişiminiz engellendi.", "Access denied.", lang) });

                if (!service.DeleteAdvertLike(like))
                    return Json(new { isSuccess = false, message = t.Get("Takibi bırakma sırasında bir hata oluştu.", "An error occurred during unfollow.", lang) });

                return Json(new { isSuccess = true, message = t.Get("İşleminiz tamamlandı!", "The follow up has been abandoned!", lang) });
            }
        }

        #endregion

        #region Yorumlar

        [Route("Hesabim/Yaptigim-Yorumlar")]
        [Route("MyAccount/MyComments")]
        [Authorize]
        public IActionResult MyComments()
        {
            return View();
        }

        #endregion

        #region Haber ve Firmalarım

        [Route("Hesabim/Haber-Ve-Firmalarim")]
        [Route("MyAccount/My-News-And-Companies")]
        [Authorize]
        public IActionResult MyNewsAndCompanies()
        {
            using (var service = new BlogPostService())
            {
                var posts = service.GetUserPosts(GetLoginID());
                if (posts == null)
                {
                    return View();
                }
                var result = posts.Where(p =>
                    p.CategoryID == (int)Enums.BlogCategory.SaticilarIthalatcilar ||
                    p.CategoryID == (int)Enums.BlogCategory.HaberlerKampanyalar).ToList();
                return View(result);
            }
        }

        #endregion

        #region Sistem ve Makalelerim

        [Route("Hesabim/Sistem-Ve-Makalelerim")]
        [Route("MyAccount/My-Systems-And-Articles")]
        [Authorize]
        public IActionResult MyArticles()
        {
            using (var service = new BlogPostService())
            {
                var posts = service.GetUserPosts(GetLoginID());
                if (posts == null)
                {
                    return View();
                }
                var result = posts.Where(p =>
                    p.CategoryID == (int)Enums.BlogCategory.OdyofilSistemleri ||
                    p.CategoryID == (int)Enums.BlogCategory.YazilarMakaleler).ToList();
                return View(result);
            }
        }

        #endregion

        #region Satın Alma ve Callback

        private ThreedsInitialize BuildPayment(User user, UserAddress userAddress, string ip, string cvc, string fullname,
            string month, string year, string number, int paymnetRequestId, DopingType doping, string callback)
        {
            Address address;
            string addressLine;
            if (userAddress == null)
            {
                addressLine = user?.District?.Name + " " + user?.City?.Name;
                address = new Address
                {
                    City = user?.City?.Name ?? "Istanbul",
                    Country = user?.Country?.Name ?? "Turkey",
                    ContactName = user?.Name,
                    Description = addressLine
                };
            }
            else
            {
                addressLine = userAddress.Address;
                address = new Address
                {
                    City = userAddress.City?.Name ?? userAddress.CityText,
                    Country = userAddress.Country?.Name ?? "Turkey",
                    ContactName = user.Name,
                    Description = userAddress.Address
                };
            }

            user.Name = user.Name.Trim();
            var name = user.Name;
            var surName = "Audiophile.org";
            if (user.Name.Contains(" "))
            {
                name = user.Name.Substring(0, user.Name.LastIndexOf(' ') + 1);
                surName = user.Name.Substring(user.Name.LastIndexOf(' ') + 1);
            }

            var buyer = new Buyer
            {
                Id = user.ID.ToString(),
                Name = name,
                Surname = surName,
                City = address.City,
                Country = address.Country,
                Email = user.Email,
                GsmNumber = user.MobilePhone.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", ""),
                IdentityNumber = (!string.IsNullOrEmpty(user.TC)) ? user.TC : "99999999999",
                Ip = ip,
                RegistrationAddress = addressLine
            };
            var card = new PaymentCard
            {
                Cvc = cvc,
                CardHolderName = fullname,
                ExpireMonth = month,
                ExpireYear = year,
                CardNumber = number,
                RegisterCard = 0
            };

            return IyzicoService.PayBanner(paymnetRequestId, doping.Price, card,
                buyer, address, address, doping.ID,
                doping.Name + "_" + doping.Day, callback);
        }

        [Route("Callback")]
        public IActionResult Callback(Iyzico3dCallback callback)
        {
            var lang = GetLang();
            var route = Constants.GetURL((int)Enums.Routing.ProfilSayfam, lang);

            if (string.IsNullOrEmpty(callback.PaymentId) || callback.PaymentId == "0")
            {
                Notification = new UiMessage(NotyType.error, new Localization().Get("3D Güvenlik hatası", "3D Secure Payment Error", lang),
                    10000);
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
                using (var bannerService = new BannerService())
                using (var blogService = new BlogPostService())
                using (var service = new PaymentRequestService())
                {
                    var payReq = service.Get(Convert.ToInt32(callback.ConversationId));
                    if (payReq != null)
                    {
                        payReq.PaymentId = threedsPayment.PaymentId;
                        payReq.ResponseDate = DateTime.Now;
                        payReq.PaymentTransactionID = threedsPayment.PaymentItems.First().PaymentTransactionId;
                        payReq.Status = Enums.PaymentRequestStatus.OnlineOdemeYapildi;
                        var update = service.Update(payReq);
                        if (update)
                        {
                            if (payReq.Type == Enums.PaymentType.BlogSureUzatma)
                            { //ödeme işlemi blog süre uzatması için ise, paymentRequest kaydından post'u bul, süresini uzat
                                var post = blogService.Get(payReq.ForeignModelID);
                                if (post.DopingEndDate < DateTime.Now) //bitiş süresi geçti ise bugüne gün ekle, geçmedi ise bitiş süresine gün ekle
                                    post.DopingEndDate = DateTime.Now.AddDays(payReq.Amount);
                                else
                                    post.DopingEndDate = post.DopingEndDate?.AddDays(payReq.Amount);

                                var updateBlog = blogService.Update(post);
                                if (updateBlog)
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı. Yazı süreniz uzatıldı.", "Payment completed. Your post was extended.", lang),
                                        10000);
                                    return Redirect(route);
                                }
                                else
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                                            "Ödeme işlemi tamamlandı fakat yazı süresi uzatılamadı. Lütfen site yönetimi ile iletişime geçin."
                                            , "Payment completed but post doping end time could not be updated. Please contact the site administration.", lang),
                                        10000);
                                }
                            }
                            if (payReq.Type == Enums.PaymentType.BannerSureUzatma)
                            { //ödeme işlemi banner süre uzatması için ise, paymentRequest kaydından banner'ı bul, süresini uzat
                                var banner = bannerService.Get(payReq.ForeignModelID);
                                if (banner.EndDate < DateTime.Now) //bitiş süresi geçti ise bugüne gün ekle, geçmedi ise bitiş süresine gün ekle
                                    banner.EndDate = DateTime.Now.AddDays(payReq.Amount);
                                else
                                    banner.EndDate = banner.EndDate?.AddDays(payReq.Amount);

                                var updateBanner = bannerService.Update(banner);
                                if (updateBanner)
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı. Banner süreniz uzatıldı.", "Payment completed. Your banner was extended.", lang),
                                        10000);
                                    return Redirect(route);
                                }
                                else
                                {
                                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                                            "Ödeme işlemi tamamlandı fakat yazı süresi uzatılamadı. Lütfen site yönetimi ile iletişime geçin."
                                            , "Payment completed but post doping end time could not be updated. Please contact the site administration.", lang),
                                        10000);
                                }
                            }

                            Notification = new UiMessage(NotyType.success, new Localization().Get("Ödeme işlemi tamamlandı", "Payment completed", lang),
                                10000);
                            return Redirect(route);
                        }
                    }
                    Notification = new UiMessage(NotyType.success, new Localization().Get(
                            "Ödeme işlemi tamamlandı fakat sisteme kayıt edilemedi. Lütfen site yönetimi ile iletişime geçin."
                            , "Payment completed but could not be saved in the system. Please contact the site administration.", lang),
                        10000);
                    return Redirect(route);
                }

            }

            Notification = new UiMessage(NotyType.error, threedsPayment.ErrorMessage, 10000);
            return Redirect(route);
        }

        #endregion



        [Authorize]
        [Route("ReadNotification")]
        public JsonResult ReadNotification(int id)
        {
            using (var service = new NotificationService())
            {
                var notify = service.Get(id);
                if (notify != null && notify.UserID == GetLoginID())
                {
                    notify.ReadedDate = DateTime.Now;
                    var update = service.Update(notify);
                    return Json(new { isSuccess = update });
                }
                return Json(new { isSuccess = false });
            }
        }

        public JsonResult MailTo(int id)
        {
            using (var service = new UserService())
            {
                var user = service.Get(id);
                if (user != null)
                {
                    return Json(new { isSuccess = true, email = user.Email });
                }
                return Json(new { isSuccess = false });
            }
        }

        public IActionResult TestError()
        {
            var y = 0;
            var x = 5 / y;
            return Content("result: " + x);
        }
    }
}