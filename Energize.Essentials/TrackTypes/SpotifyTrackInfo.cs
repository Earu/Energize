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
            Artists = simpleTrack.Artists;
            AvailableMarkets = simpleTrack.AvailableMarkets;
            DiscNumber = simpleTrack.DiscNumber;
            Duration = TimeSpan.FromMilliseconds(simpleTrack.DurationMs);
            Explicit = simpleTrack.Explicit;
            ExternUrls = new Dictionary<string, Uri>(
                simpleTrack.ExternUrls
                    .Select(pair => new KeyValuePair<string, Uri>(pair.Key, new Uri(pair.Value)))); // Convert value to URI
            Id = simpleTrack.Id;
            Name = simpleTrack.Name;
            PreviewUrl = simpleTrack.PreviewUrl;
            TrackNumber = simpleTrack.TrackNumber;
            Type = simpleTrack.Type;
            try
            {
                Uri = ExternUrls["spotify"];
            }
            catch (KeyNotFoundException) // Should never fail
            {
                // Convert Spotify URI to a track link (wont link track to playlist or album if it was linked)
                Uri = new Uri($"https://open.spotify.com/track/{Id}");
            }
        }

        public SpotifyTrackInfo(FullTrack fullTrack)
        {
            Artists = fullTrack.Artists;
            AvailableMarkets = fullTrack.AvailableMarkets;
            DiscNumber = fullTrack.DiscNumber;
            Duration = TimeSpan.FromMilliseconds(fullTrack.DurationMs);
            Explicit = fullTrack.Explicit;
            try
            {
                ExternUrls = new Dictionary<string, Uri>(
                    fullTrack.ExternUrls
                        .Select(pair => new KeyValuePair<string, Uri>(pair.Key, new Uri(pair.Value))));
                    // Convert value to URI
            }
            catch
            {
                ExternUrls = new Dictionary<string, Uri>();
            }
            
            Id = fullTrack.Id;
            Name = fullTrack.Name;
            PreviewUrl = fullTrack.PreviewUrl;
            TrackNumber = fullTrack.TrackNumber;
            Type = fullTrack.Type;
            try
            {
                Uri = ExternUrls["spotify"];
            }
            catch (KeyNotFoundException) // Should never fail
            {
                // Convert Spotify URI to a track link (wont link track to playlist or album if it was linked)
                Uri = new Uri($"https://open.spotify.com/track/{Id}");
            }
            Images = fullTrack.Album?.Images?.Select(image => image.Url)
                .ToArray();
        }
    }
}