using Energize.Utils;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using Energize.Services.Commands.Chuck;
using Discord;
using System.Linq;
using System.Collections.Generic;
using HtmlAgilityPack;
using Energize.Services.Markov;
using Energize.Services.TextProcessing;

namespace Energize.Services.Commands.Modules
{
    [CommandModule("Fun")]
    class FunCommands
    {
        [Command(Name="ascii",Help="Makes a text/sentence ascii art",Usage="ascii <sentence>")]
        private async Task ASCII(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
            }
            else
            {
                string body = await HTTP.Fetch("http://artii.herokuapp.com/make?text=" + ctx.Input,ctx.Log);
                if (body.Length > 2000)
                {
                    await ctx.MessageSender.Danger(ctx.Message, "ASCII", "The word or sentence you provided is too long!");
                }
                else
                {
                    await ctx.MessageSender.SendRaw(ctx.Message,"```\n" + body + "\n```");
                }

            }
        }

        [Command(Name="describe",Help="Does a description of a user",Usage="describe <@user|nothing>")]
        private async Task Describe(CommandContext ctx)
        {
            string[] adjs = EnergizeData.ADJECTIVES;
            string[] nouns = EnergizeData.NOUNS;
            SocketUser toping = ctx.Message.Author;
            if (ctx.TryGetUser(ctx.Arguments[0],out SocketUser user))
            {
                toping = user;
            }

            Random random = new Random();
            int times = random.Next(1, 4);
            string result = "";

            for (int i = 0; i < times; i++)
            {
                bool of = random.Next(1, 100) > 50;
                if (of)
                {
                    result += adjs[random.Next(0, adjs.Length)] + " ";
                    result += nouns[random.Next(0, nouns.Length)].ToLower();
                    result += " of the ";
                    result += nouns[random.Next(0, nouns.Length)].ToLower();
                }
                else
                {
                    string randadj = adjs[random.Next(0, adjs.Length)];
                    result += randadj;
                }
                result += " ";
            }
            result += nouns[random.Next(0, nouns.Length)].ToLower();

            bool isvowel = EnergizeData.VOWELS.Any(x => result.StartsWith(x));
            await ctx.MessageSender.Good(ctx.Message, "Description", toping.Mention + " is " + (isvowel ? "an" : "a") + " " + result);
        }

        [Command(Name="letters",Help="Transforms a sentence into letter emotes",Usage="letters <sentence>")]
        private async Task Letters(CommandContext ctx)
        {
            if (ctx.HasArguments)
            {
                string input = ctx.Input;
                string indicator = ":regional_indicator_";
                string result = "";
                for (int i = 0; i < input.Length; i++)
                {
                    string letter = input[i].ToString().ToLower();
                    if (Regex.IsMatch(letter, @"[A-Za-z]"))
                    {
                        result += indicator + letter + ": ";
                    }
                    else if (Regex.IsMatch(letter, @"\s"))
                    {
                        result += "\t";
                    }
                }

                if(result.Length == 0)
                {
                    await ctx.MessageSender.Danger(ctx.Message, "Letters", "Your message contained only non-letters characters");
                }
                else if(result.Length > 2000)
                {
                    await ctx.MessageSender.Danger(ctx.Message,"Letters","Message was too long to be sent");
                }
                else
                {
                    await ctx.MessageSender.Good(ctx.Message, "Letters", result);
                }
            }
            else
            {
                await ctx.SendBadUsage();
            }
        }

