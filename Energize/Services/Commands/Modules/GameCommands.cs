using Energize.Utils;
using System;
using Energize.Services.Commands.Warframe;
using System.Threading.Tasks;
using Energize.Services.Commands.Steam;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Energize.Services.Commands.Modules
{
    [CommandModule("Game")]
    class GameCommands
    {
        [Command(Name = "walerts", Help = "Gets the warframe ongoing alerts", Usage = "walerts <nothing>")]
        private async Task Alerts(CommandContext ctx)
        {
            string body = await HTTP.Fetch("http://content.warframe.com/dynamic/worldState.php", ctx.Log);
            WGlobal global = JSON.Deserialize<WGlobal>(body,ctx.Log);

            if (global == null)
            {
                await ctx.MessageSender.Danger(ctx.Message, "Warframe Alerts", "Looks like I couldn't get any data!");
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

                    await ctx.MessageSender.Good(ctx.Message, "Alert " + (i + 1) + "/" + global.Alerts.Length,
                        "**Level**: " + minfo.minEnemyLevel + " - " + minfo.maxEnemyLevel + "\t**Type**: " + minfo.missionType.Substring(3).ToLower().Replace("_", " ")
                        + "\t**Enemy**: " + minfo.faction.Substring(3).ToLower() + "\n"
                        + "**Credits**: " + mreward.credits + "\t**Time Left**: " + (endtime.Subtract(nowtime).Minutes) + "mins\n"
                        + showrewards
                      );
                }
            }

        }

        [Command(Name="steam",Help="Find a steam profile",Usage="steam <name|steamid|steamid64>")]
        private async Task Steam(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            string id64 = "";
            bool success = ulong.TryParse(ctx.Input,out ulong steamid64) && ctx.Input.Length == 17;
            if(success)
            {
                id64 = steamid64.ToString();
            }
            
            if (!success && ctx.Input.StartsWith("STEAM_"))
            {
                string[] parts = ctx.Input.Split(":");
                if(parts.Length == 3 && long.TryParse(parts[1],out long y) && long.TryParse(parts[2],out long z))
                {
                    long identifier = 0x0110000100000000;
                    id64 = (z * 2 + identifier + y).ToString();
                    success = true;
                }
            }

            if(!success)
            {
                string vanity = "https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1?key=" + EnergizeConfig.STEAM_API_KEY + "&vanityurl=" + ctx.Input;
                string vanityresponse = await HTTP.Fetch(vanity,ctx.Log);
                SteamVanity result = JSON.Deserialize<SteamVanity>(vanityresponse,ctx.Log);

                if(result.response.success == 1)
                {
                    id64 = result.response.steamid;
                }
                else
                {
                    await ctx.MessageSender.Danger(ctx.Message,"Steam","Couldn't find any steam profile with your input");
                    return;
                }
            }

            string endpoint = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=" + EnergizeConfig.STEAM_API_KEY + "&steamids=" + id64;
            string json = await HTTP.Fetch(endpoint,ctx.Log);

            SteamPlayerSummary summary = JSON.Deserialize<SteamPlayerSummary>(json,ctx.Log);

            if (summary == null)
            {
                await ctx.MessageSender.Danger(ctx.Message,"Steam","Couldn't find any steam profile with your input");
            }
            else
            {
                if(summary.response.players.Length < 1)
                {
                    await ctx.MessageSender.Danger(ctx.Message,"Steam","Your input was correct but it doesnt belong to anyone on steam");
                    return;
                }

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
                    string html = await HTTP.Fetch(user.profileurl,ctx.Log);
                    doc.LoadHtml(html);
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

                await ctx.MessageSender.Send(ctx.Message,"Steam",
                    "**NAME:** " + user.personaname + "\n"
                    + "**STATUS:** " + user.GetState() + "\n"
                    + "**CREATED ON:** " + datecreated + "\n"
                    + "**VISIBILITY:** " + visibility + "\n"
                    + "**STEAMID64:** " + user.steamid + "\n"
                    + "**DESCRIPTION:** ```\n" + desc + "```\n"
                    + "**URL:** " + user.profileurl,
                ctx.MessageSender.ColorGood,user.avatarfull);
            }
        }

        [Command(Name = "minesweeper", Help = "Minesweeper minigame", Usage = "minesweeper <width, height, mineamount>")]
        private async Task MineSweeper(CommandContext ctx)
        {
            if (int.TryParse(ctx.Arguments[0], out int width)
                && int.TryParse(ctx.Arguments[1], out int height)
                && int.TryParse(ctx.Arguments[2], out int amount))
            {
                if(width > 10 || height > 10)
                {
                    await ctx.MessageSender.Warning(ctx.Message, "MineSweeper", "Maximum width and height is 10");
                    return;
                }

                int maxmines = height * width;
                if (amount > maxmines)
                {
                    await ctx.MessageSender.Warning(ctx.Message, "MineSweeper", "Can't have more bombs than squares");
                    return;
                }

                Random rand = new Random();
                Dictionary<(int, int), int> mines = new Dictionary<(int, int), int>();
                for(int i=0; i<amount;i++)
                {
                    int x = rand.Next(0, width);
                    int y = rand.Next(0, height);
                    while(mines.ContainsKey((y, x)))
                    {
                        x = rand.Next(0, width);
                        y = rand.Next(0, height);
                    }

                    mines.Add((y, x), 0);
                }

                string mine = ":boom:";
                string result = string.Empty;
                string[] types = new string[]
                {
                    "white_large_square", "one", "two", "three", "four", "five", "six", "seven", "height"
                };

                for(int y = 0; y < height; y++)
                {
                    for(int x = 0; x < width; x++)
                    {
                        string casecontent;
                        if (mines.ContainsKey((y, x)))
                        {
                            casecontent = mine;
                        }
                        else
                        {
                            int minecount = 0;
                            //above
                            if (mines.ContainsKey((y - 1, x - 1)) && mines[(y - 1, x - 1)] == 0)
                                minecount++;
                            if (mines.ContainsKey((y - 1, x)) && mines[(y - 1, x)] == 0)
                                minecount++;
                            if (mines.ContainsKey((y - 1, x + 1)) && mines[(y - 1, x + 1)] == 0)
                                minecount++;

                            //around
                            if (mines.ContainsKey((y, x - 1)) && mines[(y, x - 1)] == 0)
                                minecount++;
                            if (mines.ContainsKey((y, x + 1)) && mines[(y, x + 1)] == 0)
                                minecount++;

                            //under
                            if (mines.ContainsKey((y + 1, x - 1)) && mines[(y + 1, x - 1)] == 0)
                                minecount++;
                            if (mines.ContainsKey((y + 1, x)) && mines[(y + 1, x)] == 0)
                                minecount++;
                            if (mines.ContainsKey((y + 1, x + 1)) && mines[(y + 1, x + 1)] == 0)
                                minecount++;

                            casecontent = $":{types[minecount]}:";
                        }

                        result += $"||{casecontent}||";
                    }

                    result += "\n";
                }

                if(result.Length > 2000)
                {
                    await ctx.MessageSender.Warning(ctx.Message, "MineSweeper", "Result was too long to be displayed");
                    return;
                }

                await ctx.MessageSender.Send(ctx.Message, "MineSweeper", result);
            }
            else
                await ctx.SendBadUsage();
        }
    }
}