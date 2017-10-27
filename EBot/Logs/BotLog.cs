using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EBot.Logs
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
        }

        public void Warning(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
        }

        public void Danger(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
        }

        public void Error(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("/!\\ ERROR /!\\");
            Console.WriteLine(msg);
            Console.ReadLine();
        }

        public void Good(string msg)
        {
            this.Prefix();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
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
