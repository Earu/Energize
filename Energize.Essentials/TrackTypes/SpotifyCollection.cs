using System;
using System.Collections.Generic;
using System.Linq;
using SpotifyAPI.Web.Models;

namespace Energize.Essentials.TrackTypes
{
    public class SpotifyCollection
    {
        public IEnumerable<SpotifyTrack> Items { get; }
        
        public string[] Images { get; }
        
        public List<string> Authors { get; }
        
        public Dictionary<string, Uri> ExternUrls { get; }
        
        public string Id { get; set; }
        
        public string Name { get; }

        public string Type { get; }
        
        public Uri Uri { get; }

        public SpotifyCollection(FullPlaylist playlist, IEnumerable<SpotifyTrack> items)
        {
            this.Items = items;
            
            this.Images = playlist.Images.Select(image => image.Url).ToArray();
            this.Authors = new List<string>
            {
                playlist.Owner.DisplayName
            };
            this.ExternUrls = new Dictionary<string, Uri>(
                playlist.ExternalUrls
                           .Select(pair => new KeyValuePair<string, Uri>(pair.Key, new Uri(pair.Value)))); // Convert value to URI
            this.Id = playlist.Id;
            this.Name = playlist.Name;
            this.Type = playlist.Type;
            try
            {
                this.Uri = this.ExternUrls["spotify"];
            }
            catch (KeyNotFoundException) // Should never fail
            {
                // Convert Spotify URI to a track link (wont link track to playlist or album if it was linked)
                this.Uri = new Uri(playlist.Uri);
            }
        }
    }
}