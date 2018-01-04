using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using System;

namespace EBot.MachineLearning
{
    public class MarkovHandler
    {
        private static MarkovChain _Chain = new MarkovChain();
        private static Dictionary<ulong,bool> _BlackList = new Dictionary<ulong,bool>
        {
            [81384788765712384] = false,
            [110373943822540800] = false,
            [264445053596991498] = false,
        };

        public static void Learn(string content,ulong id)
        {
            if(!_BlackList.ContainsKey(id))
            {
                _Chain.Learn(content);
            }
        }

        public static string Generate(string data)
        {
            string result = "";

            if (data == "")
            {
                List<string> words = _Chain.Generate(120);
                foreach (string word in words)
                {
                    result += " " + word;
                }

                return result;
            }
            else
            {
                string[] parts = data.Split(' ');
                string firstword = parts[parts.Length - 1];
                string firstpart = string.Join(' ', parts, 0, parts.Length - 1);
                List<string> words = _Chain.Generate(firstword,120);

                foreach (string word in words)
                {
                    result += " " + word;
                }

                return firstpart + " " + result.TrimStart();
            }
        }
    }
}
