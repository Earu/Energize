using System;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    public class SpotifyTrack : IAsyncLazyLoadTrack, IAsyncLavaTrack
    {
        private LavaTrack _innerTrack;
        private readonly Func<Task<LavaTrack>> _innerTrackCallback;
        
        public SpotifyTrackInfo SpotifyInfo { get; }


        //public string[] Images => SpotifyInfo.Images ?? new [] {(await GetInnerTrackAsync()).FetchThumbnailAsync().ConfigureAwait(false).GetAwaiter().GetResult()};
        public async Task<string> GetIdAsync() => (await GetInnerTrackAsync()).Id;
        public async Task<bool> GetIsSeekableAsync() => (await GetInnerTrackAsync()).IsSeekable;
        public async Task<string> GetAuthorAsync() => SpotifyInfo.Artists[0].Name ?? (await GetInnerTrackAsync()).Author;
        public Task<bool> GetIsStreamAsync() => Task.FromResult(_innerTrack?.IsStream ?? false);
        public async Task<TimeSpan> GetPositionAsync() => (await GetInnerTrackAsync()).Position;
        public async Task<TimeSpan> GetLengthAsync() => (await GetInnerTrackAsync()).Length;
        public async Task<string> GetTitleAsync() => SpotifyInfo.Name ?? (await GetInnerTrackAsync()).Title;
        public async Task<Uri> GetUriAsync() => SpotifyInfo.Uri ?? (await GetInnerTrackAsync()).Uri;
        public Task<string> GetProviderAsync() => Task.FromResult("spotify");


        public SpotifyTrack(SpotifyTrackInfo spotifyInfo, LavaTrack innerTrack)
        {
            SpotifyInfo = spotifyInfo;
            _innerTrack = innerTrack;
        }

        public SpotifyTrack(SpotifyTrackInfo spotifyInfo, Func<Task<LavaTrack>> innerTrackCallback)
        {
            SpotifyInfo = spotifyInfo;
            _innerTrackCallback = innerTrackCallback;
        }

        public async Task<LavaTrack> GetInnerTrackAsync()
        {
            if (_innerTrack != null)
            {
                return _innerTrack;
            }
            LavaTrack track = await _innerTrackCallback();
            _innerTrack = track;
            return track;
        }

        public async Task ResetPositionAsync() => (await GetInnerTrackAsync()).ResetPosition();
    }
}