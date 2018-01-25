using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Energize.Services.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Energize.Services.MemoryStream
{
    public class ClientInfo
    {
        private EnergizeClient _Client;
        private CommandHandler _Handler;

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

        public ClientInfo(EnergizeClient client,CommandHandler handler)
        {
            this._Client = client;
            this._Handler = handler;
        }

        public async Task<ClientInfo> Initialize()
        {
            RestApplication app = await this._Client.Discord.GetApplicationInfoAsync();
            IUser owner = app.Owner;
            SocketUser bot = this._Client.Discord.CurrentUser as SocketUser;
            owner = this._Client.Discord.GetUser(app.Owner.Id) ?? app.Owner;

            int useramount = 0;
            foreach (SocketGuild guild in this._Client.Discord.Guilds)
            {
                useramount = useramount + guild.MemberCount;
            }

            this._GuildAmount = this._Client.Discord.Guilds.Count;
            this._UserAmount = useramount;
            this._CommandAmount = this._Handler.Commands.Count;
            this._Prefix = this._Client.Prefix;
            this._Owner = owner.Username + "#" + owner.Discriminator;
            this._Name = bot.Username + "#" + bot.Discriminator;
            this._Status = this.GetStatus(bot.Status);
            this._OwnerStatus = this.GetStatus(owner.Status);
            this._Avatar = bot.GetAvatarUrl(ImageFormat.Png,1024);
            this._OwnerAvatar = owner.GetAvatarUrl(ImageFormat.Png, 1024);
            this._ID = bot.Id;

            return this;
        }
    }
}
