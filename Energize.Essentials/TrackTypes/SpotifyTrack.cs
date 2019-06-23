using System;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    /// <summary>
    ///     An AsyncLazyLoadTrack that uses SpotifyTrackInfo for the required metadata instead of the ones of a searched
    ///     LavaTrack (it does not a LavaTrack until play time)
    ///     At the moment this track does partially depend on a searched LavaTrack for metadata, but this will improve in the
    ///     future
    ///     <see cref="IAsyncLazyLoadTrack" />
    ///     <see cref="SpotifyTrackInfo" />
    /// </summary>
    public class SpotifyTrack : IAsyncLazyLoadTrack
    {
        private readonly Func<Task<ILavaTrack>> _innerTrackCallback;
        private ILavaTrack _innerTrack;

        public SpotifyTrackInfo SpotifyInfo { get; }

        /// <summary>
        ///     Initialize SpotifyTrack with an already searched LavaTrack
        /// </summary>
        /// <param name="spotifyInfo">Spotify information from the Spotify API</param>
        /// <param name="innerTrack">Searched LavaTrack</param>
        public SpotifyTrack(SpotifyTrackInfo spotifyInfo, LavaTrack innerTrack)
        {
            this.SpotifyInfo = spotifyInfo;
            this._innerTrack = innerTrack;
        }

        /// <summary>
        ///     Initialize SpotifyTrack with a callback that searches a LavaTrack (and returns it), for when it is needed
        /// </summary>
        /// <param name="spotifyInfo">Spotify information from the Spotify API</param>
        /// <param name="innerTrackCallback">Method reference to search for the LavaTrack</param>
        public SpotifyTrack(SpotifyTrackInfo spotifyInfo, Func<Task<ILavaTrack>> innerTrackCallback)
        {
            this.SpotifyInfo = spotifyInfo;
            this._innerTrackCallback = innerTrackCallback;
        }

        public async Task<ILavaTrack> GetInnerTrackAsync()
        {
            if (this._innerTrack != null)
            {
                return this._innerTrack;
            }

            ILavaTrack track = await this._innerTrackCallback();
            this._innerTrack = track;
            return track;
        }


        //public string[] Images => SpotifyInfo.Images ?? new [] {(await GetInnerTrackAsync()).FetchThumbnailAsync().ConfigureAwait(false).GetAwaiter().GetResult()};
//
//        public async Task<T> ToLavaTrackAsync<T>() where T : LavaTrack
//        {
//            async void ResetPositionCallback() => await this.ResetPositionAsync();
//
//            var sourceTrack = new EasyLavaTrack(
//                await this.GetIdAsync(),
//                await this.GetIsSeekableAsync(),
//                await this.GetAuthorAsync(),
//                await this.GetIsStreamAsync(),
//                await this.GetPositionAsync(),
//                await this.GetLengthAsync(),
//                await this.GetTitleAsync(),
//                await this.GetUriAsync(),
//                await this.GetProviderAsync(),
//                ResetPositionCallback);
//            LavaTrack thisInnerTrack = await this.GetInnerTrackAsync();
//            var outerTrack = new EasyLavaTrack(
//                thisInnerTrack.Id,
//                thisInnerTrack.IsSeekable,
//                thisInnerTrack.Author,
//                thisInnerTrack.IsStream,
//                thisInnerTrack.Position,
//                thisInnerTrack.Length,
//                thisInnerTrack.Title,
//                thisInnerTrack.Uri,
//                thisInnerTrack.Provider,
//                sourceTrack,
//                ResetPositionCallback);
//            return outerTrack as T;
//        }
        
        public string Hash { get; set; }
        
        public string Id => this.SpotifyInfo.Id ?? this._innerTrack?.Id;
        
        public bool IsSeekable => this._innerTrack?.IsSeekable ?? true;

        public string Author => this.SpotifyInfo.Artists[0]
            .Name ?? this._innerTrack?.Author ;

        public bool IsStream => this._innerTrack?.IsStream ?? false;
        
        public TimeSpan Position
        {
            get =>
                this._innerTrack?.Position ?? TimeSpan.Zero;
            set
            {
                if (this._innerTrack != null)
                {
                    this._innerTrack.Position = value;
                }
            }
            
        }

        public TimeSpan Length => this._innerTrack?.Length ?? TimeSpan.Zero;

        public string Title => this.SpotifyInfo.Name ?? this._innerTrack?.Title;

        public Uri Uri => this.SpotifyInfo.Uri ?? this._innerTrack?.Uri;

        public string Provider => "spotify";

        public void ResetPosition() => this._innerTrack?.ResetPosition();
    }
}