using DSharpPlus.Entities;
using EBot.Logs;
using EBot.Utils;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace EBot.MachineLearning
{
    class ChatBot
    {
        private static string _URL = "http://www.a-i.com/alan1/webface1_ctrl.asp?";
        private static Dictionary<DiscordChannel, CookieContainer> _Cookies = new Dictionary<DiscordChannel, CookieContainer>();
        private static string[] _SentencesEnd = {
            "owo",
            "-w-",
            "uwu",
            "x3",
            "x'3",
            "x\"3",
            ";3",
            ":3",
            ":\\3",
            "~",
            ">//<",
            ">.>",
            "<.<",
            ">.<",
            "<3",
            "^,=,^",
            "o,=,o",
            "^w^",
            "o .. o",
            "*rawr*",
            "***ROARAWRAR***",
        };

        public static async Task<string> Ask(DiscordChannel chan,string sentence,BotLog log)
        {
            if (!_Cookies.TryGetValue(chan, out CookieContainer cookie))
            {
                cookie = new CookieContainer();
                _Cookies.Add(chan, cookie);
            }

            NameValueCollection values = HttpUtility.ParseQueryString(string.Empty);
            values["name"] = "EBot";
            values["question"] = sentence;

            string parameters = values.ToString();
            string html = await HTTP.Fetch(_URL + parameters, log,null,request => {
                request.CookieContainer = cookie;
            });

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode SELECT = doc.DocumentNode.Element("html").Element("body").Element("div").Element("select");

            foreach (HtmlNode node in SELECT.ChildNodes)
            {
                string start = "answer = ";
                if (node.InnerText.StartsWith(start))
                {
                    string answer = node.InnerText.Remove(0,start.Length);
                    Random rand = new Random();
                    if (rand.Next(0, 100) <= 25) // 1/4
                    {
                        string end = " " + _SentencesEnd[rand.Next(0, _SentencesEnd.Length - 1)];
                        answer = answer.Substring(0, answer.Length - 3) + end;
                    }

                    return answer;
                }
            }

            return null;

        }

    }
}
