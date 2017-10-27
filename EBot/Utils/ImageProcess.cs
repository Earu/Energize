using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using ImageMagick;

namespace EBot.Utils
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
            using (MagickImage image = new MagickImage(path))
            {
                image.AdaptiveResize(width, height);
                image.Write(path);
            }
        }

        public static void MakeBlackWhite(string path)
        {
            using (MagickImage image = new MagickImage(path))
            {
                image.Grayscale(PixelIntensityMethod.Average);
                image.Write(path);
            }
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
