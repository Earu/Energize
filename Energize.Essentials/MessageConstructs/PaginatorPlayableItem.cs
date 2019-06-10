using System;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Essentials.MessageConstructs
{
    public class PaginatorPlayableItem
    {
        private readonly Func<Task<LavaTrack>> PlayCallback;

        public PaginatorPlayableItem(string displayUrl, Func<Task<LavaTrack>> playCallback)
        {
            this.DisplayURL = displayUrl;
            this.PlayCallback = playCallback;
        }

        public string DisplayURL { get; private set; }

        public async Task<LavaTrack> PlayAsync()
            => await this.PlayCallback();
    }
}
