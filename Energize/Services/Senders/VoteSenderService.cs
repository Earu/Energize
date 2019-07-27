using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Essentials.MessageConstructs;
using Energize.Interfaces.Services.Senders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Energize.Services.Senders
{
    [Service("Votes")]
    public class VoteSenderService : ServiceImplementationBase, IVoteSenderService
    {
        private static readonly Dictionary<string, int> Lookup = new Dictionary<string, int>
        {
            ["1⃣"] = 0, ["2⃣"] = 1, ["3⃣"] = 2, ["4⃣"] = 3, ["5⃣"] = 4,
            ["6⃣"] = 5, ["7⃣"] = 6, ["8⃣"] = 7, ["9⃣"] = 8,
        };

        private readonly Logger Logger;
        private readonly MessageSender MessageSender;
        private readonly ConcurrentDictionary<ulong, Vote> Votes;

        public VoteSenderService(EnergizeClient client)
        {
            this.Logger = client.Logger;
            this.MessageSender = client.MessageSender;
            this.Votes = new ConcurrentDictionary<ulong, Vote>();
        }

        private static async Task AddReactions(IUserMessage msg, int choiceCount)
        {
            if (msg.Channel is SocketGuildChannel chan)
            {
                if (!chan.Guild.CurrentUser.GetPermissions(chan).AddReactions)
                    return;
            }

            for(int i = 0; i < choiceCount; i++)
                await msg.AddReactionAsync(new Emoji($"{i + 1}\u20e3"));
        }

        public async Task<IUserMessage> SendVote(IMessage msg, string description, IEnumerable<string> choices)
        {
            try
            {
                Vote vote = new Vote(msg.Author, description, choices.ToList());
                vote.Message = await this.MessageSender.Send(msg, vote.VoteEmbed);
                vote.VoteFinished += async result =>
                {
                    await vote.Message.DeleteAsync();
                    await this.MessageSender.Send(msg, vote.VoteEmbed);
                    
                    this.Votes.TryRemove(vote.Message.Id, out Vote _);
                };

                if (!this.Votes.TryAdd(vote.Message.Id, vote))
                    return null;
            
                await AddReactions(vote.Message, vote.ChoiceCount);
                return vote.Message;
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Vote", ConsoleColor.Yellow, $"Could not create vote: {ex.Message}");
            }

            return null;
        }

        private static bool IsValidEmote(SocketReaction reaction)
        {
            if (reaction.UserId == Config.Instance.Discord.BotID || reaction.Emote?.Name == null)
                return false;

            return Lookup.ContainsKey(reaction.Emote.Name);
        }

        private bool IsValidReaction(Cacheable<IUserMessage, ulong> cache, SocketReaction reaction)
            => IsValidEmote(reaction) && this.Votes.ContainsKey(cache.Id);

        [DiscordEvent("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel _, SocketReaction reaction)
        {
            if (!this.IsValidReaction(cache, reaction)) return;

            int index = Lookup[reaction.Emote.Name];
            await this.Votes[cache.Id].AddVote(reaction.User.Value, index);
        }

        [DiscordEvent("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel _, SocketReaction reaction)
        {
            if (!this.IsValidReaction(cache, reaction)) return;

            int index = Lookup[reaction.Emote.Name];
            await this.Votes[cache.Id].RemoveVote(reaction.User.Value, index);
        }
    }
}
