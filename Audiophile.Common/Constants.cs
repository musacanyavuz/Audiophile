using System.Net.NetworkInformation;

namespace Audiophile.Common
{
    public class Constants
    {
        public const string RecaptchaSiteKey = "6LeVNngUAAAAAPkjVdGRfqxxjT4bVM3G5kpm5xYf";
        public const string RecaptchaSecretKey = "6LeVNngUAAAAAAAxh4JvwM3-8edILMs19tgzx7Qh";
        
        public const string messageSuccessTr = "Şifre sıfırlama bağlantısı mail adresine gönderildi.";
        public const string messageSuccessEn = "Password reset link sent to mail address.";
        public const string messageDangerTr =
            "Şifre bağlantısı gönderme işlemi başarısız oldu. Lütfen iletişim sayfasını kullanarak destek talebi oluşturun.";
        public const string messageDangerEn =
            "Sending a password connection failed. Please create a support request using the contact page.";
        
        public string GetGender(int id)
        {
            switch (id)
            {
                case 1:
                    return "Erkek";
                case 2:
                    return "Kadın";
            }
            return "";
        }

        public static int GetLang(string code)
        {
            switch (code)
            {
                case "tr":
                    return 1;
                case "en":
                    return 2;
            }
            return 1;
        }

        public static string GetMoney(int code)
        {
            switch (code)
            {
                 case 1:
                     return "TL";
                 case 2:
                     return "USD";
                 case 3:
                     return "EUR";
                 case 4:
                     return "GBP";
                 default:
                     return "";
            }
        }

        public static string GetBlogCategoryTitle(int id, int lang)
        {
            if (lang == 1)
            {
                switch (id)
                {
                    case (int)Enums.BlogCategory.HaberlerKampanyalar:
                        return "Haberler, Kampanyalar";
                    case (int)Enums.BlogCategory.SaticilarIthalatcilar:
                        return "Satıcılar, İthalatçılar";
                    case (int)Enums.BlogCategory.OdyofilSistemleri:
                        return "Odyofil Sistemleri";
                    case (int)Enums.BlogCategory.YazilarMakaleler:
                        return "Yazılar, Makaleler";
                }
            }
            else
            {
                switch (id)
                {
                    case (int)Enums.BlogCategory.HaberlerKampanyalar:
                        return "News, Campaigns";
                    case (int)Enums.BlogCategory.SaticilarIthalatcilar:
                        return "Sellers, Importers";
                    case (int)Enums.BlogCategory.OdyofilSistemleri:
                        return "Audiophile Systems";
                    case (int)Enums.BlogCategory.YazilarMakaleler:
                        return "Articles";
                }
            }
            return "";
        }
        
        public static string GetBlogCategorySlug(int id, int lang)
        {
            if (lang == 1)
            {
                switch (id)
                {
                    case (int)Enums.BlogCategory.HaberlerKampanyalar:
                        return "haberler-kampanyalar";
                    case (int)Enums.BlogCategory.SaticilarIthalatcilar:
                        return "saticilar-ithalatcilar";
                    case (int)Enums.BlogCategory.OdyofilSistemleri:
                        return "odyofil-sistemleri";
                    case (int)Enums.BlogCategory.YazilarMakaleler:
                        return "yazilar-makaleler";
                }
            }
            else
            {
                switch (id)
                {
                    case (int)Enums.BlogCategory.HaberlerKampanyalar:
                        return "news-campaigns";
                    case (int)Enums.BlogCategory.SaticilarIthalatcilar:
                        return "sellers-importers";
                    case (int)Enums.BlogCategory.OdyofilSistemleri:
                        return "audiophiles-systems";
                    case (int)Enums.BlogCategory.YazilarMakaleler:
                        return "articles";
                }
            }
            return "";
        }

        public static string GetDopingGroupName(Enums.DopingGroup group, int lang)
        {
            if (lang == 1)
            {
                switch (group)
                {
                    case Enums.DopingGroup.HaberKampanya:
                        return "Haber, Kampanya";
                    case Enums.DopingGroup.SaticiIthalatci:
                        return "Satıcılar, İthalatçılar"; 
                }
            }
            else
            {
                switch (group)
                {
                    case Enums.DopingGroup.HaberKampanya:
                        return "News, Campaings";
                    case Enums.DopingGroup.SaticiIthalatci:
                        return "Sellers, Importers"; 
                }
            }
            return "";
        }
        
