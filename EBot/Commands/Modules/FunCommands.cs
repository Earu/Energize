using DSharpPlus.Entities;
using EBot.Commands.Utils;
using EBot.Logs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EBot.Commands.Modules
{
    class FunCommands
    {

        private string Name = "Fun";
        private CommandsHandler Handler;
        private BotLog Log;

        public void Setup(CommandsHandler handler, BotLog log)
        {
            this.Handler = handler;
            this.Log = log;
        }

        private async void ASCII(CommandReplyEmbed embedrep ,DiscordMessage msg, List<string> args)
        {

            if (string.IsNullOrWhiteSpace(args[0]))
            {
                embedrep.Danger(msg, "Nope", "You didn't provide any word or sentence!");
            }
            else
            {
                string body = await HTTP.Fetch("http://artii.herokuapp.com/make?text=" + args[0],Log);
                if (body.Length > 2000)
                {
                    embedrep.Danger(msg, "ASCII", "The word or sentence you provided is too long!");
                }
                else
                {
                    await msg.RespondAsync("```\n" + body + "\n```");
                }

            }
        }

        private void Describe(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string[] adjs = CommandsData.Adjectives;
            string[] nouns = CommandsData.Nouns;
            DiscordUser toping = msg.Author;
            if (!string.IsNullOrWhiteSpace(args[0]) && msg.MentionedUsers[0] != null)
            {
                toping = msg.MentionedUsers[0];
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
            embedrep.Good(msg, "Description", Social.Action.PingUser(toping) + " is " + (isvowel ? "an" : "a") + " " + result);
        }

        private void Letters(CommandReplyEmbed embedrep, DiscordMessage msg, List<string> args)
        {
            string input = args[0];
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
            embedrep.Good(msg, msg.Author.Username, result);
        }

        private void HeightBalls(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            if (string.IsNullOrWhiteSpace(args[0]))
            {
                embedrep.Danger(msg, "Nope", "You didn't provide any word or sentence!");
            }
            else
            {
                Random rand = new Random();
                string[] answers = CommandsData.HeightBallAnswers;
                string answer = answers[rand.Next(0, answers.Length - 1)];

                embedrep.Good(msg, msg.Author.Username,answer);

            }
        }

        private void Pick(CommandReplyEmbed embedrep,DiscordMessage msg,List<string> args)
        {
            if (string.IsNullOrWhiteSpace(args[0]))
            {
                embedrep.Danger(msg, "Nope", "You didn't provide any/enough word(s)!");
            }
            else
            {
                string[] answers = CommandsData.PickAnswers;
                Random rand = new Random();
                string choice = args[rand.Next(0, args.Count - 1)].Trim();
                string answer = answers[rand.Next(0, answers.Length - 1)].Replace("<answer>", choice);

                embedrep.Good(msg, msg.Author.Username, answer);
            }
        }

        public void Load()
        {
            this.Handler.LoadCommand("describe", this.Describe, "Describes a person!");
            this.Handler.LoadCommand("letters", this.Letters, "Use discord emojis to display a sentence");
            this.Handler.LoadCommand("ascii", this.ASCII, "Display ascii art of the given word/sentence");
            this.Handler.LoadCommand("8ball", this.HeightBalls, "Fast positive or negative answer to a question asked");
            this.Handler.LoadCommand("pick", this.Pick, "Chooses for you among the choices provided");

            this.Log.Nice("Module", ConsoleColor.Green, "Loaded " + this.Name);
        }

        public void Unload()
        {
            this.Handler.UnloadCommand("describe");
            this.Handler.UnloadCommand("letters");
            this.Handler.UnloadCommand("ascii");
            this.Handler.UnloadCommand("8ball");
            this.Handler.UnloadCommand("pick");

            this.Log.Nice("Module", ConsoleColor.Green, "Unloaded " + this.Name);
        }
    }
}
