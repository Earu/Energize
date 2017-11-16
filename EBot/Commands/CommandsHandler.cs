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
        private Dictionary<string,  Command> _Cmds;
        private Dictionary<string, string> _LastChannelPictureURL;

        public CommandsHandler()
        {
            this._EmbedReply = new CommandReplyEmbed
            {
                Handler = this
            };
            this._Cmds = new Dictionary<string, Command>();
            this._LastChannelPictureURL = new Dictionary<string, string>();
        }

        public BotLog Log { get => this._Log; set => this._Log = value; }
        public CommandSource Source { get => this._Source; set => this._Source = value; }
        public CommandReplyEmbed EmbedReply { get => this._EmbedReply; set => this._EmbedReply = value; }
        public string Prefix { get => this._Prefix; set => this._Prefix = value; }
        public DiscordClient Client { get => this._Client; set => this._Client = value; }
        public Dictionary<string,Command> Commands { get => this._Cmds; }

        public string GetLastPictureURL(DiscordChannel chan)
        {
            string index = chan.Guild.Id.ToString() + chan.Id.ToString();
            return this._LastChannelPictureURL.TryGetValue(index, out string url) ? url : "";
        }

        private bool IsCmdLoaded(string cmd)
        {
            return this._Cmds.ContainsKey(cmd) ? this._Cmds[cmd].Loaded : false;
        }

        public void LoadCommand(string name,CommandCallback callback=null,string help=null, string usage=null,string modulename=null)
        {
            if (this._Cmds.ContainsKey(name))
            {
                this._Cmds.Remove(name);
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

                Command cmd = new Command(name, callback, help, usage, modulename);

                this._Cmds.Add(name,cmd);
            }
        }

        public void LoadCommand(string[] names,CommandCallback callback=null,string help=null,string usage=null,string modulename=null)
        {
            string cmd = names[0];
            this.LoadCommand(cmd, callback, help, usage, modulename);

            for (uint i = 1; i < names.Length; i++)
            {
                Command.AddAlias(cmd,names[i]);
            }
        }

        public void UnloadCommand(string name)
        {
            if (this._Cmds.ContainsKey(name))
            {
                this._Cmds[name].Loaded = false;
            }
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
            Command.Aliases.TryGetValue(alias, out string result);
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
            bool success = this._Cmds.TryGetValue(cmd, out Command retrieved);

            if (success)
            {
                try
                {
                    await retrieved.Run(this._EmbedReply, msg, args);
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