        public static string UrlTranslate(string from, int fromLang)
        {//dil değişikliğinde aynı sayfada kalmak ve url'in dilini değiştirmek için 
         //tüm routingi tarar ve kaynak dildeki routingin routingKodunu bulur
         //bulduğu rotuing kodu ile hedef dildeki çevirisini çeker ve return eder   
            var toLang = (fromLang == 1) ? 2 : 1; //hedefDil dil; gelen dil 1 ise 2 olsun, değil ise 1 olsun
            for (var i = 1; i < 100; i++)
            {
                if (GetURL(i, fromLang) == from)
                {
                    return GetURL(i, toLang);
                }
            }
            return "";
        }

        public static string GetURL(Enums.Routing routing, int lang)
        {
            return GetURL((int) routing, lang);
        }
        
        public static string GetURL(int routing, int lang)
        {
            if (lang == 1)    
            {
                switch (routing)
                {
                    case (int)Enums.Routing.Anasayfa:
                        return "/";
                    case (int)Enums.Routing.UyeOl:
                        return "/UyeOl";
                    case (int)Enums.Routing.GirisYap:
                        return "/GirisYap";
                    case (int)Enums.Routing.IlanEkle:
                        return "/IlanEkle";
                    case (int)Enums.Routing.IlanDuzenle:
                        return "/Hesabim/IlanDuzenle";
                    case (int)Enums.Routing.Hakkimizda:
                        return "/Destek/Hakkimizda";
                    case (int)Enums.Routing.Videolar:
                        return "/Destek/Videolar";
                    case (int)Enums.Routing.Yardim:
                        return "/Destek/Yardim";
                    case (int)Enums.Routing.KullanimKosullari:
                        return "/Destek/KullanimKosullari";
                    case (int)Enums.Routing.GuvenliOdemeGittiBu:
                        return "/Destek/GuvenliOdemeGittiBu";
                    case (int)Enums.Routing.Ucretler:
                        return "/Destek/Ucretler";
                    case (int)Enums.Routing.Iletisim:
                        return "/Destek/Iletisim";
                    case (int)Enums.Routing.Blog_HaberlerKampanyalar:
                        return "/Blog/Haberler-Kampanyalar";
                    case (int)Enums.Routing.Blog_SaticilarIthalatcilar:
                        return "/Blog/Saticilar-Ithalatcilar";
                    case (int)Enums.Routing.Blog_OdyofilSistemler:
                        return "/Blog/Odyofil-Sistemler";
                    case (int)Enums.Routing.Blog_YazilarMakaleler:
                        return "/Blog/Yazilar-Makaleler";
                    case (int)Enums.Routing.UyelikBilgilerim:
                        return "/Hesabim/Uyelik-Bilgilerim";
                    case (int)Enums.Routing.AdresBilgierim:
                        return "/Hesabim/Adres-Bilgilerim";
                    case (int)Enums.Routing.AdresEkle:
                        return "/Hesabim/Adres-Bilgilerim/Adres-Ekle";
                    case (int)Enums.Routing.AdresDuzenle:
                        return "/Hesabim/Adres-Bilgilerim/Adres-Duzenle";
                    case (int)Enums.Routing.GuvenliOdemeBilgilerim:
                        return "/Hesabim/Guvenli-Odeme-Bilgilerim";
                    case (int)Enums.Routing.ProfilSayfam:
                        return "/Hesabim/Profilim";
                    case (int)Enums.Routing.SifreDegistir:
                        return "/Hesabim/Sifre-Degistir";
                    case (int)Enums.Routing.CariHesabim:
                        return "/Hesabim/Cari-Hesabim";
                    case (int)Enums.Routing.YaptigimYorumlar:
                        return "/Hesabim/Yaptigim-Yorumlar";
                    case (int)Enums.Routing.BegendigimIlanlar:
                        return "/Hesabim/Begendigim-Ilanlar";
                    case (int)Enums.Routing.BannerEkle:
                        return "/Hesabim/Banner-Ekle";
                    case (int)Enums.Routing.BannerDuzenle:
                        return "/Hesabim/Banner-Duzenle";
                    case (int)Enums.Routing.Bannerlarim:
                        return "/Hesabim/Bannerlarim";
                    case (int)Enums.Routing.BlogEkle:
                        return "/Hesabim/Blog-Ekle";
                    case (int)Enums.Routing.HaberVeFirmalarim:
                        return "/Hesabim/Haber-Ve-Firmalarim"; 
                    case (int)Enums.Routing.SistemVeMakalelerim:
                        return "/Hesabim/Sistem-Ve-Makalelerim";
                    case (int)Enums.Routing.Ilanlarim:
                        return "/Hesabim/Ilanlarim";
                    case (int)Enums.Routing.CikisYap:
                        return "/CikisYap";
                    case (int)Enums.Routing.SifremiUnuttum:
                        return "/SifremiUnuttum";
                    case (int)Enums.Routing.SifremiSifirla:
                        return "/SifremiSifirla";
                    case (int)Enums.Routing.HesabimiAktiflestir:
                        return "/HesabimiAktiflestir";
                    case (int)Enums.Routing.Ilan:
                        return "/Ilan";
                    case (int)Enums.Routing.SaticiDigerIlanlari:
                        return "";
                }
            }
            else
            {
                switch (routing)
                {
                    case (int)Enums.Routing.Anasayfa:
                        return "/";
                    case (int)Enums.Routing.UyeOl:
                        return "/Register";
                    case (int)Enums.Routing.GirisYap:
                        return "/Login";
                    case (int)Enums.Routing.IlanEkle:
                        return "/AddListing";
                    case (int)Enums.Routing.IlanDuzenle:
                        return "/MyAccount/EditListing";
                    case (int)Enums.Routing.Hakkimizda:
                        return "/Support/AboutUs";
                    case (int)Enums.Routing.Videolar:
                        return "/Support/Videos";
                    case (int)Enums.Routing.Yardim:
                        return "/Support/Help";
                    case (int)Enums.Routing.KullanimKosullari:
                        return "/Support/TermsOfUse";
                    case (int)Enums.Routing.GuvenliOdemeGittiBu:
                        return "/Support/SecurePayment";
                    case (int)Enums.Routing.Ucretler:
                        return "/Support/Fees";
                    case (int)Enums.Routing.Iletisim:
                        return "/Support/Contact";
                    case (int)Enums.Routing.Blog_HaberlerKampanyalar:
                        return "/Blog/News-Campaigns";
                    case (int)Enums.Routing.Blog_SaticilarIthalatcilar:
                        return "/Blog/Sellers-Importers";
                    case (int)Enums.Routing.Blog_OdyofilSistemler:
                        return "/Blog/Audiophile-Systems";
                    case (int)Enums.Routing.Blog_YazilarMakaleler:
                        return "/Blog/Articles";
                    case (int)Enums.Routing.UyelikBilgilerim:
                        return "/MyAccount/Personal-Information";
                    case (int)Enums.Routing.AdresBilgierim:
                        return "/MyAccount/Postage-Address";
                    case (int)Enums.Routing.AdresEkle:
                        return "/MyAccount/Postage-Address/Add-Address";
                    case (int)Enums.Routing.AdresDuzenle:
                        return "/MyAccount/Postage-Address/Edit-Address";
                    case (int)Enums.Routing.GuvenliOdemeBilgilerim:
                        return "/MyAccount/Secure-Payment-Details";
                    case (int)Enums.Routing.ProfilSayfam:
                        return "/MyAccount/Profile";
                    case (int)Enums.Routing.SifreDegistir:
                        return "/MyAccount/Change-Password";
                    case (int)Enums.Routing.CariHesabim:
                        return "/MyAccount/Commercial-Account";
                    case (int)Enums.Routing.YaptigimYorumlar:
                        return "/MyAccount/MyComments";
                    case (int)Enums.Routing.BegendigimIlanlar:
                        return "/MyAccount/MyLikes";
                    case (int)Enums.Routing.BannerEkle:
                        return "/MyAccount/Add-Banner";
                    case (int)Enums.Routing.BannerDuzenle:
                        return "/MyAccount/Edit-Banner";
                    case (int)Enums.Routing.Bannerlarim:
                        return "/MyAccount/MyBanners";
                    case (int)Enums.Routing.BlogEkle:
                        return "/MyAccount/Add-Blog";
                    case (int)Enums.Routing.HaberVeFirmalarim:
                        return "/MyAccount/My-News-And-Companies"; 
                    case (int)Enums.Routing.SistemVeMakalelerim:
                        return "/MyAccount/My-Systems-And-Articles";
                    case (int)Enums.Routing.Ilanlarim:
                        return "/MyAccount/My-Listing";
                    case (int)Enums.Routing.CikisYap:
                        return "/SignOut";
                    case (int)Enums.Routing.SifremiUnuttum:
                        return "/ForgottenPassword";
                    case (int)Enums.Routing.SifremiSifirla:
                        return "/ResetPassword";
                    case (int)Enums.Routing.HesabimiAktiflestir:
                        return "/AccountActivation";
                    case (int)Enums.Routing.Ilan:
                        return "/Advert";
                    case (int)Enums.Routing.SaticiDigerIlanlari:
                        return "";
                }
            }
            return "#";
        }

