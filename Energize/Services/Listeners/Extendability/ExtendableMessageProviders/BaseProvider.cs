using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners.Extendability.ExtendableMessageProviders
{
    class BaseProvider
    {
        private readonly Regex InnerRegex;

        protected BaseProvider(string pattern)
        {
            this.InnerRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string input)
            => this.InnerRegex.IsMatch(input);

        public MatchCollection Matches(string input)
            => this.InnerRegex.Matches(input);

        public virtual Task BuildEmbedsAsync(List<Embed> embeds, IUserMessage msg, SocketReaction reaction)
            => Task.CompletedTask;
    }
}
