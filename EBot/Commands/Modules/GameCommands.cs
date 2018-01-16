using EBot.Utils;
using EBot.Logs;
using System;
using EBot.Commands.Warframe;
using System.Threading.Tasks;
using EBot.Commands.Steam;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Game")]
    class GameCommands : CommandModule,ICommandModule
    {
        [Command(Name = "walerts", Help = "Gets the warframe ongoing alerts", Usage = "walerts <nothing>")]
        private async Task Alerts(CommandContext ctx)
        {
            string body = await HTTP.Fetch("http://content.warframe.com/dynamic/worldState.php", ctx.Log);
            WGlobal global = JSON.Deserialize<WGlobal>(body,ctx.Log);

            if (global == null)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Warframe Alerts", "Looks like I couldn't get any data!");
            }
            else
            {
                for (uint i = 0; i < global.Alerts.Length; i++)
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

                    await ctx.EmbedReply.Good(ctx.Message, "Alert " + (i + 1) + "/" + global.Alerts.Length,
                        "**Level**: " + minfo.minEnemyLevel + " - " + minfo.maxEnemyLevel + "\t**Type**: " + minfo.missionType.Substring(3).ToLower().Replace("_", " ")
                        + "\t**Enemy**: " + minfo.faction.Substring(3).ToLower() + "\n"
                        + "**Credits**: " + mreward.credits + "\t**Time Left**: " + (endtime.Subtract(nowtime).Minutes) + "mins\n"
                        + showrewards
                      );
                }
            }

        }

        [Command(Name="steam",Help="Find a steam profile",Usage="steam <name|steamid64>")]
        private async Task Steam(CommandContext ctx)
        {
            string id64 = "";
            bool success = ulong.TryParse(ctx.Input,out ulong steamid64);

            if(!success)
            {
                string vanity = "https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1?key=" + EBotConfig.STEAM_API_KEY + "&vanityurl=" + ctx.Input;
                string vanityresponse = await HTTP.Fetch(vanity,ctx.Log);
                SteamVanity result = JSON.Deserialize<SteamVanity>(vanityresponse,ctx.Log);

                if(result.response.success == 1)
                {
                    id64 = result.response.steamid;
                }
                else
                {
                    await ctx.EmbedReply.Danger(ctx.Message,"Steam","Couldn't find any steam profile with your input");
                    return;
                }
            }
            else
            {
                id64 = steamid64.ToString();
            }

            string endpoint = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + EBotConfig.STEAM_API_KEY + "&steamids=" + id64;
            string json = await HTTP.Fetch(endpoint,ctx.Log);

            SteamPlayerSummary summary = JSON.Deserialize<SteamPlayerSummary>(json,ctx.Log);

            if (summary == null)
            {
                await ctx.EmbedReply.Danger(ctx.Message,"Steam","Couldn't find any steam profile with your input");
            }
            else
            {
                SteamUser user = summary.response.players[0];
                DateTime created = new DateTime(1970,1,1,0,0,0,0,DateTimeKind.Utc);
                created = created.AddSeconds(user.timecreated).ToLocalTime();

                string desc = "";
                string visibility = "";
                string datecreated = created.ToString();
                if(user.communityvisibilitystate == 3)
                {
                    visibility = "Public";
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(await HTTP.Fetch(user.profileurl,ctx.Log));
                    HtmlNodeCollection collection = doc.DocumentNode.SelectNodes("//div[contains(@class,'profile_summary')]");
                    HtmlNode node = collection[0];
                    
                    desc += node.InnerHtml;
                    desc = Regex.Replace(desc,"</?br>","\n");
                    desc = Regex.Replace(desc,"<a class=\".+\" href=\"","");
                    desc = Regex.Replace(desc,"\" target=\".+\" rel=\".+\">","");
                    desc = Regex.Replace(desc,"</?.+>","");
                    desc = desc.Replace("https://steamcommunity.com/linkfilter/?url=","");
                    desc = Regex.Replace(desc,@"(https?:\/\/.+)http","http")
                        .Replace("`","").Trim();

                    if(desc.Length > 500)
                    {
                        desc = desc.Substring(0,500) + "...";
                    }
                }
                else
                {
                    visibility = "Private";
                    desc += " - ";
                    datecreated = "???";
                }

                await ctx.EmbedReply.Send(ctx.Message,"Steam",
                    "**NAME:** " + user.personaname + "\n"
                    + "**STATUS:** " + user.GetState() + "\n"
                    + "**CREATED ON:** " + datecreated + "\n"
                    + "**VISIBILITY:** " + visibility + "\n"
                    + "**STEAMID64:** " + user.steamid + "\n"
                    + "**DESCRIPTION:** ```\n" + desc + "```\n"
                    + "**URL:** " + user.profileurl,
                ctx.EmbedReply.ColorGood,user.avatarfull);
            }
        }


        public void Initialize(CommandHandler handler,BotLog log)
        {
            handler.LoadCommand(this.Alerts);
            handler.LoadCommand(this.Steam);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}