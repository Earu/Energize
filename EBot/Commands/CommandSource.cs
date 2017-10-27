using EBot.Commands.Modules;
using EBot.Logs;

namespace EBot.Commands
{
    public class CommandSource
    {
        UtilsCommands _Utils;
        SocialCommands _Social;
        NSFWCommands _NSFW;
        SearchCommands _Search;
        ImageCommands _Image;
        FunCommands _Fun;
        WarframeCommands _Warframe;

        public CommandSource(CommandsHandler handler,BotLog log)
        {
            this._Utils = new UtilsCommands();
            this._Social = new SocialCommands();
            this._NSFW = new NSFWCommands();
            this._Search = new SearchCommands();
            this._Image = new ImageCommands();
            this._Fun = new FunCommands();
            this._Warframe = new WarframeCommands();

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
            this._Warframe.Setup(handler, log);
        }

        public void LoadCommands(CommandsHandler handler,BotLog log)
        {
            this._Utils.Load();
            this._Social.Load();
            this._NSFW.Load();
            this._Search.Load();
            this._Image.Load();
            this._Fun.Load();
            this._Warframe.Load();
        }

        public void UnloadCommands(CommandsHandler handler,BotLog log)
        {
            this._Utils.Unload();
            this._Social.Unload();
            this._NSFW.Unload();
            this._Search.Unload();
            this._Image.Unload();
            this._Fun.Unload();
            this._Warframe.Unload();
        }
    }
}
