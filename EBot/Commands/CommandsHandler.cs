using DSharpPlus;
using DSharpPlus.Entities;
using EBot.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EBot.Commands
{
    public class CommandsHandler
    {
        public delegate Task CommandCallback(CommandReplyEmbed embedrep, DiscordMessage msg, List<String> args);

        private DiscordClient _Client;
        private BotLog _Log;
        private string _Prefix;
        private CommandSource _Source;
        private CommandReplyEmbed _EmbedReply;
        private Dictionary<string, CommandCallback> _Cmds;
        private Dictionary<string, string> _CmdsHelp;
        private Dictionary<string, bool> _CmdsLoaded;
        private Dictionary<string, string> _CmdAliases;
        private Dictionary<string, List<string>> _ModuleCmds;
        private Dictionary<string, string> _LastChannelPictureURL;

        public CommandsHandler()
        {
            this._EmbedReply = new CommandReplyEmbed();
            this._EmbedReply.Handler = this;
            this._Cmds = new Dictionary<string, CommandCallback>();
            this._CmdsHelp = new Dictionary<string, string>();
            this._CmdsLoaded = new Dictionary<string, bool>();
            this._CmdAliases = new Dictionary<string, string>();
            this._ModuleCmds = new Dictionary<string, List<string>>();
            this._LastChannelPictureURL = new Dictionary<string, string>();
        }

        public BotLog Log { get => this._Log; set => this._Log = value; }
        public CommandSource Source { get => this._Source; set => this._Source = value; }
        public CommandReplyEmbed EmbedReply { get => this._EmbedReply; set => this._EmbedReply = value; }
        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public DiscordClient Client { get => this._Client; set => this._Client = value; }
        public Dictionary<string,CommandCallback> Commands { get => this._Cmds; }
        public Dictionary<string, string> CommandsHelp { get => this._CmdsHelp; }
        public Dictionary<string, List<string>> ModuleCmds { get => this._ModuleCmds; }

        public string GetLastPictureURL(DiscordChannel chan)
        {
            string index = chan.Guild.Id.ToString() + chan.Id.ToString();
            string url;
            if(this._LastChannelPictureURL.TryGetValue(index,out url))
            {
                return url;
            }
            else
            {
                return "";
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

        public void LoadCommand(string name,CommandCallback callback=null,string desc="No description provided",string modulename = "none")
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
                        callback = async (CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args) => 
                        {
                            await embedrep.Good(msg, null, "Hello world!");
                        };
                    }

                    this._Cmds.Add(name, callback);
                    this._CmdsHelp.Add(name, desc);
                    this._CmdsLoaded.Add(name, true);
                    this._CmdAliases.Add(name, name);

                    if (!this._ModuleCmds.ContainsKey(modulename)) {
                        this._ModuleCmds.Add(modulename, new List<string>());
                    }

                    List<string> modulecmds;
                    if(this._ModuleCmds.TryGetValue(modulename,out modulecmds))
                    {
                        modulecmds.Add(name);
                    }

                }
            }catch(Exception e)
            {
                this._Log.Nice("Commands", ConsoleColor.Red, "Failed to register " + name);
                this._Log.Danger(e.ToString());
            }
        }

        public void LoadCommand(string[] names,CommandCallback callback = null, string desc = "No description provided",string modulename = "none")
        {
            for(int i = 0; i < names.Length; i++)
            {
                if (i == 0)
                {
                    this.LoadCommand(names[i], callback, desc, modulename);
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
            this._CmdAliases.TryGetValue(alias, out string result);
            return result;
        }

        private void LogCommand(DiscordMessage msg, string cmd, List<string> args,bool isdm,bool isdeleted=false)
        {
            string log = "";
            ConsoleColor color = ConsoleColor.Blue;
            string head = "DMCommands";
            string action = "used";

            if (!isdm)
            {
                log += "(" + msg.Channel.Guild.Name + " - #" + msg.Channel.Name + ") ";
                color = ConsoleColor.Cyan;
                head = "Commands";
            }

            if (isdeleted)
            {
                color = ConsoleColor.Yellow;
                action = "deleted";
            }
            
            log += msg.Author.Username + " " + action + " <" + cmd + ">";
            if (!string.IsNullOrWhiteSpace(args[0]))
            {
                log += "  => [" + string.Join(',',args) + " ]";
            }
            else
            {
                log += " with no args";
            }

            this._Log.Nice(head,color, log);
        }

        private async Task CommandCall(DiscordMessage msg,string cmd)
        {
            List<string> args = this.GetCmdArgs(msg.Content);
            bool fetched = this._Cmds.TryGetValue(cmd, out CommandCallback callback);

            if (fetched)
            {
                try
                {
                    await callback(this._EmbedReply, msg, args);
                    this.LogCommand(msg, cmd, args, msg.Channel.IsPrivate);
                }
                catch (Exception e)
                {
                    this._Log.Nice("Commands", ConsoleColor.Red, "<" + cmd + "> generated an error, args were [" + string.Join(',', args) + " ]");
                    this._Log.Danger(e.ToString());

                    await this._EmbedReply.Danger(msg,msg.Author.Username, "Something went wrong, skipping!");
                }
            }
            else
            {
                this._Log.Nice("Commands", ConsoleColor.Red, "No callback for <" + cmd + ">");
            }
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
                        this.CommandCall(msg, cmd).RunSynchronously();
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
                string index = msg.Channel.Guild.ToString() + msg.Channel.Id.ToString();
                string lasturl = urls[urls.Count - 1];

                if (this._LastChannelPictureURL.ContainsKey(index))
                {
                    this._LastChannelPictureURL.Remove(index);
                }
                this._LastChannelPictureURL.Add(index, lasturl);
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

        private async Task GetDeletedCommandMessages(DiscordMessage msg)
        {
            string content = msg.Content;
            if (!msg.Author.IsBot)
            {
                string cmd = this.GetAliasOriginCmd(this.GetCmd(content));
                if (content.StartsWith(this._Prefix))
                {
                    if (this.IsCmdLoaded(cmd))
                    {
                        string user = msg.Author.Mention;
                        List<string> args = this.GetCmdArgs(content);

                        await this._EmbedReply.Send(msg.Channel,"Sneaky!",user + " deleted a command message \"" + cmd + "\" :eyes:",new DiscordColor(220, 180, 80));
                        this.LogCommand(msg, cmd, args, msg.Channel.IsPrivate, true);
                    }
                }
            }
        }

        public void Initialize()
        {
            this._Client.MessageCreated += async e =>
            {
                this.GetImageURLS(e.Message);
            };

            this._Client.MessageDeleted += async e =>
            {
                this.GetDeletedCommandMessages(e.Message).RunSynchronously();
            };

            this._Source.LoadCommands(this, this.Log);
            this._Client.MessageCreated += async e =>
            {
                this.MainCall(e.Message);
            };
        }
    }
}
