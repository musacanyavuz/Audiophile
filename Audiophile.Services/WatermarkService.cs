using System;
using System.Numerics;
using Audiophile.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using FontFamily = System.Drawing.FontFamily;

namespace Audiophile.Services
{
    public class WatermarkService
    {
        public static void WaterMark(string path)
        {
            var watermark = "www.audiophile.org";
            try
            {
                using (var img = Image.Load(path))
                {
                    Font font = SystemFonts.CreateFont("Arial", 10, FontStyle.Bold); 
                    SizeF size = TextMeasurer.Measure(watermark, new RendererOptions(font));
                    var imgW = img.Width / 2;
                    var imgH = img.Height / 2;
                    float scalingFactor = Math.Min(imgW / size.Width, imgH / size.Height);
                    Font scaledFont = new Font(font, scalingFactor * font.Size);
                    var h = Convert.ToInt32(img.Height / 1.2);
                    var center = new PointF(img.Width / 2, h);
                    var textGraphicOptions = new TextGraphicsOptions(true) {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    img.Mutate(i => i.DrawText(textGraphicOptions, watermark, scaledFont, 
                        new Rgba32(255,255,255,0.3F) , center));
                    
                    img.Save(path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Watermark Error: "+e.Message);
            }
        }
        public static bool WaterMarkNew(Image<Rgba32> img,int userID)
        {
            const string watermark = "www.audiophile.org";
            try
            {
                var font = SystemFonts.CreateFont("Arial", 10, FontStyle.Bold);
                var size = TextMeasurer.Measure(watermark, new RendererOptions(font));
                var imgW = img.Width / 2;
                var imgH = img.Height / 2;
                var scalingFactor = Math.Min(imgW / size.Width, imgH / size.Height);
                var scaledFont = new Font(font, scalingFactor * font.Size);
                var h = Convert.ToInt32(img.Height / 1.2);
                var center = new PointF(img.Width / 2, h);
                var textGraphicOptions = new TextGraphicsOptions(true)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                img.Mutate(i => i.DrawText(textGraphicOptions, watermark, scaledFont, new Rgba32(255, 255, 255, 0.3F), center));
                return true;
            }
            catch (Exception e)
            {
                using (var logService = new LogServices())
                {
                    Log log = new Log()
                    {
                        Function = "WaterMark.WaterMarkNew",
                        Message = userID + "id li kullanici WaterMarkNew metotunda hata aldi.",
                        Detail = e.ToString(),
                        CreatedDate = DateTime.Now
                    };
                    logService.Insert(log);
                }

                return false;
            }
        }
    }
}