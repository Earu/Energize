using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using SixLabors.ImageSharp;

namespace Energize.Utils
{
    public class ImageProcess
    {
        private static ulong ID = 0;

        private static async Task<string> GetImageExtension(string uri)
        {
            HttpWebRequest r = WebRequest.Create(uri) as HttpWebRequest;
            r.Method = "HEAD";
            using (WebResponse res = await r.GetResponseAsync())
            {
                string ext = res.ContentType.Substring(6).Trim();
                return "." + ext;
            }
        }

        public static async Task<string> DownloadImage(string uri)
        {

            HttpWebRequest req = WebRequest.Create(uri) as HttpWebRequest;
            using (HttpWebResponse resp = (await req.GetResponseAsync()) as HttpWebResponse)
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
                image.Mutate(x => x.Resize(width, height));
                image.Save(path);
            }
        }

        public static Image<Rgba32> Get(string path)
        {
            return Image.Load(path);
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

            Resize(file, width, height);

            return file;
        }

    }
}
