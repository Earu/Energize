﻿using Energize.Interfaces.DatabaseModels;
using Energize.Interfaces.Services.Database;
using Energize.Services.Database.Models;
using Energize.Essentials;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Energize.Services.Listeners.Music;
using Energize.Interfaces.Services.Listeners;
using System;
using System.Linq;

namespace Energize.Services.Database
{
    public class Database : DbContext, IDatabase
    {
        private readonly string ConnectionString;

        public DbSet<DiscordUser> Users { get; set; }
        public DbSet<DiscordGuild> Guilds { get; set; }
        public DbSet<DiscordChannel> Channels { get; set; }
        public DbSet<DiscordUserStats> Stats { get; set; }
        public DbSet<YoutubeVideoID> SavedVideoIds { get; set; }

        public Database(string connectionstring)
        {
            this.ConnectionString = connectionstring;
        }
    
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(this.ConnectionString);
        }

        public void Save()
            => this.SaveChanges(true);

        public async Task<IDiscordUser> GetOrCreateUser(ulong id)
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

        public async Task<IDiscordUserStats> GetOrCreateUserStats(ulong id)
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

        public async Task<IDiscordGuild> GetOrCreateGuild(ulong id)
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

        public async Task SaveYoutubeVideoIds(IEnumerable<IYoutubeVideoID> ytVideoIds)
        {
            foreach (YoutubeVideoID videoId in ytVideoIds)
                this.SavedVideoIds.Add(videoId);
            await this.SaveChangesAsync(true);
        }

        public async Task<IYoutubeVideoID> GetRandomVideoIdAsync()
        {
            int count = await this.SavedVideoIds.CountAsync();
            if (count == 0) return null;

            return this.SavedVideoIds.OrderBy(c => Guid.NewGuid()).Take(1).FirstOrDefault();
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
                Pool.Add(new DatabaseContext(this.Create(), this.Logger));
        }

        public async Task<IDatabaseContext> GetContext()
        {
            for(int i = 0; i < this.Pool.Count; i++)
            {
                DatabaseContext ctx = this.Pool[i];
                if(!ctx.IsUsed)
                {
                    ctx.IsUsed = true;
                    return ctx;
                }
            }

            //Wait a bit so we dont try again too early
            await Task.Delay(100);
            return await this.GetContext();
        }

        public IDatabaseContext CreateContext()
            => new DatabaseContext(this.Create(), this.Logger);

        private Database Create()
        {
            var context = new Database(Config.Instance.DBConnectionString);
            context.Database.EnsureCreated();

            return context;
        }
    }
}
