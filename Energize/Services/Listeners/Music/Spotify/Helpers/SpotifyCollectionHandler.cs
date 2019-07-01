using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials.TrackTypes;
using Energize.Services.Listeners.Music.Spotify.Models;

namespace Energize.Services.Listeners.Music.Spotify.Helpers
{
    internal class SpotifyCollectionHandler
    {
        public static async Task<IEnumerable<SpotifyTrackInfo>> GetAllSpotifyInfosAsync(IEnumerable<SpotifyTrackInfo> sourceTracks, string id, CollectionOptions options, 
            Func<string, CollectionOptions, Task<IEnumerable<SpotifyTrackInfo>>> collectionGetter)
        {
            List<SpotifyTrackInfo> tracksList = sourceTracks.ToList();
            bool isMaxResultsValid = options.MaxResults == 0 || options.MaxResults > 99;
            if (!isMaxResultsValid || options.Total <= 100)
            {
                return tracksList;
            }

            CollectionOptions lambdaOptions = options;
            if (options.MaxResults == 0)
            {
                lambdaOptions.MaxResults = lambdaOptions.Total;
            }            
            for (int i = tracksList.Count; i < lambdaOptions.MaxResults; i++)
            {
                lambdaOptions.StartIndex += i;
                if (lambdaOptions.MaxResults > 0)
                {
                    if (i > lambdaOptions.MaxResults)
                    {
                        break;
                    }
                }

                tracksList.AddRange(await collectionGetter.Invoke(id, lambdaOptions));
                i += tracksList.Count;
            }

            return tracksList;
        }
    }
}