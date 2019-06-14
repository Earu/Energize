using System;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Victoria.Entities;

namespace Energize.Essentials.MessageConstructs
{
    public class PaginatorPlayableItem
    {
        private readonly Func<Task<ITrack>> PlayCallback;

        public PaginatorPlayableItem(string displayUrl, Func<Task<ITrack>> playCallback)
        {
            this.DisplayURL = displayUrl;
            this.PlayCallback = playCallback;
        }

        public string DisplayURL { get; private set; }

        public async Task<ITrack> PlayAsync()
            => await this.PlayCallback();
    }
}
