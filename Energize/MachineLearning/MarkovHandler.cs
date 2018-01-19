using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using System;
using Energize.Logs;

namespace Energize.MachineLearning
{
    public class MarkovHandler
    {
        private static char[] _Separators = { ' ', '.', ',', '!', '?', ';', '_' };
        private static int _MaxDepth = 2;
        private static Dictionary<ulong,bool> _BlackList = new Dictionary<ulong,bool>
        {
            [81384788765712384]  = false,
            [110373943822540800] = false,
            [264445053596991498] = false,
        };

        public static void Learn(string content,ulong id,BotLog log)
        {
            MarkovChain chain = new MarkovChain();
            if(!_BlackList.ContainsKey(id))
            {
                try
                {   
                    chain.Learn(content);
                }
                catch(Exception e)
                {
                    log.Nice("Markov",ConsoleColor.Red,"Failed to learn from a message\n" + e.ToString());
                }
            }
        }

        public static string Generate(string data)
        {
            MarkovChain chain = new MarkovChain();

            if (data == "")
            {
                return chain.Generate(40);
            }
            else
            {
                data = data.ToLower();
                string firstpart = "";
                string[] parts = data.Split(_Separators);
                if(parts.Length > _MaxDepth)
                {
                    firstpart = string.Join(' ',parts,parts.Length - _MaxDepth,_MaxDepth);
                    return data + " " + chain.Generate(firstpart,40).TrimStart();
                }
                else
                {
                    firstpart = string.Join(' ',parts);
                    return firstpart + " " + chain.Generate(firstpart,40).TrimStart();
                }
            }
        }
    }
}
