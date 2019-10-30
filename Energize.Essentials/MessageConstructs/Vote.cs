using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace Energize.Essentials.MessageConstructs
{
    public class Vote
    {
        private readonly IUser Author;
        private readonly string Description;
        private readonly Dictionary<string, int> Choices;
        private readonly List<string> ChoiceIndexes;
        private readonly Dictionary<ulong, int> VoterIds;
        private readonly Logger Logger;

        private bool IsFinished;
        private int TotalVotes;

        public event Action<string> VoteFinished;

        public Vote(IUser author, string desc, List<string> choices, Logger logger)
        {
            this.TotalVotes = 0;
            this.Author = author;
            this.Description = desc;
            this.Choices = new Dictionary<string, int>();

            foreach (string choice in choices)
                this.Choices.Add(choice, 0);

            this.ChoiceIndexes = choices;
            this.VoterIds = new Dictionary<ulong, int>();
            this.Logger = logger;
            this.IsFinished = false;

            this.UpdateEmbed();

            Timer timer = new Timer(300000) // 5mins 
            {
                AutoReset = false,
                Enabled = true,
            };

            timer.Elapsed += async (sender, args) =>
            {
                await this.EndVote();
                try
                {
                    this.VoteFinished?.Invoke(this.GetResult());
                }
                catch (Exception ex)
                {
                    this.Logger.Danger(ex);
                }
            };
        }

        public IUserMessage Message { get; set; }
        public Embed VoteEmbed { get; private set; }
        public int ChoiceCount => this.ChoiceIndexes.Count;

        private string GetResult()
        {
            string res = string.Empty;
            int min = 0;
            foreach((string choice, int votes) in this.Choices)
            {
                if (votes <= min) continue;
                
                min = votes;
                res = choice;
            }

            return res;
        }

        private bool IsValidIndex(int choiceindex)
            => choiceindex >= 0 && choiceindex < this.ChoiceIndexes.Count;

        private void UpdateEmbed()
        {
            EmbedBuilder builder = new EmbedBuilder();
            if (this.IsFinished)
                builder.WithColorType(EmbedColorType.Special);
            builder.AddField("Vote", this.Description);
            int i = 1;
            foreach ((string choice, int votes) in this.Choices)
            {
                double perc = this.TotalVotes == 0 ? 0.0 : votes / (double)this.TotalVotes * 100.0;
                string plural = votes > 1 ? "s" : string.Empty;
                builder.AddField($"{i++}. {choice}", $"{perc}% ({votes} vote{plural})", true);
            }
               
            builder.WithAuthor(this.Author);
            builder.WithFooter(this.IsFinished ? "vote is over" : "valid for 5 minutes");

            this.VoteEmbed = builder.Build();
        }

        private async Task EndVote()
        {
            this.IsFinished = true;
            await this.Update();
        }

        private async Task Update()
        {
            try
            {
                if (this.Message == null) return;
                this.UpdateEmbed();
                await this.Message.ModifyAsync(prop => prop.Embed = this.VoteEmbed);
            } 
            catch (Exception ex)
            {
                this.Logger.Danger(ex);
            }
        }

        public async Task AddVote(IUser voter, int choiceindex)
        {
            if (voter.IsBot || voter.IsWebhook || this.IsFinished) return;
            if (this.VoterIds.ContainsKey(voter.Id)) return;
            if (this.IsValidIndex(choiceindex))
            {
                string choice = this.ChoiceIndexes[choiceindex];
                this.Choices[choice]++;
                this.VoterIds.Add(voter.Id, choiceindex);
                this.TotalVotes++;
                await this.Update();
            }
        }

        public async Task RemoveVote(IUser voter, int choiceindex)
        {
            if (voter.IsBot || voter.IsWebhook || this.IsFinished) return;
            if (!this.VoterIds.ContainsKey(voter.Id)) return;
            if (this.IsValidIndex(choiceindex) && this.VoterIds[voter.Id] == choiceindex)
            {
                string choice = this.ChoiceIndexes[choiceindex];
                this.Choices[choice]--;
                this.VoterIds.Remove(voter.Id);
                this.TotalVotes--;
                await this.Update();
            }
        }
    }
}
