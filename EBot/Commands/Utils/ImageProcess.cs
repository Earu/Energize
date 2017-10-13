using System;
using SixLabors.ImageSharp;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using EBot.Logs;
using System.Collections.Generic;
using EBot.Commands.Utils;

namespace EBot.Commands.Utils
{
    class ImageProcess
    {
        private static ulong ID = 0;

        private static async Task<string> GetImageExtension(string uri)
        {
            var r = (HttpWebRequest)WebRequest.Create(uri);
            r.Method = "HEAD";
            using (var res = await r.GetResponseAsync())
            {
                string ext = res.ContentType.Substring(6).Trim();
                return "." + ext;
            }
        }

        public static async Task<string> DownloadImage(string uri)
        {

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
            using (HttpWebResponse resp = (HttpWebResponse)await req.GetResponseAsync())
            {

                if ((resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Moved || resp.StatusCode == HttpStatusCode.Redirect) 
                    && resp.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    string ext = await GetImageExtension(uri);
                    string file = ID + ext;
                    ID++;

                    using (Stream inp = resp.GetResponseStream())
                    using (Stream outp = File.OpenWrite(file))
                    {
                        byte[] buffer = new byte[4096];
                        int br;
                        do
                        {
                            br = await inp.ReadAsync(buffer, 0, buffer.Length);
                            await outp.WriteAsync(buffer, 0, br);
                        } while (br != 0);
                    }

                    return file;
                }

                return null;
            }
        }

        public static void DeleteImage(string path)
        {
            File.Delete(path);
        }

        public static void Resize(string path,int width=500,int height=500)
        {
            using (Image<Rgba32> image = Image.Load(path))
            {
                image.Mutate(x => x.Resize(width,height));
                image.Save(path);
            }
        }

        public static void MakeBlackWhite(string path)
        {
            using(Image<Rgba32> image = Image.Load(path))
            {
                image.Mutate(x => x.BlackWhite());
                image.Save(path);
            }
        }

        public static Dictionary<string,ImagePoint> GetBounds(string path)
        {
            Dictionary<string, ImagePoint> results = new Dictionary<string,ImagePoint>();
            List<ImagePoint> all = new List<ImagePoint>();

            using (Image<Rgba32> img = Image.Load(path))
            {
                for(int  i = 0; i < img.Height; i++)
                {
                    for(int j = 0; j < img.Width; j++)
                    {
                        Rgba32 px = img[j, i];
                        if(px.A == 0)
                        {
                            all.Add(new ImagePoint(j,i));
                        }
                    }
                }

                if (all.Count > 0)
                {
                    results["Mins"] = all[0];
                    results["Maxs"] = all[all.Count - 1];
                }
                else
                {
                    results["Mins"] = new ImagePoint(0, 0);
                    results["Maxs"] = new ImagePoint(0, 0);
                }
            }

            return results;
        }

        public static async Task<string> Create(int width,int height)
        {
            string file = ID + ".png";
            ID++;

            using(Stream inp = File.OpenRead("Masks/blank.png"))
            using (Stream outp = File.OpenWrite(file))
            {
                byte[] buffer = new byte[4096];
                int br;
                do
                {
                    br = await inp.ReadAsync(buffer, 0, buffer.Length);
                    await outp.WriteAsync(buffer, 0, br);
                } while (br != 0);
            }
            
            using (Image<Rgba32> img = Image.Load(file))
            {
                img.Mutate(x => x.Resize(width, height));
            }

            return file;
        }

    }
}
