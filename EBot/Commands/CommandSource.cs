using EBot.Commands.Modules;
using EBot.Logs;

namespace EBot.Commands
{
    public class CommandSource
    {
        private UtilsCommands _Utils;
        private SocialCommands _Social;
        private NSFWCommands _NSFW;
        private SearchCommands _Search;
        private ImageCommands _Image;
        private FunCommands _Fun;
        private WarframeCommands _Warframe;
        private CommandHandler _Handler;
        private BotLog _Log;

        public CommandSource(CommandHandler handler,BotLog log)
        {
            this._Utils = new UtilsCommands();
            this._Social = new SocialCommands();
            this._NSFW = new NSFWCommands();
            this._Search = new SearchCommands();
            this._Image = new ImageCommands();
            this._Fun = new FunCommands();
            this._Warframe = new WarframeCommands();
            this._Handler = handler;
            this._Log = log;
        }

        public void LoadCommands()
        {
            this._Utils.Load(_Handler,_Log);
            this._Social.Load(_Handler, _Log);
            this._NSFW.Load(_Handler, _Log);
            this._Search.Load(_Handler, _Log);
            this._Image.Load(_Handler, _Log);
            this._Fun.Load(_Handler, _Log);
            this._Warframe.Load(_Handler, _Log);
        }

        public void UnloadCommands()
        {
            this._Utils.Unload(_Handler, _Log);
            this._Social.Unload(_Handler, _Log);
            this._NSFW.Unload(_Handler, _Log);
            this._Search.Unload(_Handler, _Log);
            this._Image.Unload(_Handler, _Log);
            this._Fun.Unload(_Handler, _Log);
            this._Warframe.Unload(_Handler, _Log);
        }
    }
}
