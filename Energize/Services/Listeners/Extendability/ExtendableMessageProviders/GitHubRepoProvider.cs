using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Services.Listeners.Extendability.ExtendableMessageProviders
{
    internal class GitHubRepo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("owner")]
        public GitHubUser Owner { get; set; }

        [JsonProperty("html_url")]
        public string Url { get; set; }

        [JsonProperty("fork")]
        public bool IsFork { get; set; }

        [JsonProperty("homepage")]
        public Uri Homepage { get; set; }

        [JsonProperty("stargazers_count")]
        public long StargazersCount { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("forks_count")]
        public long ForksCount { get; set; }

        [JsonProperty("archived")]
        public bool IsArchived { get; set; }

        [JsonProperty("disabled")]
        public bool IsDisabled { get; set; }

        [JsonProperty("open_issues_count")]
        public long OpenIssuesCount { get; set; }

        [JsonProperty("license")]
        public GitHubLicense License { get; set; }

        [JsonProperty("default_branch")]
        public string DefaultBranch { get; set; }
    }

    class GitHubLicense
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    class GitHubUser
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    internal class GitHubRepoProvider : BaseProvider
    {
        private readonly Logger Logger;

        public GitHubRepoProvider(Logger logger, string domain, string pattern) : base(domain, pattern)
        {
            this.Logger = logger;
        }

        public override async Task BuildEmbedsAsync(List<Embed> embeds, IUserMessage msg, SocketReaction reaction)
        {
            foreach(Match match in this.Matches(msg.Content))
            {
                string endpoint = $"https://api.github.com/repos/{match.Groups[1]}/{match.Groups[2]}";
                string json = await HttpHelper.GetAsync(endpoint, this.Logger);
                if (!JsonHelper.TryDeserialize(json, this.Logger, out GitHubRepo repo)) continue;
                if (repo?.Owner == null) continue;

                EmbedBuilder builder = new EmbedBuilder();
                builder
                    .WithColorType(EmbedColorType.Good)
                    .WithField("Owner", $"{repo.Owner.Login} ({repo.Owner.Type})")
                    .WithField("Stars", repo.StargazersCount)
                    .WithField("Forks", repo.ForksCount)
                    .WithField("Language", repo.Language)
                    .WithField("Homepage", repo.Homepage);

                if (repo.License != null)
                    builder.WithField("License", repo.License.Name);

                builder
                    .WithField("Default Branch", repo.DefaultBranch)
                    .WithField("Open Issues", repo.OpenIssuesCount)
                    .WithField("Fork", repo.IsFork)
                    .WithField("Archived", repo.IsArchived)
                    .WithField("Disabled", repo.IsDisabled)
                    .WithAuthorNickname(msg)
                    .WithLimitedTitle($"**{repo.Owner.Login}/{repo.Name}**")
                    .WithUrl(repo.Url);

                embeds.Add(builder.Build());
            }
        }
    }
}
