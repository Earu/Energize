using System;
using System.IO;

namespace Energize.Essentials
{
    public class Logger
    {
        public static readonly object FileLocker = new object();

        private static readonly string Prefix = "> ";
        private static readonly string Path = "logs";

        public Logger()
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }

        private static string FormattedTime()
        {
            int hour = DateTime.Now.TimeOfDay.Hours;
            int minute = DateTime.Now.TimeOfDay.Minutes;
            string nicehour = hour < 10 ? "0" + hour : hour.ToString();
            string nicemin = minute < 10 ? "0" + minute : minute.ToString();
            return $"{nicehour}:{nicemin} - ";
        }

        private static void AppendPrefix()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(Prefix);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(FormattedTime());
        }

        public void LogTo(string filename, string msg)
        {
            lock (FileLocker)
            {
                File.AppendAllText($"{Path}/{filename}", $"{DateTime.Now} - {msg}\n");
            }
        }

        public void Normal(string msg)
        {
            lock (this)
            {
                AppendPrefix();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(msg);
                this.LogTo("energize.log", $"[NORMAL] >> {msg}");
            }
        }

        public void Nice(string head, ConsoleColor col, string content)
        {
            lock (this)
            {
                AppendPrefix();
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("[");
                Console.ForegroundColor = col;
                Console.Write(head);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("] >> ");
                Console.WriteLine(content);
                this.LogTo("energize.log", $"[{head.ToUpper()}] >> {content}");
            }
        }

        public void Warning(string msg)
        {
            lock (this)
            {
                AppendPrefix();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(msg);
                this.LogTo("energize.log", $"[WARN] >> {msg}");
            }
        }

        public void Danger(string msg)
        {
            lock (this)
            {
                AppendPrefix();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                this.LogTo("energize.log", $"[DANGER] >> {msg}");
            }
        }

        public void Danger(Exception ex)
        {
            lock (this)
            {
                AppendPrefix();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                this.LogTo("energize.log", $"[DANGER] >> {ex}");
            }
        }

        public void Error(string msg)
        {
            AppendPrefix();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("/!\\ ERROR /!\\");
            Console.WriteLine(msg);
            Console.ReadLine();
            this.LogTo("energize.log", $"[ERROR] >> {msg}");
        }

        public void Good(string msg)
        {
            lock (this)
            {
                AppendPrefix();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg);
                this.LogTo("energize.log", $"[GOOD] >> {msg}");
            }
        }

        public void Notify(string msg)
            => Console.WriteLine($"\n\t---------\\\\\\\\ {msg} ////---------\n");
    }
}
