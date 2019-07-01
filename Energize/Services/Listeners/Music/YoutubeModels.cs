using Newtonsoft.Json;
using Energize.Interfaces.Services.Listeners;
using System.ComponentModel.DataAnnotations;

namespace Energize.Services.Listeners.Music
{
    public class YoutubeRelatedVideos
    {
        [JsonProperty("items")]
        public YoutubeVideo[] Videos { get; set; }
    }

    public class YoutubeVideo
    {
        [JsonProperty("id")]
        public YoutubeVideoId Id { get; set; }
    }

    public class YoutubeVideoId : IYoutubeVideoID
    {
        [JsonIgnore]
        [Key]
        public int Identity { get; set; }

        [JsonProperty("videoId")]
        public string VideoID { get; set; }
    }
}
