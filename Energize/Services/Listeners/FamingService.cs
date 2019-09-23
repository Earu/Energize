using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using Energize.Interfaces.Services.Listeners;
using Energize.Interfaces.Services.Senders;
using System;
using System.Threading.Tasks;

namespace Energize.Services.Listeners
{
    [Service("Fame")]
    public class FamingService : ServiceImplementationBase, IFamingService
    {
        private static readonly Emoji StarEmote = new Emoji("⭐");
        private static readonly Emoji Star2Emote = new Emoji("🌟");

        private readonly ServiceManager ServiceManager;
        private readonly MessageSender MessageSender;
        private readonly Logger Logger;

        public FamingService(EnergizeClient client)
        {
            this.ServiceManager = client.ServiceManager;
            this.MessageSender = client.MessageSender;
            this.Logger = client.Logger;
        }

        private async Task<ITextChannel> CreateFameChannelAsync(IMessage msg)
        {
            string name = "⭐hall-of-fames⭐";
            string desc = $"Memorable, unique messages. Adds a message when reacting with a ⭐ to a message.";
            if (msg.Author is SocketGuildUser guildUser)
            {
                IGuild guild = guildUser.Guild;
                IGuildUser botUser = await guild.GetCurrentUserAsync();
                if (botUser.GuildPermissions.ManageChannels)
                {
                    RestTextChannel chan = await guildUser.Guild.CreateTextChannelAsync(name);
                    OverwritePermissions basePerms = new OverwritePermissions(mentionEveryone: PermValue.Deny, sendMessages: PermValue.Deny, sendTTSMessages: PermValue.Deny);
                    OverwritePermissions botPerms = new OverwritePermissions(sendMessages: PermValue.Allow, addReactions: PermValue.Allow, embedLinks: PermValue.Allow, manageWebhooks: PermValue.Allow);
                    await chan.AddPermissionOverwriteAsync(guildUser.Guild.EveryoneRole, basePerms);
                    await chan.AddPermissionOverwriteAsync(guildUser.Guild.CurrentUser, botPerms);
                    await chan.ModifyAsync(prop => prop.Topic = desc);

                    return chan;
                }
            }

            return null;
        }

        public async Task<ITextChannel> CreateAndSaveFameChannelAsync(IDiscordGuild dbGuild, IMessage msg)
        {
            ITextChannel newChan = await this.CreateFameChannelAsync(msg);
            if (newChan != null)
            {
                dbGuild.HallOfShameID = newChan.Id;
                dbGuild.HasHallOfShames = true;
                return newChan;
            }

            return null;
        }

        public async Task<bool> RemoveFameChannelAsync(IDiscordGuild dbGuild, IMessage msg)
        {
            try
            {
                if (msg.Author is SocketGuildUser guildUser)
                {
                    IGuild guild = guildUser.Guild;
                    IGuildUser botUser = await guild.GetCurrentUserAsync();
                    if (dbGuild.HasHallOfShames && botUser.GuildPermissions.ManageChannels)
                    {
                        IGuildChannel chan = await guild.GetChannelAsync(dbGuild.HallOfShameID);
                        if (chan != null)
                            await chan.DeleteAsync();
                        dbGuild.HasHallOfShames = false;
                        dbGuild.HallOfShameID = 0;

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                this.Logger.Nice("Fame", ConsoleColor.Magenta, $"Could not disable a fame channel: {ex}");
                return false;
            }
        }

        private async Task<ITextChannel> GetOrCreateFameChannelAsync(IMessage msg)
        {
            if (!(msg.Author is SocketGuildUser guildUser)) return null;
            
            IDatabaseService db = this.ServiceManager.GetService<IDatabaseService>("Database");
            using (IDatabaseContext ctx = await db.GetContextAsync())
            {
                IGuild guild = guildUser.Guild;
                IDiscordGuild dbGuild = await ctx.Instance.GetOrCreateGuildAsync(guild.Id);
                    
                if (dbGuild.HasHallOfShames)
                {
                    IGuildChannel chan = await guild.GetChannelAsync(dbGuild.HallOfShameID);
                    if (chan == null)
                        return await this.CreateAndSaveFameChannelAsync(dbGuild, msg);
                    else
                        return chan as ITextChannel;
                }

                return null;
            }
        }

        private async Task SendFameMessageAsync(IMessage msg)
        {
            ITextChannel chan = await this.GetOrCreateFameChannelAsync(msg);
            if (chan == null) return;

            IGuildUser guildUser = await chan.Guild.GetCurrentUserAsync();
            if (guildUser.GetPermissions(chan).Has(ChannelPermission.ManageWebhooks | ChannelPermission.SendMessages | ChannelPermission.AddReactions))
            {
                IWebhookSenderService webhook = this.ServiceManager.GetService<IWebhookSenderService>("Webhook");
                ulong msgId = await webhook.RepostMessageAsync(chan, msg);

                IUserMessage postedMsg;
                if (msgId == 0)
                    postedMsg = await this.MessageSender.RepostMessageAsync(chan, msg);
                else 
                    postedMsg = (IUserMessage)await chan.GetMessageAsync(msgId);

                if (postedMsg != null)
                    await postedMsg.AddReactionAsync(Star2Emote);
            }
        }

        private static bool IsValidReaction(ISocketMessageChannel chan, SocketReaction reaction, ulong authorId)
        {
            if (reaction.Emote?.Name == null) return false;
            if (!reaction.Emote.Name.Equals(StarEmote.Name) && !reaction.Emote.Name.Equals(Star2Emote.Name)) return false;
            if (reaction.UserId == Config.Instance.Discord.BotID || reaction.UserId == authorId) return false;
            if (!(chan is IGuildChannel) || reaction.User.GetValueOrDefault() == null) return false;
            if (reaction.User.Value.IsBot || reaction.User.Value.IsWebhook) return false;

            return true;
        }

        private bool ShouldSendMessage(IUserMessage msg)
        {
            int count = 0;
            if (msg.Reactions.TryGetValue(Star2Emote, out ReactionMetadata start2Data))
                count += start2Data.ReactionCount;

            if (msg.Reactions.TryGetValue(StarEmote, out ReactionMetadata startData))
                count += startData.ReactionCount;

            if (count == 2)
                return true;

            return false;
        }

        [DiscordEvent("ReactionAdded")]
        public async Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!cache.HasValue) return;
            if (!IsValidReaction(chan, reaction, cache.Value.Author.Id)) return;

            if (this.ShouldSendMessage(cache.Value))
                await this.SendFameMessageAsync(cache.Value);
        }
    }
}
