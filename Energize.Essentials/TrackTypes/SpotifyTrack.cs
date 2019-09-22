using System;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Essentials.TrackTypes
{
    /// <summary>
    ///     An AsyncLazyLoadTrack that uses SpotifyTrackInfo for the required metadata instead of the ones of a searched
    ///     LavaTrack (it does not a LavaTrack until play time)
    ///     <see cref="IAsyncLazyLoadTrack" />
    ///     <see cref="SpotifyTrackInfo" />
    /// </summary>
    public class SpotifyTrack : IAsyncLazyLoadTrack
    {
        private readonly Func<Task<ILavaTrack>> InnerTrackCallback;
        private ILavaTrack InnerTrack;

        public SpotifyTrackInfo SpotifyInfo { get; }

        /// <summary>
        ///     Initialize SpotifyTrack with an already searched LavaTrack
        /// </summary>
        /// <param name="spotifyInfo">Spotify information from the Spotify API</param>
        /// <param name="innerTrack">Searched LavaTrack</param>
        public SpotifyTrack(SpotifyTrackInfo spotifyInfo, ILavaTrack innerTrack)
        {
            this.SpotifyInfo = spotifyInfo;
            this.InnerTrack = innerTrack;
        }

        /// <summary>
        ///     Initialize SpotifyTrack with a callback that searches a LavaTrack (and returns it), for when it is needed
        /// </summary>
        /// <param name="spotifyInfo">Spotify information from the Spotify API</param>
        /// <param name="innerTrackCallback">Method reference to search for the LavaTrack</param>
        public SpotifyTrack(SpotifyTrackInfo spotifyInfo, Func<Task<ILavaTrack>> innerTrackCallback)
        {
            this.SpotifyInfo = spotifyInfo;
            this.InnerTrackCallback = innerTrackCallback;
        }

        public async Task<ILavaTrack> GetInnerTrackAsync()
        {
            if (this.InnerTrack != null)
            {
                return this.InnerTrack;
            }

            ILavaTrack track = await this.InnerTrackCallback();
            this.InnerTrack = track;
            return track;
        }

        public string Hash { get; set; }
        
        public string Id => this.SpotifyInfo.Id ?? this.InnerTrack?.Id;
        
        public bool IsSeekable => this.InnerTrack?.IsSeekable ?? true;

        public string Author => this.SpotifyInfo.Artists[0]
            .Name ?? this.InnerTrack?.Author ;

        public bool IsStream => this.InnerTrack?.IsStream ?? false;
        
        public TimeSpan Position
        {
            get =>
                this.InnerTrack?.Position ?? TimeSpan.Zero;
            set
            {
                if (this.InnerTrack != null)
                    this.InnerTrack.Position = value;
            }
            
        }

        public TimeSpan Length => this.InnerTrack?.Length ?? TimeSpan.Zero;

        public bool HasLength
        {
            get
            {
                TimeSpan len = this.InnerTrack?.Length ?? TimeSpan.MaxValue;
                return len > TimeSpan.Zero && len < TimeSpan.MaxValue;
            }
        }

        public string Title => this.SpotifyInfo.Name ?? this.InnerTrack?.Title;

        public Uri Uri => this.SpotifyInfo.Uri ?? this.InnerTrack?.Uri;

        public string Provider => "spotify";

        public void ResetPosition() => this.InnerTrack?.ResetPosition();
    }
}