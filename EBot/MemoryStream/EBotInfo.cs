using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EBot.MemoryStream
{
    public class EBotInfo
    {
        private EBotClient _Client;

        private ulong _ID;
        private int _UserAmount;
        private int _GuildAmount;
        private int _CommandAmount;
        private string _Status;
        private string _Prefix;
        private string _Owner;
        private string _OwnerStatus;
        private string _Name;
        private string _Avatar;
        private string _OwnerAvatar;

        public ulong ID { get => this._ID; }
        public int UserAmount { get => this._UserAmount; }
        public int GuildAmount { get => this._GuildAmount; }
        public int CommandAmount { get => this._CommandAmount; }
        public string Status { get => this._Status; }
        public string Prefix { get => this._Prefix; }
        public string Owner { get => this._Owner; }
        public string OwnerStatus { get => this._OwnerStatus; }
        public string Name { get => this._Name; }
        public string Avatar { get => this._Avatar; }
        public string OwnerAvatar { get => this._OwnerAvatar; }

        private string GetStatus(UserStatus status)
        {
            return Enum.GetName(typeof(UserStatus), status);
        }

        public EBotInfo(EBotClient client)
        {
            this._Client = client;
        }

        public async Task<EBotInfo> Initialize()
        {
            DiscordUser owner = await this._Client.Discord.GetUserAsync(EBotCredentials.OWNER_ID);
            DiscordUser bot = await this._Client.Discord.GetUserAsync(EBotCredentials.BOT_ID_MAIN);
            int useramount = 0;
            foreach (KeyValuePair<ulong, DiscordGuild> guild in this._Client.Discord.Guilds)
            {
                useramount = useramount + guild.Value.MemberCount;
            }

            this._GuildAmount = _Client.Discord.Guilds.Count;
            this._UserAmount = useramount;
            this._CommandAmount = _Client.Handler.Commands.Count;
            this._Prefix = _Client.Prefix;
            this._Owner = owner.Username + "#" + owner.Discriminator;
            this._Name = bot.Username + "#" + bot.Discriminator;
            this._Status = GetStatus(bot.Presence.Status);
            this._OwnerStatus = GetStatus(owner.Presence.Status);
            this._Avatar = bot.AvatarUrl;
            this._OwnerAvatar = owner.AvatarUrl;
            this._ID = bot.Id;

            return this;
        }
    }
}
