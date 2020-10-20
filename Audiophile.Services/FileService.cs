using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Audiophile.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SixLabors.ImageSharp;

namespace Audiophile.Services
{
    public class FileService : IDisposable
    {
        private int _numberOfRetries = 0;
        public string FileUpload(int userID, IFormFile file, string path, string fileName)
        {
            try
            {
                if (fileName.ToLower().Contains(".jpeg"))
                {
                    fileName = fileName.ToLower().Replace(".jpeg", ".jpg");
                }

                string pth = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path, fileName);
                using (var x = file.OpenReadStream())
                {
                    using (var newImage = Image.Load(x))
                    {
                        if(!WatermarkService.WaterMarkNew(newImage, userID))
                        {
                            return null;
                        }
                        newImage.Save(pth);
                    }
                }

                var imageUrl = path + fileName;
                return imageUrl;
            }

            catch (Exception e)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        Function = "FileService.FileUpload",
                        Message = userID + "id li kullanici " + path + " su yola " + fileName + " isimle kaydederken hata aldi.",
                        Params = JsonConvert.SerializeObject(new List<string>(){
                            file.ContentDisposition,
                            file.ContentType,
                            file.FileName,
                            file.Length.ToString(),
                            file.Name,
                        }),
                        Detail = e.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
                return null;
            }
        }
        public string FileUpload(int userID, IFormFile file, string path, string fileName,bool insertWaterMark=true)
        {
            try
            {
                if (fileName.ToLower().Contains(".jpeg"))
                {
                    fileName = fileName.ToLower().Replace(".jpeg", ".jpg");
                }

                string pth = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path, fileName);
                using (var x = file.OpenReadStream())
                {
                    using (var newImage = Image.Load(x))
                    {
                        if (insertWaterMark && !WatermarkService.WaterMarkNew(newImage, userID))
                        {
                            return null;
                        }
                        newImage.Save(pth);
                    }
                }

                var imageUrl = path + fileName;
                return imageUrl;
            }

            catch (Exception e)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        Function = "FileService.FileUpload",
                        Message = userID + "id li kullanici " + path + " su yola " + fileName + " isimle kaydederken hata aldi.",
                        Params = JsonConvert.SerializeObject(new List<string>(){
                            file.ContentDisposition,
                            file.ContentType,
                            file.FileName,
                            file.Length.ToString(),
                            file.Name,
                        }),
                        Detail = e.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
                return null;
            }
        }

        public bool FileUpload(byte[] file, string path, string fileName)
        {
            try
            {
                var pth = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path, fileName);
                File.WriteAllBytes(pth, file);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public void DeleteFile(string path)
        {

            try
            {
                var pth = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path);
                File.Delete(pth);
            }
            catch (Exception e)
            {

            }
        }

        public void DeleteFile(string path, string fileName)
        {

            for (int i = 0; i < 4; i++)
            {
                try
                {
                    string pth = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path, fileName);
                    if (File.Exists(pth))
                    {
                        File.Delete(pth);
                        break;
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        public void Dispose()
        {

        }
    }
}