namespace Energize.Services.Listeners.Music.Spotify.Models
{
    internal class CollectionOptions
    {
        public int Total { get; }
        
        public int StartIndex { get; set; }
        
        public int MaxResults { get; set; }

        public CollectionOptions(int total, int startIndex, int maxResults)
        {
            this.Total = total;
            this.StartIndex = startIndex;
            this.MaxResults = maxResults;
        }
    }
}