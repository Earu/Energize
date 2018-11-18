﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Rest;
using System.IO;

namespace Energize
{
    public class EnergizeClient
    {
        private readonly string _Token;

        public EnergizeClient(string token, string prefix)
        {
            Console.Clear();
            Console.Title = "Energize's Logs";

            this._Token        = token;
            this.Prefix        = prefix;
            this.Log           = new EnergizeLog();
            this.MessageSender = new EnergizeMessage(this.Log);
            this.Discord       = new DiscordShardedClient(new DiscordSocketConfig
            {
                MessageCacheSize = 1000,
            });
            this.DiscordREST = new DiscordRestClient();

            this.Log.Nice("Config", ConsoleColor.Yellow, "Token used => [ " + token + " ]");
            this.Log.Notify("Initializing");

            Services.ServiceManager.LoadServices(this);
        }

        public string               Prefix        { get; }
        public DiscordShardedClient Discord       { get; }
        public DiscordRestClient    DiscordREST   { get; }
        public EnergizeLog          Log           { get; }
        public EnergizeMessage      MessageSender { get; }

        public async Task InitializeAsync()
        {
            try
            {
                if(File.Exists("logs.txt"))
                    File.Delete("logs.txt");

                await this.Discord.LoginAsync(TokenType.Bot, _Token, true);
                await this.Discord.StartAsync();
                await this.DiscordREST.LoginAsync(TokenType.Bot, _Token, true);
                await Services.ServiceManager.LoadServicesAsync(this);

                StreamingGame game = new StreamingGame($"{this.Prefix}help | {this.Prefix}info",EnergizeConfig.TWITCH_URL);
                await this.Discord.SetActivityAsync(game);

                Timer gctimer = new Timer(arg =>
                {
                    long mb = GC.GetTotalMemory(false) / 1024 / 1024; //b to mb
                    GC.Collect();
                    this.Log.Nice("GC", ConsoleColor.Gray, "Collected " + mb + "MB of garbage");
                });

                int hour = 1000 * 60 * 60;
                gctimer.Change(hour, hour);

            }
            catch (Exception e)
            {
                this.Log.Nice("Init", ConsoleColor.Red, $"Something went wrong: {e.Message}");
            }
        }
    }
}
