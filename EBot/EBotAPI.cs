using DSharpPlus.Entities;
using EBot.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace EBot
{
    [DataContract]
    public class EBotAPI
    {
        [DataMember]
        public ulong ID;

        [DataMember]
        public int UserAmount;

        [DataMember]
        public int GuildAmount;

        [DataMember]
        public int CommandAmount;

        [DataMember]
        public string Status;

        [DataMember]
        public string Prefix;

        [DataMember]
        public string Owner;

        [DataMember]
        public string OwnerStatus;

        [DataMember]
        public string Name;

        [DataMember]
        public string Avatar;

        [DataMember]
        public string OwnerAvatar;

        private static string GetStatus(UserStatus status)
        {
            return Enum.GetName(typeof(UserStatus), status);
        }

        public static async Task SaveAsync(EBotClient client)
        {
            EBotAPI api = new EBotAPI();
            api.GuildAmount = client.Discord.Guilds.Count;
            int useramount = 0;
            foreach (KeyValuePair<ulong, DiscordGuild> guild in client.Discord.Guilds)
            {
                useramount = useramount + guild.Value.MemberCount;
            }
            api.UserAmount = useramount;
            api.CommandAmount = client.Handler.Commands.Count;
            api.Prefix = client.Prefix;
            DiscordUser owner = await client.Discord.GetUserAsync(EBotCredentials.OWNER_ID);
            api.Owner = owner.Username + "#" + owner.Discriminator;
            DiscordUser bot = await client.Discord.GetUserAsync(EBotCredentials.BOT_ID_MAIN);
            api.Name = bot.Username + "#"  + bot.Discriminator;
            api.Status = GetStatus(bot.Presence.Status);
            api.OwnerStatus = GetStatus(owner.Presence.Status);
            api.Avatar = bot.AvatarUrl;
	        api.OwnerAvatar = owner.AvatarUrl;
            api.ID = bot.Id;

            string json = JSON.Serialize(api,client.Log);
            await File.WriteAllTextAsync("External/info.json", json);
        }
    }
}
