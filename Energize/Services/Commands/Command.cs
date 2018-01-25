using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using static Energize.Services.Commands.CommandHandler;

namespace Energize.Services.Commands
{
    public class Command
    {
        private static Dictionary<string,List<Command>> _Modules = new Dictionary<string, List<Command>>();
        private static Dictionary<string, bool> _ModulesLoaded = new Dictionary<string, bool>();

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

            this.AddToModule(this, modulename);
        }

        public static Dictionary<string,List<Command>> Modules { get => _Modules; }

        public bool Loaded { get => this._Loaded; set => this._Loaded = value; }
        public string Cmd { get => this._Name; set => this._Name = value; }
        public CommandCallback Callback { get => this._Callback; }

        public static bool IsLoadedModule(string module)
        {
            if (!_ModulesLoaded.ContainsKey(module))
            {
                _ModulesLoaded[module] = true;
            }

            return _ModulesLoaded[module];
        }

        public static void SetLoadedModule(string module,bool state)
        {
            _ModulesLoaded[module] = state;
        }

        public async Task Run(CommandContext ctx,IDisposable state)
        {
            await this._Callback(ctx);
            state.Dispose();
        }

        public string GetHelp()
        {
            string help =  "**USAGE:**\n";
            help += "``" + this._Usage + "``\n";
            help += "**HELP:**\n";
            help += "```" + this._Help + "```";

            return help;
        }

        private void AddToModule(Command cmd,string modulename)
        {
            foreach(Command c in _Modules[modulename])
            {
                if(c.Cmd == cmd.Cmd)
                {
                    _Modules[modulename].Remove(c);
                    break;
                }
            }

            _Modules[modulename].Add(cmd);
        }
    }
}
