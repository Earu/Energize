using EBot.Commands.Modules;
using EBot.Logs;

namespace EBot.Commands
{
    class CommandSource
    {
        UtilsCommands _Utils = new UtilsCommands();
        SocialCommands _Social = new SocialCommands();
        NSFWCommands _NSFW = new NSFWCommands();
        SearchCommands _Search = new SearchCommands();
        ImageCommands _Image = new ImageCommands();
        FunCommands _Fun = new FunCommands();

        public CommandSource(CommandsHandler handler,BotLog log)
        {
            Setup(handler, log);
        }

        private void Setup(CommandsHandler handler,BotLog log)
        {
            this._Utils.Setup(handler, log);
            this._Social.Setup(handler, log);
            this._NSFW.Setup(handler, log);
            this._Search.Setup(handler, log);
            this._Image.Setup(handler, log);
            this._Fun.Setup(handler, log);
        }

        public void LoadCommands(CommandsHandler handler,BotLog log)
        {
            this._Utils.Load();
            this._Social.Load();
            this._NSFW.Load();
            this._Search.Load();
            this._Image.Load();
            this._Fun.Load();
        }

        public void UnloadCommands(CommandsHandler handler,BotLog log)
        {
            this._Utils.Unload();
            this._Social.Unload();
            this._NSFW.Unload();
            this._Search.Unload();
            this._Image.Unload();
            this._Fun.Unload();
        }
    }
}
