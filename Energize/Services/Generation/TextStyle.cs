﻿using Discord;
using Discord.WebSocket;
using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using Energize.Interfaces.Services.Generation;
using Energize.Interfaces.Services.Senders;
using Energize.Services.Senders;
using Energize.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Energize.Services.Generation
{
    [Service("TextStyle")]
    public class TextStyle : ITextStyleService
    {
        private delegate string StyleCallback(string input);
        private static Dictionary<string, StyleCallback> StyleCallbacks = new Dictionary<string, StyleCallback>
        {
            ["owo"] = input => {
                string result = input.Replace("r", "w")
                    .Replace("R", "W")
                    .Replace("l", "w")
                    .Replace("L", "W");
                Random rand = new Random();
                if (rand.Next(0, 100) > 50)
                    result += "~";

                if (rand.Next(0, 100) < 25)
                {
                    if (rand.Next(0, 100) > 50)
                        result += " owo";
                    else
                        result += " uwu";
                }

                return result;
            },
            ["crazy"] = input => {
                string content = input;
                string result = "";
                Random rand = new Random();
                foreach (char letter in content)
                {
                    string part = letter.ToString();
                    if (rand.Next(1, 100) >= 50)
                        part = part.ToUpper();
                    else
                        part = part.ToLower();

                    result += part;
                }

                return result;
            },
            ["reverse"] = input => {
                char[] chars = input.ToCharArray();
                Array.Reverse(chars);

                return new string(chars);
            },
            ["anime"] = input => {
                input = input.First().ToString().ToUpper() + input.Substring(1);
                string result = "";
                foreach (char c in input)
                {
                    if (char.IsLetter(c))
                    {
                        if (char.IsUpper(c))
                        {
                            byte converted = (byte)(161 + Convert.ToByte(c) - 65);
                            string utf8 = Encoding.UTF8.GetString(new byte[] { 239, 188, converted });

                            result += utf8;
                        }
                        else
                        {
                            byte converted = (byte)(130 + Convert.ToByte(c) - 98);
                            string utf8 = Encoding.UTF8.GetString(new byte[] { 239, 189, converted });

                            result += utf8;
                        }
                    }
                    else
                    {
                        result += c;
                    }
                }
                result = result.Replace(".", "．")
                    .Replace("!", "！")
                    .Replace(" ", "\t");

                Random rand = new Random();
                if (rand.Next(0, 100) > 90)
                    result += "～";

                if (rand.Next(0, 100) > 75)
                    result += "．．";

                if (rand.Next(0, 100) > 75)
                    result += "！！";

                string[] decoration = StaticData.Instance.AnimeDecorations[rand.Next(0, StaticData.Instance.AnimeDecorations.Length - 1)];
                result = decoration[0] + result + decoration[1];

                string emote = StaticData.Instance.AnimeEmotes[rand.Next(0, StaticData.Instance.AnimeEmotes.Length - 1)];
                if (rand.Next(0, 100) > 50)
                    result = emote + " － " + result;
                else
                    result = result + " － " + emote;

                return result;
            },
            ["kid"] = input => {
                Random rand = new Random();
                string result = input.ToLower()
                    .Replace("i will", "ima")
                    .Replace("i dont know", "idk")
                    .Replace("dont know", "dunno")
                    .Replace("because", "cuz")
                    .Replace("seriously", "srs")
                    .Replace("you are", "you is")
                    .Replace("they are", "they is")
                    .Replace("what", "wat")
                    .Replace("you're", "ur")
                    .Replace("you", "u")
                    .Replace("people", "ppl")
                    .Replace("that", "taht")
                    .Replace("this", "dis")
                    .Replace("please", "pls")
                    .Replace("arent", "aint")
                    .Replace("than", "den")
                    .Replace("fucking", "fuken")
                    .Replace("kid", "kiddi")
                    .Replace("see", "c")
                    .Replace("yes", "ye")
                    .Replace("the", "da")
                    .Replace("why", "y")

                    .Replace("'", "")
                    .Replace(".", "!!1 ")
                    .Replace(",", " ")
                    .Replace("to", "2")
                    .Replace("for", "4")
                    .Replace("oh", "o")
                    .Replace("be", "B")
                    .Replace("ll", "l")
                    .Replace("nn", "n")
                    .Replace("pp", "p")
                    .Replace("ck", "k");

                if (rand.Next(0, 100) > 50)
                {
                    int rnum = rand.Next(0, 100);
                    if (rnum > 66)
                    {
                        int amount = rand.Next(1, 3);
                        result += " :" + new string('D', amount);
                    }
                    else if (rnum < 33)
                    {
                        int amount = rand.Next(1, 3);
                        result += " X" + new string('D', amount);
                    }
                    else
                    {
                        result += " =)";
                    }
                }

                return result;
            },
            ["leet"] = input => {
                string result = input.ToLower()
                    .Replace("e", "3")
                    .Replace("a", "4")
                    .Replace("i", "1")
                    .Replace("o", "0");

                return result;
            },
            ["zalgo"] = input =>
            {
                string ret = string.Empty;
                Random rand = new Random();
                foreach(char c in input)
                {
                    string toput = string.Empty;
                    int count = rand.Next(5, 20);
                    for(uint i =0; i < count; i++)
                        toput += StaticData.Instance.Zalgo[rand.Next(0, StaticData.Instance.Zalgo.Length - 1)];

                    ret += toput + c;
                }
                return ret;
            }
        };

        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly ServiceManager _ServiceManager;

        public TextStyle(EnergizeClient client)
        {
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._ServiceManager = client.ServiceManager;
        }

        public string GetStyleResult(string input, string style)
        {
            if (StyleCallbacks.ContainsKey(style))
                return StyleCallbacks[style](input);
            else
                return input;
        }

        public List<string> GetStyles()
        {
            List<string> styles = new List<string>();
            foreach (KeyValuePair<string, StyleCallback> callback in StyleCallbacks)
                styles.Add(callback.Key);

            return styles;
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (msg.Channel is IDMChannel || msg.Author.IsBot) return;

            SocketGuildUser user = msg.Author as SocketGuildUser;
            IDatabaseService db = this._ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext dbctx = await db.GetContext())
            {
                IDiscordUser dbuser = await dbctx.Instance.GetOrCreateUser(user.Id);
                if (dbuser.Style == "none") return;
                string style = dbuser.Style;

                if (!StyleCallbacks.ContainsKey(style)) return;

                bool shouldpost = true;
                try
                {
                    await msg.DeleteAsync();
                }
                catch
                {
                    shouldpost = false;
                }

                if (!shouldpost) return;

                string result = GetStyleResult(msg.Content, style);
                string avatar = msg.Author.GetAvatarUrl(ImageFormat.Auto);
                string name = msg.Author.Username;
                ulong success = 0;
                IWebhookSenderService sender = this._ServiceManager.GetService<IWebhookSenderService>("Webhook");

                if (result.Length > 2000)
                    success = await sender.SendRaw(msg, "Message was over discord limit!", name, avatar);
                else
                    success = await sender.SendRaw(msg, result, name, avatar);

                if (success == 0)
                {
                    string display = $"{msg.Author}: {result}\n`(This feature needs the \"Manage webhooks\" right to work properly)`";
                    if (display.Length > 2000)
                        await this._MessageSender.Warning(msg, "Style", "Message was over discord limit!");
                    else
                        await this._MessageSender.SendRaw(msg, display);
                }
            }
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}