        [Command(Name="8b",Help="Gives a negative or positive answer to a question",Usage="8b <question>")]
        private async Task EightBalls(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
            }
            else
            {
                Random rand = new Random();
                string[] answers = EnergizeData.EIGHT_BALL_ANSWERS;
                string answer = answers[rand.Next(0, answers.Length - 1)];

                await ctx.MessageSender.Good(ctx.Message,"8ball",answer);
            }
        }

        [Command(Name="pick",Help="Picks a choice among those given",Usage="pick <choice>,<choice>,<choice|nothing>,...")]
        private async Task Pick(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
            }
            else
            {
                string[] answers = EnergizeData.PICK_ANSWERS;
                Random rand = new Random();
                string choice = ctx.Arguments[rand.Next(0, ctx.Arguments.Count - 1)].Trim();
                string answer = answers[rand.Next(0, answers.Length - 1)].Replace("<answer>", choice);

                await ctx.MessageSender.Good(ctx.Message,"Pick", answer);
            }
        }

        [Command(Name="m",Help="Generates a random sentence based on input",Usage="m <input|nothing>")]
        private async Task Markov(CommandContext ctx)
        {
            string sentence = ctx.Input;
            string generated = ServiceManager.GetService<MarkovHandler>("Markov").Generate(sentence);
            if(string.IsNullOrWhiteSpace(generated))
            {
                await ctx.MessageSender.Danger(ctx.Message,"Markov","Generated nothing??!");
                return;
            }
            generated = Regex.Replace(generated,"\\s\\s"," ");
            await ctx.MessageSender.Good(ctx.Message,"Markov", generated);
        }

        [Command(Name="chuck",Help="Gets a random chuck norris fact",Usage="chuck <nothing>")]
        private async Task Chuck(CommandContext ctx)
        {
            string endpoint = "https://api.chucknorris.io/jokes/random";
            string json = await HTTP.Fetch(endpoint, ctx.Log);
            FactObject fact = JSON.Deserialize<FactObject>(json, ctx.Log);

            await ctx.MessageSender.Good(ctx.Message, "Chuck Norris Fact", fact.value);
        }

        [Command(Name="meme",Help="Gets a random meme picture",Usage="meme <nothing>")]
        private async Task Meme(CommandContext ctx)
        {
            string endpoint = "https://api.imgflip.com/get_memes";
            string json = await HTTP.Fetch(endpoint, ctx.Log);
            Meme.ResponseObject response = JSON.Deserialize<Meme.ResponseObject>(json, ctx.Log);
            Random rand = new Random();
            string url = response.data.memes[rand.Next(0, response.data.memes.Length - 1)].url;
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithAuthor(ctx.Message.Author);
            builder.WithImageUrl(url);
            builder.WithFooter("Meme");
            builder.WithColor(ctx.MessageSender.ColorGood);

            await ctx.MessageSender.Send(ctx.Message, builder.Build());
        }

        [Command(Name="gname",Help="Gets a random username",Usage="gname <nothing>")]
        private async Task GenName(CommandContext ctx)
        {
            Random rand = new Random();
            if(rand.Next(1,100) < 30)
            {
                string endpoint = "https://randomuser.me/api/";
                string json = await HTTP.Fetch(endpoint, ctx.Log);
                GenName.ResultObject result = JSON.Deserialize<GenName.ResultObject>(json, ctx.Log);

                if (result != null)
                {
                    await ctx.MessageSender.Good(ctx.Message, "GName", result.results[0].login.username);
                }
                else
                {
                    await ctx.MessageSender.Danger(ctx.Message, "GName", "Couldn't generate a new username");
                }
            }
            else
            {
                string result = "";
                result += EnergizeData.ADJECTIVES[rand.Next(0, EnergizeData.ADJECTIVES.Length - 1)];
                result += EnergizeData.NOUNS[rand.Next(0, EnergizeData.NOUNS.Length - 1)];
                if(rand.Next(1,100) < 30)
                {
                    result += rand.Next(0, 1000);
                }

                result = result.Replace("-", "").ToLower();

                await ctx.MessageSender.Good(ctx.Message, "GName", result);
            }

        }

        [Command(Name="files",Help="?",Usage="?")]
        private async Task XFiles(CommandContext ctx)
        {
            if (ctx.Handler.Prefix == "x")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(ctx.MessageSender.ColorGood);
                builder.WithAuthor(ctx.Message.Author);
                builder.WithFooter("XFiles");
                builder.WithImageUrl("https://i.imgur.com/kWK2BEW.png");

                await ctx.MessageSender.Send(ctx.Message, builder.Build());
            }
        }

        [Command(Name="style",Help="Sets a typing style for yourself or use one on a sentence",Usage="style <style>,<toggle|sentence>")]
        private async Task Style(CommandContext ctx)
        {
            if(!ctx.HasArguments)
            {
                await ctx.SendBadUsage();
                return;
            }

            TextStyle style = ServiceManager.GetService<TextStyle>("TextStyle");
            if(ctx.Arguments.Count > 1)
            {
                if(!style.GetStyles().Any(x => x == ctx.Arguments[0]))
                {
                    await ctx.MessageSender.Danger(ctx.Message,"Style","Styles available:\n`" + string.Join(",",style.GetStyles()) + "`");
                    return;
                }

                if(ctx.Arguments[1].Trim() == "toggle")
                {
                    if(ctx.IsPrivate)
                    {
                        await ctx.MessageSender.Danger(ctx.Message,"Style","You can't toggle a style in DM!");
                        return;
                    }

                    SocketGuildUser user = ctx.Message.Author as SocketGuildUser;
                    string identifier = "EnergizeStyle: ";
                    string rolename = identifier + ctx.Arguments[0];

                    IGuild guild = user.Guild as IGuild;
                    IGuildUser bot = await guild.GetUserAsync(ctx.Client.CurrentUser.Id);
                    if(!bot.GuildPermissions.ManageMessages || !bot.GuildPermissions.ManageRoles)
                    {
                        await ctx.MessageSender.Danger(ctx.Message,"Style","I dont seem to have the rights for that!");
                        return;
                    }

                    if(ctx.HasRole(user,rolename)) //untoggle
                    {
                        IRole oldrole = await ctx.GetOrCreateRole(user,rolename);
                        await user.RemoveRoleAsync(oldrole);

                        await ctx.MessageSender.Good(ctx.Message,"Style","Untoggled style");
                        return;
                    }

                    if(ctx.HasRoleStartingWith(user,identifier)) //changes of style
                    {
                        IRole oldrole = user.Roles.Where(x => x.Name != null && x.Name.StartsWith(identifier)).First();
                        await user.RemoveRoleAsync(oldrole);
                    }

                    IRole newrole = await ctx.GetOrCreateRole(user,rolename);
                    await user.AddRoleAsync(newrole);

                    await ctx.MessageSender.Good(ctx.Message,"Style","Style applied");
                }
                else if(!string.IsNullOrWhiteSpace(ctx.Arguments[1]))
                {
                    List<string> parts = new List<string>(ctx.Arguments);
                    parts.RemoveAt(0);

                    string result = style.GetStyleResult(string.Join(",",parts),ctx.Arguments[0]);

                    await ctx.MessageSender.Good(ctx.Message,"Style",result);
                }
                else
                {
                    await ctx.SendBadUsage();
                }
            }
            else
            {
                await ctx.SendBadUsage();
            }
        }

        [Command(Name="oldswear",Help="Generates old swear words and insults",Usage="oldswear <nothing>")]
        private async Task OldInsult(CommandContext ctx)
        {
            string html = await HTTP.Fetch("http://www.pangloss.com/seidel/Shaker/",ctx.Log);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode node = doc.DocumentNode.SelectNodes("//font").FirstOrDefault();

            if(node == null)
            {
                await ctx.MessageSender.Danger(ctx.Message,"Old Insult","Seems like the website is down");
                return;
            }

            await ctx.MessageSender.Good(ctx.Message,"Old Insult",node.InnerText);
        }
    }
}
