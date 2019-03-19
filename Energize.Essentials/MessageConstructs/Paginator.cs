using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Toolkit.MessageConstructs
{
    public class Paginator<T> where T : class
    {
        private readonly List<T> _Data;
        private readonly Func<T, string> _DisplayCallback;
        private readonly Action<T, EmbedBuilder> _DisplayEmbedCallback;
        private DateTime _TimeToLive;
        private Embed _Embed;

        public Paginator(ulong userid, IEnumerable<T> data, Func<T, string> displaycallback, Embed embed=null)
        {
            this._Data = new List<T>(data);
            this._DisplayCallback = displaycallback;
            this._DisplayEmbedCallback = null;
            this.CurrentIndex = 0;
            this._TimeToLive = DateTime.Now.AddMinutes(5);
            this._Embed = embed;
            this.UserID = userid;
        }

        public Paginator(ulong userid, IEnumerable<T> data, Action<T, EmbedBuilder> displaycallback, Embed embed=null)
        {
            this._Data = new List<T>(data);
            this._DisplayCallback = null;
            this._DisplayEmbedCallback = displaycallback;
            this.CurrentIndex = 0;
            this._TimeToLive = DateTime.Now.AddMinutes(5);
            this._Embed = embed;
            this.UserID = userid;
        }

        public IUserMessage Message { get; set; }
        public bool IsExpired { get => this._TimeToLive > DateTime.Now; }
        public int CurrentIndex { get; private set; }
        public ulong UserID { get; private set; }

        public async Task Next()
        {
            int temp = this.CurrentIndex + 1;
            if (temp >= this._Data.Count)
                temp = 0;
            this.CurrentIndex = temp;
            await this.Update();
        }

        public async Task Previous()
        {
            int temp = this.CurrentIndex - 1;
            if (temp < 0)
                temp = this._Data.Count - 1;
            this.CurrentIndex = temp;
            await this.Update();
        }

        public Paginator<object> ToObject()
        {
            if (this._DisplayCallback != null)
            {
                return new Paginator<object>(this.UserID, this._Data, obj => this._DisplayCallback((T)obj))
                {
                    Message = this.Message,
                    _Embed = this._Embed,
                };
            }
            else
            {
                return new Paginator<object>(this.UserID, this._Data, (obj, builder) => this._DisplayEmbedCallback((T)obj, builder))
                {
                    Message = this.Message,
                    _Embed = this._Embed,
                };
            }
        }

        private void Refresh()
            => this._TimeToLive = DateTime.Now.AddMinutes(5);

        private async Task Update()
        {  
            if (this.Message == null) return;
            string display = this._DisplayCallback?.Invoke(this._Data[this.CurrentIndex]);
            await this.Message.ModifyAsync(prop =>
            {
                if (this._Embed != null)
                {
                    Embed oldembed = this._Embed;
                    EmbedBuilder builder = new EmbedBuilder();
                    builder
                        .WithColor(oldembed.Color.Value)
                        .WithTitle(oldembed.Title)
                        .WithDescription(display);

                    if (oldembed.Timestamp.HasValue)
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

                    this._DisplayEmbedCallback?.Invoke(this._Data[this.CurrentIndex], builder);

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
