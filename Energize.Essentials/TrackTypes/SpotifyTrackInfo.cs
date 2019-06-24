using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyAPI.Web.Models;

namespace Energize.Essentials.TrackTypes
{
    /// <summary>
    /// Model class that wraps the different track results from Spotify API
    /// <seealso cref="SimpleTrack" />
    /// <seealso cref="FullTrack" />
    /// </summary>
    public class SpotifyTrackInfo
    {
        public string[] Images { get; }

        public List<SimpleArtist> Artists { get; }
        
        public List<string> AvailableMarkets { get; }
        
        public int DiscNumber { get; }
        
        public TimeSpan Duration { get; }
        
        public bool Explicit { get; }
        
        public Dictionary<string, Uri> ExternUrls { get; }
        
        public string Id { get; }
        
        public string Name { get; }
        
        public string PreviewUrl { get; }
        
        public int TrackNumber { get; }
        
        public string Type { get; }
        
        public Uri Uri { get; }
        
        public SpotifyTrackInfo(SimpleTrack simpleTrack)
        {
            this.Artists = simpleTrack.Artists;
            this.AvailableMarkets = simpleTrack.AvailableMarkets;
            this.DiscNumber = simpleTrack.DiscNumber;
            this.Duration = TimeSpan.FromMilliseconds(simpleTrack.DurationMs);
            this.Explicit = simpleTrack.Explicit;
            this.ExternUrls = new Dictionary<string, Uri>(
                simpleTrack.ExternUrls
                    .Select(pair => new KeyValuePair<string, Uri>(pair.Key, new Uri(pair.Value)))); // Convert value to URI
            this.Id = simpleTrack.Id;
            this.Name = simpleTrack.Name;
            this.PreviewUrl = simpleTrack.PreviewUrl;
            this.TrackNumber = simpleTrack.TrackNumber;
            this.Type = simpleTrack.Type;
            try
            {
                this.Uri = this.ExternUrls["spotify"];
            }
            catch (KeyNotFoundException) // Should never fail
            {
                // Convert Spotify URI to a track link (wont link track to playlist or album if it was linked)
                this.Uri = new Uri($"https://open.spotify.com/track/{this.Id}");
            }
        }

        public SpotifyTrackInfo(FullTrack fullTrack)
        {
            this.Artists = fullTrack.Artists;
            this.AvailableMarkets = fullTrack.AvailableMarkets;
            this.DiscNumber = fullTrack.DiscNumber;
            this.Duration = TimeSpan.FromMilliseconds(fullTrack.DurationMs);
            this.Explicit = fullTrack.Explicit;
            try
            {
                this.ExternUrls = new Dictionary<string, Uri>(
                    fullTrack.ExternUrls
                        .Select(pair => new KeyValuePair<string, Uri>(pair.Key, new Uri(pair.Value))));
                    // Convert value to URI
            }
            catch
            {
                this.ExternUrls = new Dictionary<string, Uri>();
            }

            this.Id = fullTrack.Id;
            this.Name = fullTrack.Name;
            this.PreviewUrl = fullTrack.PreviewUrl;
            this.TrackNumber = fullTrack.TrackNumber;
            this.Type = fullTrack.Type;
            try
            {
                this.Uri = this.ExternUrls["spotify"];
            }
            catch (KeyNotFoundException) // Should never fail
            {
                // Convert Spotify URI to a track link (wont link track to playlist or album if it was linked)
                this.Uri = new Uri($"https://open.spotify.com/track/{this.Id}");
            }

            this.Images = fullTrack.Album?.Images?.Select(image => image.Url)
                .ToArray();
        }
    }
}