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

        public Dictionary<string, Uri> Authors { get; }

        public Dictionary<string, Uri> ExternUrls { get; }

        public string Id { get; set; }

        public string Name { get; }

        public string Type { get; }

        public Uri Uri { get; }

        public SpotifyCollection(FullPlaylist playlist, IEnumerable<SpotifyTrack> items)
        {
            Items = items;

            Images = playlist.Images.Select(image => image.Url)
                .ToArray();

            Uri ownerUri;
            PublicProfile playlistOwner = playlist.Owner;
            try
            {
                ownerUri = new Uri(playlistOwner.ExternalUrls["spotify"]);
            }
            catch (KeyNotFoundException) // Should never fail
            {
                ownerUri = new Uri(playlistOwner.Uri);
            }

            Authors = new Dictionary<string, Uri>
            {
                {playlistOwner.DisplayName, ownerUri}
            };
            ExternUrls = new Dictionary<string, Uri>(
                playlist.ExternalUrls.Select(
                    pair => new KeyValuePair<string, Uri>(pair.Key, new Uri(pair.Value)))); // Convert value to URI
            Id = playlist.Id;
            Name = playlist.Name;
            Type = playlist.Type;
            try
            {
                Uri = ExternUrls["spotify"];
            }
            catch (KeyNotFoundException) // Should never fail
            {
                // Convert Spotify URI to a track link (wont link track to playlist or album if it was linked)
                Uri = new Uri(playlist.Uri);
            }
        }

        public SpotifyCollection(FullAlbum album, IEnumerable<SpotifyTrack> items)
        {
            Items = items;

            Images = album.Images.Select(image => image.Url)
                .ToArray();
            Authors = album.Artists.ToDictionary(
                artist => artist.Name,
                artist =>
                {
                    try
                    {
                        return new Uri(artist.ExternalUrls["spotify"]);
                    }
                    catch (KeyNotFoundException) // Should never fail
                    {
                        return new Uri(artist.Uri);
                    }
                });
            ExternUrls = new Dictionary<string, Uri>(
                album.ExternalUrls.Select(
                    pair => new KeyValuePair<string, Uri>(pair.Key, new Uri(pair.Value)))); // Convert value to URI
            Id = album.Id;
            Name = album.Name;
            Type = album.Type;
            try
            {
                Uri = ExternUrls["spotify"];
            }
            catch (KeyNotFoundException) // Should never fail
            {
                // Convert Spotify URI to a track link (wont link track to playlist or album if it was linked)
                Uri = new Uri(album.Uri);
            }
        }
    }
}