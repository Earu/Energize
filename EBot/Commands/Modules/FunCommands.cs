using EBot.Utils;
using EBot.Logs;
using EBot.MachineLearning;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using EBot.Commands.Chuck;
using Discord;

namespace EBot.Commands.Modules
{
    [CommandModule(Name="Fun")]
    class FunCommands : CommandModule,ICommandModule
    {
        [Command(Name="ascii",Help="Makes a text/sentence ascii art",Usage="ascii <sentence>")]
        private async Task ASCII(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                ctx.EmbedReply.Danger(ctx.Message, "ASCII", "You didn't provide any word or sentence!");
            }
            else
            {
                string body = await HTTP.Fetch("http://artii.herokuapp.com/make?text=" + ctx.Arguments[0],ctx.Log);
                if (body.Length > 2000)
                {
                    ctx.EmbedReply.Danger(ctx.Message, "ASCII", "The word or sentence you provided is too long!");
                }
                else
                {
                    ctx.EmbedReply.Good(ctx.Message,"ASCII","```\n" + body + "\n```");
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
                    result += " of ";
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

            bool isvowel = false;
            string[] vowels = CommandsData.Vowels;
            for (int i = 0; i < vowels.Length; i++)
            {
                if (result.StartsWith(vowels[i]))
                {
                    isvowel = true;
                    break;
                }
            }
            ctx.EmbedReply.Good(ctx.Message, "Description", toping.Mention + " is " + (isvowel ? "an" : "a") + " " + result);
        }

        [Command(Name="letters",Help="Transforms a sentence into letter emotes",Usage="letters <sentence>")]
        private async Task Letters(CommandContext ctx)
        {
            string input = ctx.Arguments[0];
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
            ctx.EmbedReply.Good(ctx.Message,"Letters", result);
        }

        [Command(Name="8ball",Help="Gives a negative or positive answer to a question",Usage="8ball <question>")]
        private async Task EightBalls(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                ctx.EmbedReply.Danger(ctx.Message, "8ball", "You didn't provide any word or sentence!");
            }
            else
            {
                Random rand = new Random();
                string[] answers = CommandsData.HeightBallAnswers;
                string answer = answers[rand.Next(0, answers.Length - 1)];

                ctx.EmbedReply.Good(ctx.Message,"8ball",answer);
            }
        }

        [Command(Name="pick",Help="Picks a choice among those given",Usage="pick <choice>,<choice>,<choice|nothing>,...")]
        private async Task Pick(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                ctx.EmbedReply.Danger(ctx.Message, "Pick", "You didn't provide any/enough word(s)!");
            }
            else
            {
                string[] answers = CommandsData.PickAnswers;
                Random rand = new Random();
                string choice = ctx.Arguments[rand.Next(0, ctx.Arguments.Count - 1)].Trim();
                string answer = answers[rand.Next(0, answers.Length - 1)].Replace("<answer>", choice);

                ctx.EmbedReply.Good(ctx.Message,"Pick", answer);
            }
        }

        [Command(Name="m",Help="Generates a random sentence based on input",Usage="m <input|nothing>")]
        private async Task Markov(CommandContext ctx)
        {
            string sentence = string.Join(",", ctx.Arguments);
            try
            {
                string generated = MarkovHandler.Generate(sentence);
                ctx.EmbedReply.Good(ctx.Message,"Markov", generated);
            }
            catch(Exception e)
            {
                ctx.EmbedReply.Danger(ctx.Message, "Markov", "Something went wrong:\n" + e.ToString());
            }
        }

        [Command(Name="chuck",Help="Gets a random chuck norris fact",Usage="chuck <nothing>")]
        private async Task Chuck(CommandContext ctx)
        {
            string endpoint = "https://api.chucknorris.io/jokes/random";
            string json = await HTTP.Fetch(endpoint, ctx.Log);
            FactObject fact = JSON.Deserialize<FactObject>(json, ctx.Log);

            ctx.EmbedReply.Good(ctx.Message, "Chuck Norris Fact", fact.value);
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

            ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

        [Command(Name="crazy",Help="Make a sentence look crazy",Usage="crazy <sentence>")]
        private async Task Crazy(CommandContext ctx)
        {
            string content = string.Join(',', ctx.Arguments);
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

            ctx.EmbedReply.Good(ctx.Message, "Crazy", result);
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

            log.Nice("Module", ConsoleColor.Green, "Initialized " + this.GetModuleName());
        }
    }
}
