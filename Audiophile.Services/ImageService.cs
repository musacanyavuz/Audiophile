using System;
using System.IO;
using Audiophile.Models;
using FreeImageAPI;
using Microsoft.AspNetCore.Http;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Font = SixLabors.Fonts.Font;

namespace Audiophile.Services
{
    public class ImageService : IDisposable
    {
        public void Resize(string path, int size)
        {
            try
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path);
                using (var original = FreeImageBitmap.FromFile(path))
                {
                    int width, height;
                    if (original.Width > original.Height)
                    {
                        width = size;
                        height = original.Height * size / original.Width;
                    }
                    else
                    {
                        width = original.Width * size / original.Height;
                        height = size;
                    }
                    var resized = new FreeImageBitmap(original, width, height);
                    // JPEG_QUALITYGOOD is 75 JPEG.
                    // JPEG_BASELINE strips metadata (EXIF, etc.)
                    original.Dispose();
                    File.Delete(path);
                    resized.Save(path, FREE_IMAGE_FORMAT.FIF_JPEG,
                        FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD |
                        FREE_IMAGE_SAVE_FLAGS.JPEG_BASELINE);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR Resize() " + e.Message);
            }
        }

        public string Optimize75(string path, string directory, string fileName, bool gif = false, bool addExt = true)
        {
            try
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path);
                using (var original = FreeImageBitmap.FromFile(path))
                {
                    var resized = new FreeImageBitmap(original);
                    original.Dispose();
                    File.Delete(path);
                    if (!gif)
                    {
                        resized.Save(path, FREE_IMAGE_FORMAT.FIF_JPEG,
                            FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD |
                            FREE_IMAGE_SAVE_FLAGS.JPEG_BASELINE);
                        if (!addExt)
                            return directory + fileName;

                        var newPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + directory + fileName + ".jpg");
                        WatermarkService.WaterMark(newPath);

                        return directory + fileName + ".jpg";
                    }
                    else
                    {
                        resized.Save(path, FREE_IMAGE_FORMAT.FIF_GIF,
                            FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD);
                        return directory + fileName + ".gif";
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR Optimize75() " + e.Message);
            }
            return null;
        }

        public string OptimizeGif(string path)
        {
            try
            {

                var fullpath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path);
                using (var image = SixLabors.ImageSharp.Image.Load(fullpath))
                {
                    image.Mutate(i => i.Resize(200, 126));
                    var encoder = new GifEncoder()
                    {
                        ColorTableMode = GifColorTableMode.Global,
                        Quantizer = new OctreeQuantizer(32)
                    };
                    image.Save(fullpath, encoder);
                }
                return path;
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR OptimizeGif() " + e.Message);
            }
            return null;
        }


        public bool CreateThumbnail(int userID, string path, string directory, string fileName)
        {
            try
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + path);
                using (var newImage = Image.Load(path))
                {
                    double oldW = newImage.Width;
                    double oldH = newImage.Height;

                    var pth = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot" + directory, fileName);
                    if (oldH >= oldW)
                        newImage.Mutate(o => o.Resize(0, 305, KnownResamplers.Lanczos3));
                    else
                        newImage.Mutate(o => o.Resize(305, 0, KnownResamplers.Lanczos3));

                    newImage.Save(pth);
                    return true;
                }
            }
            catch (Exception e)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        Function = "ImageService.CreateThumbnail",
                        Message = userID + "id li kullanici thumbnail resmi icin " + path + "su yoldaki" + directory + "su klasor altina" + fileName + " isimle kaydederken hata aldi.",
                        Detail = e.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }
            }

            return false;
        }



        public void Dispose()
        {

        }
    }
}