using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energize.Essentials;
using Energize.Essentials.TrackTypes;
using Victoria;
using Victoria.Entities;

namespace Energize.Services.Listeners.Music.Spotify.Helpers
{
    internal class SpotifyTrackConverter
    {
        private readonly SpotifyConfig _config;
        private readonly LavaRestClient _lavaRest;

        public SpotifyTrackConverter(LavaRestClient lavaRest, SpotifyConfig config)
        {
            _lavaRest = lavaRest;
            _config = config;
        }

        /// <summary>
        ///     Converts a collection of SpotifyTrackInfos (from the Spotify API) to a collection of SpotifyTrack, in parallel
        ///     using Map-Reduce.
        ///     Note: This method returns a Task but it does not use asynchronous method invocation in the iterations, it can only
        ///     be awaited so it wont have to be waited to
        /// </summary>
        /// <param name="spotifyTrackInfos">Source SpotifyTrackInfo collection</param>
        /// <returns>Task of collection of converted SpotifyTracks</returns>
        /// <see cref="SpotifyTrackInfo" />
        /// <see cref="SpotifyTrack" />
        public async Task<IEnumerable<SpotifyTrack>> CreateSpotifyTracksAsync(
            IEnumerable<SpotifyTrackInfo> spotifyTrackInfos)
        {
            List<SpotifyTrackInfo> infosList = spotifyTrackInfos.ToList();

            // Partition (Map)
            int batches = _config.OperationsPerThread > 0 ? _config.OperationsPerThread : 1;
            ParallelQuery<IGrouping<int, KeyValuePair<int, SpotifyTrackInfo>>> parallelBatches = SplitToBatches(
                infosList,
                _config.ConcurrentPoolSize,
                batches);

            // Reduce (using conversion between SpotifyTrackInfo to SpotifyTrack
            return parallelBatches.SelectMany(
                dictionary => dictionary.Select(
                    pair => CreateSpotifyTrackAsync(pair.Value, false)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult()));
        }

        private static ParallelQuery<IGrouping<int, KeyValuePair<int, T>>> SplitToBatches<T>(
            IEnumerable<T> source,
            int poolSize,
            int batches)
        {
            ParallelQuery<T> asParallel = source.AsParallel()
                .AsOrdered();
            if (poolSize > 0)
            {
                asParallel = asParallel.WithDegreeOfParallelism(poolSize);
            }

            return asParallel.Select((x, i) => new KeyValuePair<int, T>(i, x))
                .GroupBy(x => x.Key / batches);
        }

        /// <summary>
        ///     Converts a SpotifyTrackInfo to a SpotifyTrack, decides by configuration or parameter if it should search for the
        ///     InnerTrack now, or LazyLoad (search for it later)
        /// </summary>
        /// <param name="trackInfo">Source SpotifyTrackInfo</param>
        /// <param name="lazyLoad">Optional parameter to specify if to lazy load or not, no matter what the configuration specifies</param>
        /// <returns>Converted SpotifyTrack</returns>
        public async Task<SpotifyTrack> CreateSpotifyTrackAsync(SpotifyTrackInfo trackInfo, bool? lazyLoad = null)
        {
            bool isLazyLoad = lazyLoad ?? _config.LazyLoad;
            return isLazyLoad
                ? new SpotifyTrack(trackInfo, () => SearchSpotify(trackInfo))
                : new SpotifyTrack(trackInfo, await SearchSpotify(trackInfo));
        }

        private async Task<ILavaTrack> SearchSpotify(SpotifyTrackInfo spotifyResult)
        {
            string artistName = $"{spotifyResult.Artists.FirstOrDefault()?.Name} - ";
            SearchResult searchResult = await _lavaRest.SearchYouTubeAsync($"{artistName}{spotifyResult.Name}");
            return searchResult.Tracks.FirstOrDefault();
        }
    }
}