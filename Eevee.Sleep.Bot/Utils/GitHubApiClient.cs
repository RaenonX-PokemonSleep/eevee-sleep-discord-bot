using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Eevee.Sleep.Bot.Utils;

public static class GitHubApiClient {
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient() {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ConfigHelper.GetGithubToken());
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EeveeSleepBot");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    public static async Task<int> CreateIssueAsync(
        string title,
        string body,
        IEnumerable<string> labels
    ) {
        var repoSlug = ConfigHelper.GetGithubRepoSlug();
        var payload = new {
            title,
            body,
            labels = labels.ToArray(),
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await Client.PostAsync(
            $"https://api.github.com/repos/{repoSlug}/issues",
            content
        );

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GitHubIssueResponse>(json)
                     ?? throw new InvalidOperationException("GitHub API returned null response.");

        return result.Number;
    }

    private sealed class GitHubIssueResponse {
        [JsonPropertyName("number")]
        public int Number { get; init; }
    }
}
