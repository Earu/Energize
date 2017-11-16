using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static EBot.Commands.CommandsHandler;

namespace EBot.Commands
{
    public class Command
    {
        private static Dictionary<string,List<Command>> _Modules = new Dictionary<string, List<Command>>();
        private static Dictionary<string, string> _Aliases = new Dictionary<string, string>();

        private string _Name;
        private CommandCallback _Callback;
        private string _Help;
        private string _Usage;
        private bool _Loaded;
        private string _ModuleName;

        public Command(string cmd,CommandCallback callback,string help,string usage,string modulename)
        {
            help = help ?? "No help was provided";
            usage = usage ?? "No usage was provided";
            modulename = modulename ?? "None";

            this._Name = cmd;
            this._Callback = callback;
            this._Help = help;
            this._Usage = usage;
            this._Loaded = true;
            this._ModuleName = modulename;

            if (!_Modules.ContainsKey(modulename))
            {
                _Modules[modulename] = new List<Command>();
            }

            _Modules[modulename].Add(this);
            _Aliases[cmd] = cmd;
        }

        public static Dictionary<string,List<Command>> Modules { get => _Modules; }
        public static Dictionary<string, string> Aliases { get => _Aliases; set => _Aliases = value; }

        public bool Loaded { get => this._Loaded; set => this._Loaded = value; }
        public string Cmd { get => this._Name; set => this._Name = value; }

        public async Task Run(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            await this._Callback(embedrep, msg, args);
        }

        public string GetHelp()
        {
            string help =  "*Usage*:\n";
            help += "```\n" + this._Usage + "```\n";
            help += "*Help*:\n";
            help += "```\n" + this._Help + "```\n";

            return help;
        }

        public static void AddAlias(string origin,string alias)
        {
            _Aliases[alias] = origin;
        }

    }
}
