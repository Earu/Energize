using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Energize.Logs
{
    public class BotLog
    {
        private string _Prefix = "> ";

        private void Prefix()
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(this._Prefix);
            Console.ForegroundColor = ConsoleColor.Gray;
            int hour = DateTime.Now.TimeOfDay.Hours;
            int minute = DateTime.Now.TimeOfDay.Minutes;
            string nicehour = hour < 10 ? "0" + hour : hour.ToString();
            string nicemin = minute < 10 ? "0" + minute : minute.ToString();
            Console.Write(nicehour + ":" + nicemin + " - ");
        }

        public void Normal(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(msg);

            File.AppendAllText("logs.txt","[NORMAL] >> " + msg + "\n\n");
        }

        public void Nice(string head,ConsoleColor col,string content)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");
            Console.ForegroundColor = col;
            Console.Write(head);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] >> ");
            Console.WriteLine(content);

            File.AppendAllText("logs.txt","[NICE-" + head.ToUpper() + "] >> " + content + "\n\n");
        }

        public void Warning(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);

            File.AppendAllText("logs.txt","[WARN >> " + msg + "\n\n");
        }

        public void Danger(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);

            File.AppendAllText("logs.txt","[DANGER] >> " + msg + "\n\n");
        }

        public void Error(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("/!\\ ERROR /!\\");
            Console.WriteLine(msg);
            Console.ReadLine();

            File.AppendAllText("logs.txt","[ERROR] >> " + msg + "\n\n");
        }

        public void Good(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);

            File.AppendAllText("logs.txt","[GOOD] >> " + msg + "\n\n");
        }

        public void Notify(string msg)
        {
            Console.WriteLine("\n\t---------\\\\\\\\ " + msg + " ////---------\n");
        }

        public static void Debug(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Debug");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" >> ");
            Console.WriteLine(msg);
        }

        public static void Debug(List<string> msgs)
        {
            foreach(string msg in msgs)
            {
                Debug(msg);
            }
        }
    }
}
