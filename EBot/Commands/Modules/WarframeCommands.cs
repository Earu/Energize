using EBot.Utils;
using EBot.Logs;
using System;
using System.Collections.Generic;
using EBot.Commands.Warframe;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace EBot.Commands.Modules
{
    class WarframeCommands : ICommandModule
    {
        private string Name = "Warframe";

        private async Task Alerts(CommandContext ctx)
        {
            string body = await HTTP.Fetch("http://content.warframe.com/dynamic/worldState.php", ctx.Log);
            WGlobal global = JSON.Deserialize<WGlobal>(body,ctx.Log);

            if (global == null)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Err", "Looks like I couldn't access date for that!");
            }
            else
            {

                for (int i = 0; i < global.Alerts.Length; i++)
                {
                    WAlert alert = global.Alerts[i];
                    WMission minfo = alert.MissionInfo;
                    WReward mreward = minfo.missionReward;

                    DateTime endtime = DateTime.Now.Date.AddTicks(alert.Expiry.date.numberLong);
                    DateTime offset = new DateTime().AddHours(5); //canada time offset (server hosted in germany)
                    DateTime nowtime = new DateTime(DateTime.Now.Subtract(offset).Ticks);

                    string showrewards = "";

                    if (mreward.items != null)
                    {
                        showrewards = "**Items**: \n";
                        for (int j = 0; j < mreward.items.Length; j++)
                        {
                            string[] dirs = mreward.items[j].Split("/");
                            string name = dirs[dirs.Length - 1];

                            showrewards += "\t\t" + name + "\n";
                        }
                    }

                    await ctx.EmbedReply.Good(ctx.Message, "Warframe Alert #" + (i + 1),
                          "**Level**: " + minfo.minEnemyLevel + " - " + minfo.maxEnemyLevel + "\t**Type**: " + minfo.missionType.Substring(3).ToLower().Replace("_", " ")
                        + "\t**Enemy**: " + minfo.faction.Substring(3).ToLower() + "\n"
                        + "**Credits**: " + mreward.credits + "\t**Time Left**: " + (endtime.Subtract(nowtime).Minutes) + "mins\n"
                        + showrewards
                    );
                }

            }

        }

        public void Load(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand("walerts",this.Alerts, "Gets the ongoing alerts information","walerts",this.Name);

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload(CommandHandler handler,BotLog log)
        {
            handler.UnloadCommand("walerts");

            log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}