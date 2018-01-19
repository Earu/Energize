using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace Energize.Drama
{
    class DramaAnalyzer
    {
        private static string[] DramaWords = EnergizeData.DRAMA_FILTER;
        private static string[] ApologizeWords = EnergizeData.APOLOGIZE_FILTER;
        private static List<SocketUser> WatchList = new List<SocketUser>();
        private static Dictionary<SocketChannel, int> ChanDrama = new Dictionary<SocketChannel, int>();
        private static Dictionary<SocketUser, int> UserDramaPotential = new Dictionary<SocketUser, int>();
        private static Dictionary<SocketUser, int> UserDramaFactor = new Dictionary<SocketUser, int>();
        private static Dictionary<SocketChannel, List<DramaCachedMessage>> ChanCachedMessages = new Dictionary<SocketChannel, List<DramaCachedMessage>>();
        private static int MaxDrama = 300;
        private static int MaxCacheSize = 10;

        public void AddToWatchList(SocketUser user)
        {
            if (!WatchList.Contains(user))
            {
                WatchList.Add(user);
            }
        }

        public void RemoveFromWatchlist(SocketUser user)
        {
            if (WatchList.Contains(user))
            {
                WatchList.Remove(user);
            }
        }

        public bool IsUserWatchlisted(SocketUser user)
        {
            return WatchList.Contains(user);
        }

        public void AddDrama(SocketChannel chan,int amount)
        {
            if (!ChanDrama.ContainsKey(chan))
            {
                ChanDrama[chan] = 0;
            }

            int result = ChanDrama[chan] + amount;
            if(result > MaxDrama)
            {
                ChanDrama[chan] = MaxDrama;
            }else if(result < 0)
            {
                ChanDrama[chan] = 0;
            }
            else
            {
                ChanDrama[chan] = result;
            }
        }

        public void AddDramaPotential(SocketUser user,int amount)
        {
            if (WatchList.Contains(user)) return;
            if (!UserDramaPotential.ContainsKey(user))
            {
                UserDramaPotential[user] = 0;
            }

            int result = UserDramaPotential[user] + amount;
            if(result > 100)
            {
                UserDramaPotential[user] = 100;
            }
            else if(result < 0)
            {
                UserDramaPotential[user] = 0;
            }
            else
            {
                UserDramaPotential[user] = result;
            }
        }

        public void AddDramaFactor(SocketUser user, int amount)
        {
            if (!WatchList.Contains(user)) return;
            if (!UserDramaFactor.ContainsKey(user))
            {
                UserDramaFactor[user] = 0;
            }

            int result = UserDramaFactor[user] + amount;
            if (result >= 5)
            {
                UserDramaFactor[user] = 5;
            }
            else if (result <= 1)
            {
                UserDramaFactor[user] = 1;
            }
            else
            {
                UserDramaFactor[user] = result;
            }
        }

        public bool IsSensibleWord(string input)
        {
            input = input.ToLower();
            foreach(string word in DramaWords)
            {
                if(input == word)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsApologizeWord(string input)
        {
            input = input.ToLower();
            foreach (string word in ApologizeWords)
            {
                if (input == word)
                {
                    return true;
                }
            }

            return false;
        }

        public void CacheMessage(SocketMessage msg,DramaInfo info)
        {
            SocketChannel chan = msg.Channel as SocketChannel;
            if (!ChanCachedMessages.ContainsKey(chan))
            {
                ChanCachedMessages[chan] = new List<DramaCachedMessage>();
            }

            if(ChanCachedMessages[chan].Count > MaxCacheSize)
            {
                ChanCachedMessages[chan].RemoveAt(0);
            }
            DramaCachedMessage cached = new DramaCachedMessage
            {
                Message = msg,
                Info = info
            };

            ChanCachedMessages[chan].Add(cached);
        }

        public bool WasQuotedRecently(SocketChannel chan,string input)
        {
            input = input.ToLower();
            foreach(DramaCachedMessage msg in ChanCachedMessages[chan])
            {
                if (msg.Info.ApologizeWords.Contains(input))
                {
                    return true;
                }

                if (msg.Info.SensibleWords.Contains(input))
                {
                    return true;
                }

                if (msg.Info.NormalWords.Contains(input))
                {
                    return true;
                }
            }

            return false;
        }

        public DramaInfo GetMessageDrama(SocketMessage msg)
        {
            DramaInfo info = new DramaInfo
            {
                Drama = 0,
                Watcheds = new List<SocketUser>(),
                Users = new List<SocketUser>(),
                SensibleWords = new List<string>(),
                ApologizeWords = new List<string>(),
                NormalWords = new List<string>(),
                IsCaps = false,
                Factor = 0
            };

            info.IsCaps = msg.Content.ToUpper() == msg.Content;
            string[] parts = msg.Content.ToLower().Split(" ");

            if (msg.Channel is IDMChannel)
            {
                info.Factor = 1;
            }
            else
            {
                SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
                if(chan.Guild.MemberCount >= 30)
                {
                    info.Factor = 0.5;
                }
                else if(chan.Guild.MemberCount > 10 && chan.Guild.MemberCount < 30)
                {
                    info.Factor = 1;
                }
                else
                {
                    info.Factor = 2;
                }
            }

            return info;
        }
    }
}
