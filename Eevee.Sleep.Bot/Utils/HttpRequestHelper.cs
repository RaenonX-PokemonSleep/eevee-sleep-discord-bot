namespace Eevee.Sleep.Bot.Utils;

public static class HttpRequestHelper {
    private static readonly HttpClient Client = new();

    public static async Task<string> GenerateDiscordActivationLink(
        string discordId,
        IEnumerable<ulong> roleIds
    ) {
        var response = await Client.PostAsync(
            ConfigHelper.GetInternalApiGenerateActivation(),
            new FormUrlEncodedContent(
                new Dictionary<string, string> {
                    { "token", ConfigHelper.GetInternalOutboundApiToken() },
                    { "roleIds", string.Join(",", roleIds) },
                    { "discordId", discordId },
                }
            )
        );

        return await response.Content.ReadAsStringAsync();
    }
}