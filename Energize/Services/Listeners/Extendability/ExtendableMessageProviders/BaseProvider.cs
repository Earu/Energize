using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners.Extendability.ExtendableMessageProviders
{
    internal class BaseProvider
    {
        private readonly Regex Regex;
        private readonly string Domain;

        protected BaseProvider(string domain, string pattern)
        {
            this.Domain = domain;
            this.Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string input)
            => input.Contains(this.Domain) && this.Regex.IsMatch(input);

        public MatchCollection Matches(string input)
            => this.Regex.Matches(input);

        public virtual Task BuildEmbedsAsync(List<Embed> embeds, IUserMessage msg, SocketReaction reaction)
            => Task.CompletedTask;
    }
}
