using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Energize.Essentials.MessageConstructs
{
    public class Paginator<T> where T : class
    {
        private readonly List<T> Data;
        private readonly Func<T, string> DisplayCallback;
        private readonly Action<T, EmbedBuilder> DisplayEmbedCallback;
        private DateTime TimeToLive;
        private Embed Embed;

        public Paginator(ulong userid, IEnumerable<T> data, Func<T, string> displaycallback, Embed embed=null)
        {
            this.Data = new List<T>(data);
            this.DisplayCallback = displaycallback;
            this.DisplayEmbedCallback = null;
            this.CurrentIndex = 0;
            this.TimeToLive = DateTime.Now.AddMinutes(5);
            this.Embed = embed;
            this.UserId = userid;
        }

        public Paginator(ulong userid, IEnumerable<T> data, Action<T, EmbedBuilder> displaycallback, Embed embed=null)
        {
            this.Data = new List<T>(data);
            this.DisplayCallback = null;
            this.DisplayEmbedCallback = displaycallback;
            this.CurrentIndex = 0;
            this.TimeToLive = DateTime.Now.AddMinutes(5);
            this.Embed = embed;
            this.UserId = userid;
        }

        public IUserMessage Message { get; set; }
        public bool IsExpired => this.TimeToLive < DateTime.Now; 
        public int CurrentIndex { get; private set; }
        public T CurrentValue => this.Data[this.CurrentIndex];
        public ulong UserId { get; }

        public async Task Next()
        {
            int temp = this.CurrentIndex + 1;
            if (temp >= this.Data.Count)
                temp = 0;
            this.CurrentIndex = temp;
            await this.Update();
        }

        public async Task Previous()
        {
            int temp = this.CurrentIndex - 1;
            if (temp < 0)
                temp = this.Data.Count - 1;
            this.CurrentIndex = temp;
            await this.Update();
        }

        public Paginator<object> ToObject()
        {
            if (this.DisplayCallback != null)
            {
                return new Paginator<object>(this.UserId, this.Data, obj => this.DisplayCallback((T)obj))
                {
                    Message = this.Message,
                    Embed = this.Embed,
                };
            }
            else
            {
                return new Paginator<object>(this.UserId, this.Data, (obj, builder) => this.DisplayEmbedCallback((T)obj, builder))
                {
                    Message = this.Message,
                    Embed = this.Embed,
                };
            }
        }

        private void Refresh()
            => this.TimeToLive = DateTime.Now.AddMinutes(5);

        private async Task Update()
        {  
            if (this.Message == null) return;
            string display = this.DisplayCallback?.Invoke(this.Data[this.CurrentIndex]);
            await this.Message.ModifyAsync(prop =>
            {
                if (this.Embed != null)
                {
                    Embed oldEmbed = this.Embed;
                    EmbedBuilder builder = new EmbedBuilder();
                    builder
                        .WithLimitedTitle(oldEmbed.Title)
                        .WithLimitedDescription(display);

                    if (oldEmbed.Color.HasValue)
                        builder.WithColor(oldEmbed.Color.Value);

                    if (oldEmbed.Timestamp.HasValue)
                        builder.WithTimestamp(oldEmbed.Timestamp.Value);

                    if(oldEmbed.Author.HasValue)
                    {
                        EmbedAuthor oldAuthor = oldEmbed.Author.Value;
                        EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder();
                        authorBuilder
                            .WithIconUrl(oldAuthor.IconUrl)
                            .WithName(oldAuthor.Name)
                            .WithUrl(oldAuthor.Url);
                        builder.WithAuthor(authorBuilder);
                    }

                    if (oldEmbed.Footer.HasValue)
                    {
                        EmbedFooter oldFooter = oldEmbed.Footer.Value;
                        EmbedFooterBuilder footerBuilder = new EmbedFooterBuilder();
                        footerBuilder
                            .WithText(oldFooter.Text)
                            .WithIconUrl(oldFooter.IconUrl);
                        builder.WithFooter(footerBuilder);
                    }

                    this.DisplayEmbedCallback?.Invoke(this.Data[this.CurrentIndex], builder);

                    this.Embed = builder.Build();
                    prop.Embed = this.Embed;
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
