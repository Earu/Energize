using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using Energize.Services.Database.Models;
using Energize.Essentials;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Services.Listeners.Music;
using Energize.Interfaces.Services.Listeners;
using System;

namespace Energize.Services.Database
{
    public class Database : DbContext, IDatabase
    {
        private readonly string ConnectionString;
        private readonly Random Rand;

        public DbSet<DiscordUser> Users { get; set; }
        public DbSet<DiscordGuild> Guilds { get; set; }
        public DbSet<DiscordUserStats> Stats { get; set; }
        public DbSet<YoutubeVideoId> SavedVideoIds { get; set; }

        public Database(string connectionstring)
        {
            this.ConnectionString = connectionstring;
            this.Rand = new Random();
        }
    
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite(this.ConnectionString);

        public void Save()
            => this.SaveChanges(true);

        public async Task<IDiscordUser> GetOrCreateUserAsync(ulong id)
        {
            DiscordUser user = await this.Users.FirstOrDefaultAsync(x => x.ID == id);
            if (user != null)
            {
                return user;
            }
            else
            {
                user = new DiscordUser(id);
                this.Users.Add(user);
                await this.SaveChangesAsync(true);

                return user;
            }
        }

        public async Task<IDiscordUserStats> GetOrCreateUserStatsAsync(ulong id)
        {
            DiscordUserStats stats = await this.Stats.FirstOrDefaultAsync(x => x.ID == id);
            if(stats != null)
            {
                return stats;
            }
            else
            {
                stats = new DiscordUserStats(id);
                this.Stats.Add(stats);
                await this.SaveChangesAsync(true);

                return stats;
            }
        }

        public async Task<IDiscordGuild> GetOrCreateGuildAsync(ulong id)
        {
            DiscordGuild guild = await this.Guilds.FirstOrDefaultAsync(x => x.ID == id);
            if(guild != null)
            {
                return guild;
            }
            else
            {
                guild = new DiscordGuild(id);
                this.Guilds.Add(guild);
                await this.SaveChangesAsync(true);

                return guild;
            }
        }

        public async Task SaveYoutubeVideoIdsAsync(IEnumerable<IYoutubeVideoID> ytVideoIds)
        {
            foreach (IYoutubeVideoID videoId in ytVideoIds)
            {
                if (videoId is YoutubeVideoId id)
                    this.SavedVideoIds.Add(id);
            }

            await this.SaveChangesAsync(true);
        }

        public async Task<IYoutubeVideoID> GetRandomVideoIdAsync()
        {
            List<YoutubeVideoId> vidIds = await this.SavedVideoIds.ToListAsync();
            return vidIds[this.Rand.Next(0, vidIds.Count)];
        }
    }

    [Service("Database")]
    public class DatabaseService : ServiceImplementationBase, IDatabaseService
    {
        private readonly Logger Logger;
        private readonly List<DatabaseContext> Pool;

        public DatabaseService(EnergizeClient client)
        {
            this.Logger = client.Logger;

            this.Pool = new List<DatabaseContext>();
            for (uint i = 0; i < 10; i++)
                this.Pool.Add(new DatabaseContext(Create(), this.Logger));
        }

        public async Task<IDatabaseContext> GetContextAsync()
        {
            foreach (DatabaseContext ctx in this.Pool)
            {
                if(ctx.IsUsed) continue;

                ctx.IsUsed = true;
                return ctx;
            }

            //Wait a bit so we dont try again too early
            await Task.Delay(100);
            return await this.GetContextAsync();
        }

        public IDatabaseContext CreateContext()
            => new DatabaseContext(Create(), this.Logger);

        private static Database Create()
        {
            Database context = new Database(Config.Instance.DBConnectionString);
            context.Database.EnsureCreated();

            return context;
        }
    }
}
