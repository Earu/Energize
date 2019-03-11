using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace Energize.Toolkit.MessageConstructs
{
    public class Vote
    {
        private readonly IUser _Author;
        private readonly string _Description;
        private readonly Dictionary<string, int> _Choices;
        private readonly List<string> _ChoiceIndexes;
        private readonly Dictionary<ulong, int> _VoterIds;
        private readonly Timer _Timer;
        private readonly DateTime _EndTime;

        private bool _IsFinished;
        private int _TotalVotes;

        public event Action<string> VoteFinished;

        public Vote(IUser author, string desc, List<string> choices)
        {
            this._TotalVotes = 0;
            this._Author = author;
            this._Description = desc;
            this._Choices = new Dictionary<string, int>();

            foreach (string choice in choices)
                this._Choices.Add(choice, 0);

            this._ChoiceIndexes = choices;
            this._VoterIds = new Dictionary<ulong, int>();
            this._IsFinished = false;
            this._EndTime = DateTime.Now.AddMinutes(5.0);

            this.UpdateEmbed();

            this._Timer = new Timer(300000) // 5mins 
            {
                AutoReset = false,
                Enabled = true,
            };

            this._Timer.Elapsed += async (sender, args) =>
            {
                await this.EndVote();
                this.VoteFinished?.Invoke(this.GetResult());
            };
        }

        public IUserMessage Message { get; set; }
        public Embed VoteEmbed { get; private set; }
        public int ChoiceCount { get => this._ChoiceIndexes.Count; }

        private string GetResult()
        {
            string res = string.Empty;
            int min = 0;
            foreach(KeyValuePair<string, int> choice in this._Choices)
            {
                if(choice.Value > min)
                {
                    min = choice.Value;
                    res = choice.Key;
                }
            }

            return res;
        }

        private bool IsValidIndex(int choiceindex)
            => choiceindex >= 0 && choiceindex < this._ChoiceIndexes.Count;

        private void UpdateEmbed()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(this._IsFinished ? new Color(0, 175, 220) : new Color(30, 30, 30));
            builder.AddField("Vote", this._Description, false);
            int i = 1;
            foreach (KeyValuePair<string, int> kv in this._Choices)
            {
                double perc = this._TotalVotes == 0 ? 0.0 : kv.Value / (double)this._TotalVotes * 100.0;
                string plural = kv.Value > 1 ? "s" : string.Empty;
                builder.AddField($"{i++}. {kv.Key}", $"{perc}% ({kv.Value} vote{plural})", true);
            }
               
            builder.WithAuthor(this._Author);
            builder.WithFooter(this._IsFinished ? "Expired" : "Valid for 5 minutes");

            this.VoteEmbed = builder.Build();
        }

        private async Task EndVote()
        {
            this._IsFinished = true;
            await this.Update();
        }

        private async Task Update()
        {
            if (this.Message == null) return;
            this.UpdateEmbed();
            await this.Message.ModifyAsync(prop => prop.Embed = this.VoteEmbed);
        }

        public async Task AddVote(IUser voter, int choiceindex)
        {
            if (this._IsFinished) return;
            if (this._VoterIds.ContainsKey(voter.Id)) return;
            if (this.IsValidIndex(choiceindex))
            {
                string choice = this._ChoiceIndexes[choiceindex];
                this._Choices[choice]++;
                this._VoterIds.Add(voter.Id, choiceindex);
                this._TotalVotes++;
                await this.Update();
            }
        }

        public async Task RemoveVote(IUser voter, int choiceindex)
        {
            if(this._IsFinished) return;
            if (!this._VoterIds.ContainsKey(voter.Id)) return;
            if (this.IsValidIndex(choiceindex) && this._VoterIds[voter.Id] == choiceindex)
            {
                string choice = this._ChoiceIndexes[choiceindex];
                this._Choices[choice]--;
                this._VoterIds.Remove(voter.Id);
                this._TotalVotes--;
                await this.Update();
            }
        }
    }
}
