using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Toolkit
{
    public class Paginator<T> where T : class
    {
        private readonly List<T> _Data;
        private readonly Func<T, string> _DisplayCallback;
        private int _CurrentIndex;
        private DateTime _TimeToLive;
        private Embed _Embed;

        public Paginator(IEnumerable<T> data, Func<T, string> display, Embed embed=null)
        {
            this._Data = new List<T>(data);
            this._DisplayCallback = display;
            this._CurrentIndex = 0;
            this._TimeToLive = DateTime.Now.AddMinutes(5);
            this._Embed = embed;
        }

        public IUserMessage Message { get; set; }
        public bool IsExpired { get => this._TimeToLive > DateTime.Now; }
        public int CurrentIndex { get => this._CurrentIndex; }

        public async Task Next()
        {
            int temp = this._CurrentIndex + 1;
            if (temp >= this._Data.Count)
                temp = 0;
            this._CurrentIndex = temp;
            await this.Update();
        }

        public async Task Previous()
        {
            int temp = this._CurrentIndex - 1;
            if (temp < 0)
                temp = this._Data.Count - 1;
            this._CurrentIndex = temp;
            await this.Update();
        }

        public Paginator<object> ToObject()
        {
            return new Paginator<object>(this._Data, obj => this._DisplayCallback((T)obj))
            {
                Message = this.Message,
                _Embed = this._Embed,
            };
        }

        private void Refresh()
            => this._TimeToLive = DateTime.Now.AddMinutes(5);

        private async Task Update()
        {
            string display = this._DisplayCallback(this._Data[this._CurrentIndex]);
            if (this.Message == null) return;
            await this.Message.ModifyAsync(prop =>
            {
                if (this._Embed != null)
                {
                    Embed oldembed = this._Embed;
                    EmbedBuilder builder = new EmbedBuilder();
                    builder
                        .WithColor(oldembed.Color.Value)
                        .WithDescription(display)
                        .WithTitle(oldembed.Title);

                    if(oldembed.Timestamp.HasValue)
                        builder.WithTimestamp(oldembed.Timestamp.Value);

                    if(oldembed.Author.HasValue)
                    {
                        EmbedAuthor oldauthor = oldembed.Author.Value;
                        EmbedAuthorBuilder authorbuilder = new EmbedAuthorBuilder();
                        authorbuilder
                            .WithIconUrl(oldauthor.IconUrl)
                            .WithName(oldauthor.Name)
                            .WithUrl(oldauthor.Url);
                        builder.WithAuthor(authorbuilder);
                    }

                    if (oldembed.Footer.HasValue)
                    {
                        EmbedFooter oldfooter = oldembed.Footer.Value;
                        EmbedFooterBuilder footerbuilder = new EmbedFooterBuilder();
                        footerbuilder
                            .WithText(oldfooter.Text)
                            .WithIconUrl(oldfooter.IconUrl);
                        builder.WithFooter(footerbuilder);
                    }

                    this._Embed = builder.Build();
                    prop.Embed = this._Embed;
                }
                else
                {
                    prop.Content = display;
                }
            });

            this.Refresh();
        }
    }
}
