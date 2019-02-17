using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Energize.Services.Commands.CommandHandler;

namespace Energize.Services.Commands
{
    public class Command
    {
        private static Dictionary<string, bool> _ModulesLoaded = new Dictionary<string, bool>();
        private readonly string _Help;
        private readonly string _Usage;
        private readonly string _ModuleName;

        public Command(string cmd,CommandCallback callback,string help,string usage,string modulename)
        {
            help = help ?? "No help was provided";
            usage = usage ?? "No usage was provided";
            modulename = modulename ?? "None";

            this.Cmd = cmd;
            this.Callback = callback;
            this._Help = help;
            this._Usage = usage;
            this.Loaded = true;
            this._ModuleName = modulename;

            if (!Modules.ContainsKey(modulename))
                Modules[modulename] = new List<Command>();

            this.AddToModule(this, modulename);
        }

        public static Dictionary<string, List<Command>> Modules { get; } = new Dictionary<string, List<Command>>();

        public bool Loaded { get; set; }
        public string Cmd { get; set; }
        public CommandCallback Callback { get; }

        public static bool IsLoadedModule(string module)
        {
            if (!_ModulesLoaded.ContainsKey(module))
                _ModulesLoaded[module] = true;

            return _ModulesLoaded[module];
        }

        public static void SetLoadedModule(string module,bool state)
            => _ModulesLoaded[module] = state;

        public async Task Run(CommandContext ctx)
        {
            try
            {
                await this.Callback(ctx);
            }
            catch(Exception e)
            {
                await ctx.MessageSender.Danger(ctx.Message,"Internal error","Something went wrong, try again?");
                ctx.Log.Nice("CommandError",ConsoleColor.Red,e.ToString());
            }
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
            foreach(Command c in Modules[modulename])
            {
                if(c.Cmd == cmd.Cmd)
                {
                    Modules[modulename].Remove(c);
                    break;
                }
            }

            Modules[modulename].Add(cmd);
        }
    }
}