        public static string GetPaymentMethod(int id, int lang)
        {
            if (lang == 1)
            {
                switch (id)
                {
                    case 1:
                        return "Banka Havalesi İle Peşin";
                    case 2:
                        return "Banka Havalesi İle Teslimattan Sonra";
                    case 3:
                        return "Elden Ödeme";
                    case 4:
                        return "Kredi Kartı İle Güvenli Ödeme GittiBu";
                }
            }
            else
            {
                switch (id)
                {
                    case 1:
                        return "Bank Transfer Before Shipping";
                    case 2:
                        return "Bank Transfer After Shipping";
                    case 3:
                        return "Cash";
                    case 4:
                        return "Credit Card";
                }
            }
            return "";
        }

        public static string Array2String(int[] array)
        {
            var result = "";
            foreach (var i in array)
            {
                result += i + ",";
            }
            result = result.Remove(result.Length - 1);
            return result;
        }

        public static string GetPaymentRequestStatus(Enums.PaymentRequestStatus s, int lang, int id = 0)
        {
            string text;
            switch (s)
            {
                case Enums.PaymentRequestStatus.Bekleniyor:
                    return (lang == 1) ? "Bekleniyor" : "Waiting";
                case Enums.PaymentRequestStatus.KargoyaVerildi:
                    text = (lang == 1) ? "Kargoya Verildi" : "Shipping";
                    return id > 0 ? $"<button type=\"button\" class=\"btn btn-sm btn-secondary\" onclick=\"cargoInfo({id})\">{text}</button>" : text;
                case Enums.PaymentRequestStatus.TeslimEdildi:
                    text = (lang == 1) ? "Teslim Edildi" : "Delivered";
                    return id > 0 ? $"<button type=\"button\" class=\"btn btn-sm btn-secondary\" onclick=\"cargoInfo({id})\">{text}</button>" : text;
                case Enums.PaymentRequestStatus.AliciIptalEtti:
                    text = (lang == 1) ? "Alıcı İptal Etti" : "Buyer Canceled";
                    return id > 0 ? $"<button type=\"button\" class=\"btn btn-sm btn-secondary\" onclick=\"cargoInfo({id})\">{text}</button>" : text;
                case Enums.PaymentRequestStatus.SaticiIptalEtti:
                    text = (lang == 1) ? "Satıcı İptal Etti" : "Seller Canceled";
                    return id > 0 ? $"<button type=\"button\" class=\"btn btn-sm btn-secondary\" onclick=\"cargoInfo({id})\">{text}</button>" : text;
                default:
                    return s.ToString();
            }
        }
    }
}