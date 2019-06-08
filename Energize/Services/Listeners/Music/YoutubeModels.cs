using Newtonsoft.Json;
using Energize.Interfaces.Services.Listeners;
using System.ComponentModel.DataAnnotations;

namespace Energize.Services.Listeners.Music
{
    public partial class YoutubeRelatedVideos
    {
        [JsonProperty("items")]
        public YoutubeVideo[] Videos { get; set; }
    }

    public partial class YoutubeVideo
    {
        [JsonProperty("id")]
        public YoutubeVideoID Id { get; set; }
    }

    public partial class YoutubeVideoID : IYoutubeVideoID
    {
        [JsonIgnore]
        [Key]
        public int Identity { get; set; }

        [JsonProperty("videoId")]
        public string VideoID { get; set; }
    }
}
