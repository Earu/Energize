using DSharpPlus;
using DSharpPlus.Entities;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands
{
    class CommandsHandler
    {
        public delegate void CommandCallback(CommandReplyEmbed embedrep, DiscordMessage msg, List<String> args);

        private DiscordClient _Client;
        private BotLog _Log;
        private string _Prefix;
        private CommandSource _Source;
        private CommandReplyEmbed _EmbedReply = new CommandReplyEmbed();
        private Dictionary<string,CommandCallback> _Cmds = new Dictionary<string,CommandCallback>();
        private Dictionary<string,string> _CmdsHelp = new Dictionary<string,string>();
        private Dictionary<string, bool> _CmdsLoaded = new Dictionary<string, bool>();
        private Dictionary<string, string> _CmdAliases = new Dictionary<string, string>();
        private Dictionary<DiscordChannel, string> _LastChannelPictureURL = new Dictionary<DiscordChannel, string>();
        private List<string> _PictureURLCacheAll = new List<string>();
        private List<string> _PictureURLCacheSFW = new List<string>();
        private List<string> _PictureURLCacheNSFW = new List<string>();

        public BotLog Log { get => this._Log; set => this._Log = value; }
        public CommandSource Source { get => this._Source; set => this._Source = value; }
        public CommandReplyEmbed EmbedReply { get => this._EmbedReply; set => this._EmbedReply = value; }
        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public DiscordClient Client { get => this._Client; set => this._Client = value; }
        public Dictionary<string,CommandCallback> Commands { get => this._Cmds; }
        public Dictionary<string, string> CommandsHelp { get => this._CmdsHelp; }

        public string GetLastPictureURL(DiscordChannel chan)
        {
            string url;
            if(this._LastChannelPictureURL.TryGetValue(chan,out url))
            {
                return url;
            }
            else
            {
                return "";
            }
        }

        public List<string> GetURLCache(string what)
        {
            switch (what)
            {
                case "nsfw":
                    {
                        return this._PictureURLCacheNSFW;
                    }
                case "sfw":
                    {
                        return this._PictureURLCacheSFW;
                    }
                case "all":
                    {
                        return this._PictureURLCacheAll;
                    }
                default:
                    {
                        return this._PictureURLCacheSFW;
                    }
            }
        }

        private bool IsCmdLoaded(string cmd)
        {
            bool result = true;
            if (this._Cmds.ContainsKey(cmd) && this._CmdsLoaded.TryGetValue(cmd,out result))
            {
                return result;
            }
            else
            {
                return false;
            }
        }

        private static void DefaultCallback(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args){}

        public void LoadCommand(string name,CommandCallback callback=null,string desc="No description provided")
        {
            try
            {
                if (this._Cmds.ContainsKey(name))
                {
                    this._Cmds.Remove(name);
                    this._CmdsHelp.Remove(name);
                    this._CmdsLoaded.Remove(name);
                }
                else
                {
                    if (callback == null)
                    {
                        callback = DefaultCallback;
                    }
                    this._Cmds.Add(name, callback);
                    this._CmdsHelp.Add(name, desc);
                    this._CmdsLoaded.Add(name, true);
                    this._CmdAliases.Add(name, name);
                }
            }catch(Exception e)
            {
                this._Log.Nice("Commands", ConsoleColor.Red, "Failed to register " + name);
                this._Log.Danger(e.ToString());
            }
        }

        public void LoadCommand(string[] names,CommandCallback callback = null, string desc = "No description provided")
        {
            for(int i = 0; i < names.Length; i++)
            {
                if (i == 0)
                {
                    this.LoadCommand(names[i], callback, desc);
                }
                else
                {
                    if (this._CmdAliases.ContainsKey(names[i]))
                    {
                        this._CmdAliases.Remove(names[i]);
                    }
                    this._CmdAliases.Add(names[i], names[0]);
                }
            }
        }

        public void UnloadCommand(string name)
        {
            if (this._CmdsLoaded.ContainsKey(name))
            {
                this._CmdsLoaded.Remove(name);
            }
            this._CmdsLoaded.Add(name, false);
            this._Log.Nice("Commands", ConsoleColor.Red, "Unloaded " + name);
        }

        private string GetCmd(string line)
        {
            return line.Substring(this._Prefix.Length).Split(' ')[0];
        }

        private List<string> GetCmdArgs(string line)
        {
            string str = line.Remove(0, this.GetCmd(line).Length + this._Prefix.Length);
            return new List<string>(str.Split(','));
        }

        private string GetAliasOriginCmd(string alias)
        {
            string result = "";
            this._CmdAliases.TryGetValue(alias, out result);
            return result;
        }

        private void LogCommand(DiscordMessage msg,string cmd,List<string> args)
        {

            string log = "(" + msg.Channel.Guild.Name + " - #" + msg.Channel.Name + ") " + msg.Author.Username + " used <" + cmd + ">";
            if (!string.IsNullOrWhiteSpace(args[0]))
            {
                log += "  => [\t";
                foreach (string arg in args)
                {
                    log += arg + "\t";
                }
                log += "]";
            }
            else
            {
                log += " with no args";
            }
            this._Log.Nice("Commands", ConsoleColor.Cyan, log);
        }

        private void MainCall(DiscordMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                string cmd = this.GetAliasOriginCmd(this.GetCmd(content));
                if (content.StartsWith(this._Prefix))
                {
                    if (this.IsCmdLoaded(cmd))
                    {
                        List<string> args = this.GetCmdArgs(content);
                        CommandCallback callback;
                        bool fetched = this._Cmds.TryGetValue(cmd, out callback);

                        if (fetched)
                        {
                            callback(this._EmbedReply,msg, args);
                            this.LogCommand(msg, cmd, args);
                        }
                        else
                        {
                            this._EmbedReply.Danger(msg, "Uh oh", "Something went very wrong, please contact Earu#9037");
                            this._Log.Nice("Commands", ConsoleColor.Red, "Couldn't retrieve callback for <" + cmd + ">");
                        }
                    }
                    else
                    {
                        this._Log.Nice("Commands", ConsoleColor.Red, msg.Author.Username + " tried to use an invalid command <" + cmd + ">");
                    }
                }
            }
        }

        private void CacheURLs(DiscordMessage msg,List<string> urls)
        {
            if (urls.Count > 0)
            {
                string lasturl = urls[urls.Count - 1];

                if (this._LastChannelPictureURL.ContainsKey(msg.Channel))
                {
                    this._LastChannelPictureURL.Remove(msg.Channel);
                }
                this._LastChannelPictureURL.Add(msg.Channel, lasturl);

                if (msg.Channel.IsNSFW)
                {
                    foreach (string url in urls)
                    {
                        if (!this._PictureURLCacheNSFW.Contains(url))
                        {
                            this._PictureURLCacheNSFW.Add(url);
                        }
                    }
                }
                else
                {
                    foreach (string url in urls)
                    {
                        if (!this._PictureURLCacheSFW.Contains(url))
                        {
                            this._PictureURLCacheSFW.Add(url);
                        }
                    }
                }

                foreach (string url in urls)
                {
                    if (!this._PictureURLCacheAll.Contains(url))
                    {
                        this._PictureURLCacheAll.Add(url);
                    }
                }

                this._Log.Nice("PictureCache", ConsoleColor.Gray, "Cache updated");
            }
        }

        private void GetImageURLS(DiscordMessage msg)
        {
            List<string> urls = new List<string>();

            IReadOnlyList<DiscordAttachment> attachs = msg.Attachments;
            foreach (DiscordAttachment attach in attachs)
            {
                if(attach.FileName.EndsWith(".jpg") || attach.FileName.EndsWith(".jpeg") || attach.FileName.EndsWith(".png"))
                {
                    urls.Add(attach.Url);
                }
            }

            IReadOnlyList<DiscordEmbed> embeds = msg.Embeds;
            foreach(DiscordEmbed embed in embeds)
            {
                if (embed.Type == "image")
                {
                    string url = embed.Thumbnail.Url.ToString();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        urls.Add(url);
                    }

                }else if(embed.Type == "rich")
                {
                    string url = embed.Image.Url.ToString();
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        urls.Add(url);
                    }
                }
            }

            this.CacheURLs(msg,urls);

        }

        public void Initialize()
        {
            this._Client.MessageCreated += async e =>
            {
                this.GetImageURLS(e.Message);
            };

            this._Source.LoadCommands(this,this.Log);
            this._Client.MessageCreated += async e =>
            {
                this.MainCall(e.Message);
            };

        }

    }
}
