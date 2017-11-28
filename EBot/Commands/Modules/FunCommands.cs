using EBot.Utils;
using EBot.Logs;
using EBot.MachineLearning;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using EBot.Commands.Chuck;
using Discord;
using System.Linq;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Fun")]
    class FunCommands : CommandModule,ICommandModule
    {
        [Command(Name="ascii",Help="Makes a text/sentence ascii art",Usage="ascii <sentence>")]
        private async Task ASCII(CommandContext ctx)
        {
            if (ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "ASCII", "You didn't provide any word or sentence!");
            }
            else
            {
                string body = await HTTP.Fetch("http://artii.herokuapp.com/make?text=" + ctx.Input,ctx.Log);
                if (body.Length > 2000)
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "ASCII", "The word or sentence you provided is too long!");
                }
                else
                {
                    await ctx.EmbedReply.Good(ctx.Message,"ASCII","```\n" + body + "\n```");
                }

            }
        }

        [Command(Name="describe",Help="Does a description of a user",Usage="describe <@user>")]
        private async Task Describe(CommandContext ctx)
        {
            string[] adjs = CommandsData.Adjectives;
            string[] nouns = CommandsData.Nouns;
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

            bool isvowel = CommandsData.Vowels.Any(x => result.StartsWith(x));
            await ctx.EmbedReply.Good(ctx.Message, "Description", toping.Mention + " is " + (isvowel ? "an" : "a") + " " + result);
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
                await ctx.EmbedReply.Good(ctx.Message, "Letters", result);
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Letters", "You didn't provide any sentence");
            }
        }

        [Command(Name="8ball",Help="Gives a negative or positive answer to a question",Usage="8ball <question>")]
        private async Task EightBalls(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "8ball", "You didn't provide any word or sentence!");
            }
            else
            {
                Random rand = new Random();
                string[] answers = CommandsData.HeightBallAnswers;
                string answer = answers[rand.Next(0, answers.Length - 1)];

                await ctx.EmbedReply.Good(ctx.Message,"8ball",answer);
            }
        }

        [Command(Name="pick",Help="Picks a choice among those given",Usage="pick <choice>,<choice>,<choice|nothing>,...")]
        private async Task Pick(CommandContext ctx)
        {
            if (!ctx.HasArguments)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Pick", "You didn't provide any/enough word(s)!");
            }
            else
            {
                string[] answers = CommandsData.PickAnswers;
                Random rand = new Random();
                string choice = ctx.Arguments[rand.Next(0, ctx.Arguments.Count - 1)].Trim();
                string answer = answers[rand.Next(0, answers.Length - 1)].Replace("<answer>", choice);

                await ctx.EmbedReply.Good(ctx.Message,"Pick", answer);
            }
        }

        [Command(Name="m",Help="Generates a random sentence based on input",Usage="m <input|nothing>")]
        private async Task Markov(CommandContext ctx)
        {
            string sentence = ctx.Input;
            try
            {
                string generated = MarkovHandler.Generate(sentence);
                await ctx.EmbedReply.Good(ctx.Message,"Markov", generated);
            }
            catch(Exception e)
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Markov", "Something went wrong:\n" + e.ToString());
            }
        }

        [Command(Name="chuck",Help="Gets a random chuck norris fact",Usage="chuck <nothing>")]
        private async Task Chuck(CommandContext ctx)
        {
            string endpoint = "https://api.chucknorris.io/jokes/random";
            string json = await HTTP.Fetch(endpoint, ctx.Log);
            FactObject fact = JSON.Deserialize<FactObject>(json, ctx.Log);

            await ctx.EmbedReply.Good(ctx.Message, "Chuck Norris Fact", fact.value);
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
            builder.WithColor(ctx.EmbedReply.ColorGood);

            await ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        [Command(Name="crazy",Help="Make a sentence look crazy",Usage="crazy <sentence>")]
        private async Task Crazy(CommandContext ctx)
        {
            string content = ctx.Input;
            string result = "";
            Random rand = new Random();
            foreach(char letter in content)
            {
                string part = letter.ToString();
                if(rand.Next(1,100) >= 50)
                {
                    part = part.ToUpper();
                }
                else
                {
                    part = part.ToLower();
                }

                result += part;
            }

            await ctx.EmbedReply.Good(ctx.Message, "Crazy", result);
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
                    await ctx.EmbedReply.Good(ctx.Message, "GName", result.results[0].login.username);
                }
                else
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "GName", "Couldn't generate a new username");
                }
            }
            else
            {
                string result = "";
                result += CommandsData.Adjectives[rand.Next(0, CommandsData.Adjectives.Length - 1)];
                result += CommandsData.Nouns[rand.Next(0, CommandsData.Nouns.Length - 1)];
                if(rand.Next(1,100) < 30)
                {
                    result += rand.Next(0, 1000);
                }

                result = result.Replace("-", "").ToLower();

                await ctx.EmbedReply.Good(ctx.Message, "GName", result);
            }

        }

        [Command(Name="reverse",Help="Reverses a sentence",Usage="reverse <sentence>")]
        private async Task Reverse(CommandContext ctx)
        {
            string input = ctx.Input;
            if (ctx.HasArguments)
            {
                char[] chars = input.ToCharArray();
                Array.Reverse(chars);

                await ctx.EmbedReply.Good(ctx.Message, "Reverse", new string(chars));
            }
            else
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Reverse", "You must provide a sentence");
            }
        }

        [Command(Name="files",Help="?",Usage="?")]
        private async Task XFiles(CommandContext ctx)
        {
            if (ctx.Handler.Prefix == "x")
            {
                EmbedBuilder builder = new EmbedBuilder();
                builder.WithColor(ctx.EmbedReply.ColorGood);
                builder.WithAuthor(ctx.Message.Author);
                builder.WithFooter("XFiles");
                builder.WithImageUrl("https://i.imgur.com/kWK2BEW.png");

                await ctx.EmbedReply.Send(ctx.Message, builder.Build());
            }
        }

        public void Initialize(CommandHandler handler, BotLog log)
        {
            handler.LoadCommand(this.Describe);
            handler.LoadCommand(this.Letters);
            handler.LoadCommand(this.ASCII);
            handler.LoadCommand(this.EightBalls);
            handler.LoadCommand(this.Pick);
            handler.LoadCommand(this.Markov);
            handler.LoadCommand(this.Chuck);
            handler.LoadCommand(this.Meme);
            handler.LoadCommand(this.Crazy);
            handler.LoadCommand(this.GenName);
            handler.LoadCommand(this.Reverse);
            handler.LoadCommand(this.XFiles);

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
