using Energize.Logs;

namespace Energize.Commands
{
    interface ICommandModule
    {
        void Initialize(CommandHandler handler,BotLog log);
    }
}
