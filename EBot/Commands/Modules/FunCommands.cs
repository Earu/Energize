using EBot.Utils;
using EBot.Logs;
using EBot.MachineLearning;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using EBot.Commands.Chuck;
using Discord;

namespace EBot.Commands.Modules
{
    class FunCommands : ICommandModule
    {

        private string Name = "Fun";

        private async Task ASCII(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Nope", "You didn't provide any word or sentence!");
            }
            else
            {
                string body = await HTTP.Fetch("http://artii.herokuapp.com/make?text=" + ctx.Arguments[0],ctx.Log);
                if (body.Length > 2000)
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "ASCII", "The word or sentence you provided is too long!");
                }
                else
                {
                    await ctx.Message.Channel.SendMessageAsync("```\n" + body + "\n```");
                }

            }
        }

        private async Task Describe(CommandContext ctx)
        {
            string[] adjs = CommandsData.Adjectives;
            string[] nouns = CommandsData.Nouns;
            SocketUser toping = ctx.Message.Author;
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]) && ctx.Message.MentionedUsers.Count > 0)
            {
                IReadOnlyList<SocketUser> users = ctx.Message.MentionedUsers as IReadOnlyList<SocketUser>;
                toping = users[0];
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
            await ctx.EmbedReply.Good(ctx.Message, "Description", toping.Mention + " is " + (isvowel ? "an" : "a") + " " + result);
        }

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
            await ctx.EmbedReply.Good(ctx.Message,"Letters", result);
        }

        private async Task EightBalls(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Nope", "You didn't provide any word or sentence!");
            }
            else
            {
                Random rand = new Random();
                string[] answers = CommandsData.HeightBallAnswers;
                string answer = answers[rand.Next(0, answers.Length - 1)];

                await ctx.EmbedReply.Good(ctx.Message,"8ball",answer);

            }
        }

        private async Task Pick(CommandContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                await ctx.EmbedReply.Danger(ctx.Message, "Nope", "You didn't provide any/enough word(s)!");
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

        private async Task Markov(CommandContext ctx)
        {
            if (!string.IsNullOrWhiteSpace(ctx.Arguments[0]))
            {
                string sentence = string.Join(",", ctx.Arguments);

                try
                {
                    string generated = await MarkovHandler.Generate(sentence);
                    await ctx.EmbedReply.Good(ctx.Message,"Markov", generated);
                }
                catch(Exception e)
                {
                    await ctx.EmbedReply.Danger(ctx.Message, "Markov", "Something went wrong:\n" + e.ToString());
                }
            }
            
        }

        private async Task Chuck(CommandContext ctx)
        {
            string endpoint = "https://api.chucknorris.io/jokes/random";
            string json = await HTTP.Fetch(endpoint, ctx.Log);
            FactObject fact = JSON.Deserialize<FactObject>(json, ctx.Log);

            await ctx.EmbedReply.Good(ctx.Message, "Chuck Norris Fact", fact.value);
        }

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

            await ctx.EmbedReply.Send(ctx.Message, builder.Build());
        }

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

            await ctx.EmbedReply.Good(ctx.Message, "Crazy", result);
        }

        public void Load(CommandHandler handler, BotLog log)
        {
            handler.LoadCommand("describe", this.Describe, "Random description of a user","describe \"@user\"",this.Name);
            handler.LoadCommand("letters", this.Letters, "Transforms a sentence into letter emojis","letters \"sentence\"", this.Name);
            handler.LoadCommand("ascii", this.ASCII, "Transforms a sentence into ascii art","ascii \"sentence\"", this.Name);
            handler.LoadCommand("8ball", this.EightBalls, "Yes or no answer to a question","8ball \"question\"", this.Name);
            handler.LoadCommand("pick", this.Pick, "Picks a choice among those given","pick \"choice1\",\"choice2\",\"choice3\",...", this.Name);
            handler.LoadCommand("m", this.Markov, "Generate a random sentence based on user input","m \"sentence\"",this.Name);
            handler.LoadCommand("chuck", this.Chuck, "Gets a random chuck norris fact", "chuck", this.Name);
            handler.LoadCommand("meme", this.Meme, "Gets a random meme", "meme", this.Name);
            handler.LoadCommand("crazy", this.Crazy, "Make a sentence look crazy", "crazy \"sentence\"", this.Name);

            log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload(CommandHandler handler, BotLog log)
        {
            handler.UnloadCommand("describe");
            handler.UnloadCommand("letters");
            handler.UnloadCommand("ascii");
            handler.UnloadCommand("8ball");
            handler.UnloadCommand("pick");
            handler.UnloadCommand("m");
            handler.UnloadCommand("chuck");
            handler.UnloadCommand("meme");
            handler.UnloadCommand("crazy");

            log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
