using EBot.Logs;

namespace EBot.Commands
{
    interface ICommandModule
    {
        void Initialize(CommandHandler handler,BotLog log);
    }
}
