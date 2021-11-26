using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audiophile.Common;
using Audiophile.Models;
using Audiophile.Services;
using Dapper.FastCrud;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Audiophile.Importer
{
    class Program
    {
        private static List<City> cities;
        private static List<District> districts;
        private static List<AdvertCategory> advertCategories;
        private static List<Advert> adverts;
        static void Main(string[] args)
        {
            Console.WriteLine("Program Started...Excel File Reading...");
            //using (var adCatService = new AdvertCategoryService())
            //using (var baseService = new BaseService())
            //using (var service = new PublicService())
            //{
            //    cities = service.GetCities();
            //    districts = service.GetDistricts();
            //    advertCategories = adCatService.GetAll();
            //    adverts = baseService.GetConnection().Find<Advert>().ToList();
            //}
            
            //ImportUsers();
            //ImportAds();
            //ImportAdImages();

            //ImportBlogs(); //859
            
            Console.WriteLine("Exit.");
            Console.ReadKey();
        }

        #region ImportUsers

        static void ImportUsers()
        {
            if (File.Exists("uyeler_2.xlsx"))
            {
                var fileInfo = new FileInfo("./uyeler_2.xlsx");
                
                using (var package = new ExcelPackage(fileInfo))
                { 
                    var worksheet = package.Workbook.Worksheets[0];
                    var userList = new List<User>();
                    var rows = worksheet.Dimension.Rows;
                    for (int r = 2; r < rows; r++) //2'den başla
                    {
                        userList.Add(Cell2User(worksheet.Cells, r));
                    }
                    Console.WriteLine("Tanımlanan Kullanıcı Miktarı: "+userList.Count);
                    Console.WriteLine("Veritabanına yazılıyor...");
                    
                    using (var service = new UserService())
                    {
                        var insert = service.BulkInsert(userList);
                        if (insert)
                            Console.WriteLine("Aktarım tamamlandı.");
                        else 
                            Console.WriteLine("HATA OLUŞTU");
                    }
                }
            }
        }
 
        static User Cell2User(ExcelRange cell, int row)
        {
            try
            {
                if (row % 10 == 0)
                {
                    Console.WriteLine("Reading... Row: "+row);
                }
                var user = new User();
                user.ID = int.Parse(cell[row, 1].Value.ToString());
                user.UserName = cell[row, 2].Value?.ToString();
                user.Name = cell[row, 3].Value.ToString();
                user.Email = cell[row, 4].Value.ToString();
                user.GenderID = int.Parse(cell[row, 5].Value.ToString()) == 2 ? 2 : 1;
                user.Password = cell[row, 6].Value?.ToString();
                user.IsActive = int.Parse(cell[row, 7].Value.ToString()) == 1;
                user.LastLoginDate = TryParseDateTime(cell[row, 8].Value?.ToString());
                // 9 = son cikis
                user.MobilePhone = cell[row, 10].Value?.ToString();
                user.WorkPhone = cell[row, 11].Value?.ToString();
                user.BirthDate = TryParseDateTime(cell[row, 12].Value?.ToString());
                // 13 = ilce id
                user.WebSite = cell[row, 14].Value?.ToString();
                user.About = cell[row, 15].Value?.ToString();
                user.LanguageID = int.Parse(cell[row, 16].Value.ToString());
                user.CreatedDate = TryParseDateTime(cell[row, 17].Value?.ToString());
                user.InMailing = cell[row, 18].Value?.ToString() == "1";
                // 19 = duyuru
                user.ProfilePicture = GenerateProfilePictureUrl(cell[row, 20].Value?.ToString());
                user.TC = cell[row, 21].Value?.ToString();
                user.CityID = GetCityID(cell[row, 23].Value?.ToString());
                if (user.CityID > 0)
                {
                    user.DistrictID = GetDistrictID(cell[row, 22].Value?.ToString(), user.CityID);
                }
                user.CountryID = 1;
                 
                user.Role = "User";
                
                return user;
            }
            catch (Exception e)
            {
                Console.WriteLine("HATA OLUŞTU | ROW : "+row);
                Console.WriteLine(e.Message);
            }
            return null;
        }

        static int GetCityID(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            var city = cities.SingleOrDefault(x => x.Name == s);
            if (city == null)
                return 0;

            return city.ID;
        }
        
        static int GetDistrictID(string s, int city)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            var district = districts.FirstOrDefault(x => x.Name == s && x.CityID == city);
            if (district == null)
                return 0;

            return district.ID;
        }
        
        static string GenerateProfilePictureUrl(string s)
        {
            if (string.IsNullOrEmpty(s))
                return null;
            if (s.Contains("http"))
            {
                return s;
            }
            return "/Upload/Users/" + s;
        }

        #endregion

        #region ImportAds

        static void ImportAds()
        {
            if (File.Exists("ilans.xlsx"))
            {
                var fileInfo = new FileInfo("./ilans.xlsx");
                
                using (var package = new ExcelPackage(fileInfo))
                { 
                    var worksheet = package.Workbook.Worksheets[0];
                    var adList = new List<Advert>();
                    var rows = worksheet.Dimension.Rows;
                    for (int r = 2; r < rows; r++) //2'den başla
                    {
                        adList.Add(Cell2Ad(worksheet.Cells, r));
                        if (r % 10 == 0)
                        {
                            Console.WriteLine("Readed & Mapped Row Count: "+r);
                        }
                    }
                    Console.WriteLine("Total Count: "+adList.Count);
                    Console.WriteLine("Inserting to database...");
                    using (var service = new AdvertService())
                    {
                        var insert = service.BulkInsert(adList);
                        if (!insert)
                            Console.WriteLine("INSERT ERROR");
                        else
                            Console.WriteLine("INSERT SUCCEEDED");
                    }
                }
            }
        }

        static Advert Cell2Ad(ExcelRange cell, int row)
        {
            //eski db'ye göre -1: aktif 0: pasif demek.
            try
            {
                var ad = new Advert();
                ad.ID = int.Parse(cell[row, 1].Value.ToString());
                ad.Title = cell[row, 2].Value?.ToString();
                ad.Content = GetContent(cell[row, 3].Value?.ToString()?.Trim());
                ad.NewProductPrice = ParseInt(cell[row, 4].Value);
                ad.Price = ParseInt(cell[row, 5].Value);
                ad.IsAvailableForSwap = cell[row, 6].Value?.ToString() == "1";
                ad.IsAvailableBargain = cell[row, 7].Value?.ToString() == "1";
                ad.PaymentMethodID = GetPaymentMethod(cell, row); // 8,9,10,11
                ad.FreeShipping = cell[row, 12].Value?.ToString() == "1";
                ad.ShippingTypeID = ParseInt(cell[row, 13].Value);
                ad.OriginalBox = cell[row, 14].Value?.ToString() == "1";
                ad.TermsOfUse = cell[row, 15].Value?.ToString() == "1";
                ad.ProductStatus = cell[row, 16].Value?.ToString();
                ad.WebSite = cell[row, 17].Value?.ToString();
                // 18: kategori id
                ad.CategoryID = GetAdCategoryID(cell[row, 19].Value?.ToString());
                ad.CreatedDate = DateTime.Parse(cell[row, 20].Value?.ToString());
                ad.LastUpdateDate = TryParseDateTime(cell[row, 21].Value?.ToString());
                ad.UserID = int.Parse(cell[row, 22].Value.ToString());
                ad.ViewCount = ParseInt(cell[row, 23].Value);
                ad.MoneyTypeID = ParseInt(cell[row, 24].Value);
                ad.StockAmount = ParseInt(cell[row, 25].Value);
                ad.IsActive = cell[row, 26].Value?.ToString() == "1";
                //27: onay durumu
                ad.ShippingPrice = ParseInt(cell[row, 28].Value);
                ad.UseSecurePayment = cell[row, 29].Value?.ToString() == "1";
                ad.ProductDefects = cell[row, 30].Value?.ToString();
                ad.IsDeleted = cell[row, 31].Value?.ToString() == "1";
                ad.Brand = cell[row, 32].Value?.ToString();
                ad.AvailableInstallments = cell[row, 33].Value?.ToString();
                ad.Model = cell[row, 34].Value?.ToString();
                 
                return ad;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        static string GetContent(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            if (s.Length > 5000)
                return s.Substring(0, 5000);
            return s;
        }

        static int GetAdCategoryID(string catName)
        {
            if (string.IsNullOrEmpty(catName))
                return 0;
            catName = catName.Trim();
            foreach (var advertCategory in advertCategories)
            {
                if (advertCategory.Name == catName)
                {
                    return advertCategory.ID;
                }
                else if (advertCategory.ChildCategories!=null && advertCategory.ChildCategories.Any())
                {
                    foreach (var childCategory in advertCategory.ChildCategories)
                    {
                        if (childCategory.Name == catName)
                        {
                            return childCategory.ID;
                        }
                    }
                }
            }
            return 0;
        }

        static int GetPaymentMethod(ExcelRange cell, int row)
        {
            var bankaHavalesiIlePesin = cell[row, 8].Value?.ToString();
            var bankaHavalesiIleTeslimattanSonra = cell[row, 9].Value?.ToString();
            var eldenNakit = cell[row, 10].Value?.ToString();
            var krediKarti = cell[row, 11].Value?.ToString();

            if (bankaHavalesiIlePesin == "-1")
                return (int)Enums.PaymentMethods.BankaHavalesiIlePesin;
            if (bankaHavalesiIleTeslimattanSonra == "-1")
                return (int) Enums.PaymentMethods.BankaHavalesiIleTeslimattanSonra;
            if (eldenNakit == "-1")
                return (int) Enums.PaymentMethods.EldenOdeme;
            if (krediKarti == "-1")
                return (int) Enums.PaymentMethods.KrediKartiIle;

            return (int)Enums.PaymentMethods.EldenOdeme;
        }

        #endregion
         
        #region ImportAdImages

        static void ImportAdImages()
        {
            var fileInfo = new FileInfo("./ilan_resimler.xlsx");
            var updateAdverts = new List<Advert>();
            
            using (var package = new ExcelPackage(fileInfo))
            { 
                var worksheet = package.Workbook.Worksheets[0];
                var photoList = new List<AdvertPhoto>();
                var rows = worksheet.Dimension.Rows;
                for (int r = 2; r < rows; r++) //2'den başla
                {
                    var photo = new AdvertPhoto()
                    {
                        ID = int.Parse(worksheet.Cells[r, 1].Value.ToString()),
                        AdvertID = ParseInt(worksheet.Cells[r, 2].Value?.ToString()),
                        Source = "/Upload/Sales/Old/"+worksheet.Cells[r, 4].Value,
                        Thumbnail = "/Upload/Sales/Old/"+worksheet.Cells[r, 4].Value,
                        CreatedDate = ParseDateTime(worksheet.Cells[r,6].Value?.ToString())
                    };
                    photoList.Add(photo);
                    
                    if (worksheet.Cells[r, 7].Value.ToString() == "1")
                    {
                        var advert = adverts.SingleOrDefault(x => x.ID == photo.AdvertID);
                        if (advert != null)
                        {
                            advert.Thumbnail = photo.Source;
                            advert.CoverPhoto = photo.Source;
                            updateAdverts.Add(advert);
                            //Console.WriteLine("UPDATE: "+advert.ID+" "+advert.Title+" "+advert.CoverPhoto);
                        }
                    }
                    if (r % 10 == 0)
                    {
                        //Console.WriteLine("Readed & Mapped Row Count: "+r);
                    }
                }
                Console.WriteLine("Total Image Count: "+photoList.Count);
                Console.WriteLine("Total Update Ad Count: "+updateAdverts.Count);
                
                Console.WriteLine("Inserting to database...");

                using (var service = new AdvertService())
                {
                    var insert = service.BulkInsertAdvertPhoto(photoList);
                    if (insert)
                    {
                        Console.WriteLine("INSERT FINISH SUCCESS... STARTING UPDATES...");
                        foreach (var advert in updateAdverts)
                        {
                            var update = service.Update(advert);
                            if (!update)
                            {
                                Console.WriteLine("UPDATE ERROR");
                                break;
                            }
                        }
                        Console.WriteLine("UPDATES FINISHED");
                    }
                    else
                    {
                        Console.WriteLine("INSERT ERROR");
                    }
                }
                
            }
        }

        #endregion
        
        static DateTime? TryParseDateTime(string s)
        {
            try
            {
                return DateTime.Parse(s);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        static DateTime ParseDateTime(string s)
        {
            try
            {
                return DateTime.Parse(s.Trim());
            }
            catch (Exception e)
            {
                return DateTime.Now;
            }
        }
        
        static int ParseInt(object s)
        {
            try
            {
                return int.Parse(s.ToString());
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        #region ImportBlog

         static void ImportBlogs()
        {
            var fileInfo = new FileInfo("./blogs.xlsx");

            using (var package = new ExcelPackage(fileInfo))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var blogs = new List<BlogPost>();
                var rows = worksheet.Dimension.Rows;
                for (int r = 2; r < rows; r++) //2'den başla
                {
                    var post = new BlogPost();
                    post.ID = int.Parse(worksheet.Cells[r, 1].Value.ToString());
                    post.Title = worksheet.Cells[r, 2].Value?.ToString();
                    post.ShortContent = worksheet.Cells[r, 3].Value?.ToString();
                    post.Content = worksheet.Cells[r, 4].Value?.ToString();
                    var json = worksheet.Cells[r, 5].Value?.ToString();
                    post.Images = GetImages(json, post.ID);
                    if (post.Images != null)
                    {
                        post.CoverImage = post.Images.First().Source;
                        post.Thumbnail = post.Images.First().Source;
                    }
                    //6: slug
                    post.CreatedDate = DateTime.Parse(worksheet.Cells[r, 7].Value.ToString());
                    //8: update
                    post.UserID = ParseInt(worksheet.Cells[r, 9].Value);
                    //10: row
                    post.IsActive = worksheet.Cells[r, 11].Value?.ToString() == "1";
                    //12 delete
                    //13 status
                    //14 username
                    post.CategoryID = GetBlogCategoryID(ParseInt(worksheet.Cells[r, 15].Value));
                    post.ViewCount = ParseInt(worksheet.Cells[r, 16].Value);
                    post.Tags = worksheet.Cells[r, 17].Value?.ToString();
                    //18: start
                    post.DopingEndDate = ParseDateTime(worksheet.Cells[r, 19].Value?.ToString());
                    post.PaymentAmount = ParseInt(worksheet.Cells[r, 20].Value);
                    blogs.Add(post);
                    //Console.WriteLine(post.ID+" | "+post.Title+" | "+post.Images?.Count());
                }
                
                Console.WriteLine("Post Count: "+blogs.Count);
                Console.WriteLine("INSERTING...");
                using (var service = new BlogPostService())
                {
                    foreach (var post in blogs)
                    {
                        var insertPost = service.Insert(post);
                        if (insertPost)
                        {
                            if (post.Images != null)
                            {
                                foreach (var postImage in post.Images)
                                {
                                    service.InsertImage(postImage);
                                }
                            }
                        }
                        else
                            Console.WriteLine("POST INSERT ERROR - ID: "+post.ID);
                    }
                }
                
            }
        }

        static List<BlogPostImage> GetImages(string json, int id)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return null;
            
                var data = JsonConvert.DeserializeObject<BlogSmallImage>(json);
                if (data != null && data.Image != null && data.Image.Any())
                {
                    var result = new List<BlogPostImage>();
                    foreach (var image in data.Image)
                    {
                        result.Add(new BlogPostImage()
                        {
                            Source = "/Upload/Blog/Old/"+image.yol,
                            CreatedDate = DateTime.Now,
                            Thumbnail = "/Upload/Blog/Old/"+image.yol, 
                            PostID = id
                        });
                    }

                    return result;
                }
            }
            catch (Exception e)
            {
            }
            return null;
        }

        static int GetBlogCategoryID(int id)
        {
            switch (id)
            {
                case 8:
                    return (int) Enums.BlogCategory.SaticilarIthalatcilar;
                case 9:
                    return (int) Enums.BlogCategory.HaberlerKampanyalar;
                case 58:
                    return (int) Enums.BlogCategory.OdyofilSistemleri;
                case 59:
                    return (int) Enums.BlogCategory.YazilarMakaleler;
                default:
                    return 0;
            }
        }



        class BlogSmallImage
        {
            public int id { get; set; }
            public List<Image> Image { get; set; }
        }

        class Image
        {
            public int ID { get; set; }
            public string yol { get; set; }
        }

        #endregion

       
    }
}