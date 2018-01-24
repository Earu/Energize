using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Energize.Services
{
    public abstract class BaseService
    {
        /// <summary>
        /// Fired when disconnected to the Discord gateway.
        /// </summary>
        /// <param name="ex">Exception that caused us to disconnect</param>
        public virtual Task Disconnected ( Exception ex )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when connected to the Discord gateway.
        /// </summary>
        public virtual Task Connected ( )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a heartbeat is received from the Discord gateway.
        /// </summary>
        /// <param name="oldLatency">Previous latency</param>
        /// <param name="newLatency">Current latency</param>
        public virtual Task LatencyUpdated ( Int32 oldLatency, Int32 newLatency )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when guild data has finished downloading.
        /// </summary>
        public virtual Task Ready ( )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when all reactions to a message are cleared.
        /// </summary>
        /// <param name="message">Message whose reactions were cleared</param>
        /// <param name="channel">Channel where the message is</param>
        public virtual Task ReactionsCleared ( Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a role is created.
        /// </summary>
        /// <param name="role">Role that was created</param>
        public virtual Task RoleCreated ( SocketRole role )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a role is deleted.
        /// </summary>
        /// <param name="role">Role that was deleted</param>
        public virtual Task RoleDeleted ( SocketRole role )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a role is updated.
        /// </summary>
        /// <param name="oldRole">Old role info</param>
        /// <param name="newRole">New role info</param>
        public virtual Task RoleUpdated ( SocketRole oldRole, SocketRole newRole )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when the connected account joins a guild.
        /// </summary>
        /// <param name="guild">The guild we joined</param>
        public virtual Task JoinedGuild ( SocketGuild guild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a reaction is removed from a message.
        /// </summary>
        /// <param name="message">Message that had it's reaction removed</param>
        /// <param name="channel">Channel where the message is</param>
        /// <param name="reaction">Reaction that was removed</param>
        public virtual Task ReactionRemoved ( Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel, SocketReaction reaction )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when the connected account leaves a guild.
        /// </summary>
        /// <param name="guild">The guild we left</param>
        public virtual Task LeftGuild ( SocketGuild guild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a guild becomes unavailable.
        /// </summary>
        /// <param name="guild">Guild that became unavailable</param>
        public virtual Task GuildUnavailable ( SocketGuild guild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when offline guild members are downloaded.
        /// </summary>
        /// <param name="guild">Guild whose members were downloaded</param>
        public virtual Task GuildMembersDownloaded ( SocketGuild guild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a guild is updated.
        /// </summary>
        /// <param name="oldGuild">Old guild info</param>
        /// <param name="newGuild">New guild info</param>
        public virtual Task GuildUpdated ( SocketGuild oldGuild, SocketGuild newGuild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user joins a guild.
        /// </summary>
        /// <param name="user">User that joined</param>
        public virtual Task UserJoined ( SocketGuildUser user )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user leaves a guild.
        /// </summary>
        /// <param name="user">User that left</param>
        public virtual Task UserLeft ( SocketGuildUser user )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user is banned from a guild.
        /// </summary>
        /// <param name="user">todo: describe user parameter on UserBanned</param>
        /// <param name="guild">todo: describe guild parameter on UserBanned</param>
        public virtual Task UserBanned ( SocketUser user, SocketGuild guild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user is unbanned from a guild.
        /// </summary>
        /// <param name="user">User that was banned</param>
        /// <param name="guild">Guild the user was banned from</param>
        public virtual Task UserUnbanned ( SocketUser user, SocketGuild guild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user is updated.
        /// </summary>
        /// <param name="oldUser">Old user info</param>
        /// <param name="newUser">New user info</param>
        public virtual Task UserUpdated ( SocketUser oldUser, SocketUser newUser )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a guild member is updated, or a member
        /// presence is updated.
        /// </summary>
        /// <param name="oldUser">Old info</param>
        /// <param name="newUser">New info</param>
        public virtual Task GuildMemberUpdated ( SocketGuildUser oldUser, SocketGuildUser newUser )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user joins, leaves, or moves voice channels.
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="oldVoiceState">Old voice info</param>
        /// <param name="newVoiceState">New voice info</param>
        public virtual Task UserVoiceStateUpdated ( SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when the connected account is updated.
        /// </summary>
        /// <param name="oldSelfUser">Old info</param>
        /// <param name="newSelfUser">New info</param>
        public virtual Task CurrentUserUpdated ( SocketSelfUser oldSelfUser, SocketSelfUser newSelfUser )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user starts typing.
        /// </summary>
        /// <param name="user">User that started typing</param>
        /// <param name="channel">
        /// Channel that the user is typing in
        /// </param>
        public virtual Task UserIsTyping ( SocketUser user, ISocketMessageChannel channel )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a guild becomes available.
        /// </summary>
        /// <param name="guild">Guild that got available</param>
        public virtual Task GuildAvailable ( SocketGuild guild )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a reaction is added to a message.
        /// </summary>
        /// <param name="message">
        /// Message that had a reaction added to it
        /// </param>
        /// <param name="channel">
        /// Channel where the message is
        /// </param>
        /// <param name="reaction">
        /// Reaction which was added to the message
        /// </param>
        public virtual Task ReactionAdded ( Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel, SocketReaction reaction )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a channel is updated.
        /// </summary>
        /// <param name="oldChannel">Old channel info</param>
        /// <param name="newChannel">New channel info</param>
        public virtual Task ChannelUpdated ( SocketChannel oldChannel, SocketChannel newChannel )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a message is deleted.
        /// </summary>
        /// <param name="message">Message that was deleted</param>
        /// <param name="channel">
        /// Channel which the message was in
        /// </param>
        public virtual Task MessageDeleted ( Cacheable<IMessage, UInt64> message, ISocketMessageChannel channel )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a message is updated.
        /// </summary>
        /// <param name="message1">
        /// todo: describe message1 parameter on MessageUpdated
        /// </param>
        /// <param name="message2">
        /// todo: describe message2 parameter on MessageUpdated
        /// </param>
        /// <param name="channel">
        /// todo: describe channel parameter on MessageUpdated
        /// </param>
        public virtual Task MessageUpdated ( Cacheable<IMessage, UInt64> message1, SocketMessage message2, ISocketMessageChannel channel )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a channel is created.
        /// </summary>
        /// <param name="channel">Channel that was created</param>
        public virtual Task ChannelCreated ( SocketChannel channel )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user is removed from a group channel.
        /// </summary>
        /// <param name="groupUser">Recipiend that was removed</param>
        public virtual Task RecipientRemoved ( SocketGroupUser groupUser )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a user joins a group channel.
        /// </summary>
        /// <param name="groupUser">Recipiend that was added</param>
        public virtual Task RecipientAdded ( SocketGroupUser groupUser )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a message is received.
        /// </summary>
        /// <param name="message">Message received</param>
        public virtual Task MessageReceived ( SocketMessage message )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a channel is destroyed.
        /// </summary>
        /// <param name="channel">Channel that was destroyed</param>
        public virtual Task ChannelDestroyed ( SocketChannel channel )
        {
            return Task.CompletedTask;
        }
    }
}
