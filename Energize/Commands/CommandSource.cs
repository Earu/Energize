using Energize.Commands.Modules;
using Energize.Logs;
using System.Collections.Generic;

namespace Energize.Commands
{
    public class CommandSource
    {
        private CommandHandler _Handler;
        private BotLog _Log;

        public CommandSource(CommandHandler handler,BotLog log)
        {
            this._Handler = handler;
            this._Log = log;
        }

        public void Initialize()
        {
            new FunCommands().Initialize(this._Handler, this._Log);
            new ImageCommands().Initialize(this._Handler, this._Log);
            new InfoCommands().Initialize(this._Handler, this._Log);
            new NSFWCommands().Initialize(this._Handler, this._Log);
            new SearchCommands().Initialize(this._Handler, this._Log);
            new SocialCommands().Initialize(this._Handler, this._Log);
            new UtilsCommands().Initialize(this._Handler, this._Log);
            new GameCommands().Initialize(this._Handler, this._Log);
            new AdministrationCommands().Initialize(this._Handler, this._Log);
        }
    }
}
