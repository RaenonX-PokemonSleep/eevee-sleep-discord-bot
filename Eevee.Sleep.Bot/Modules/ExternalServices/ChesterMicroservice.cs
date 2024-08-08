using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Utils;

namespace Eevee.Sleep.Bot.Modules.ExternalServices;

public static class ChesterMicroservice {
    public static async Task<string> FetchVersionNumber() {
        var url = $"https://pks.yuh926.com/api/sleep/getInv?token={ConfigHelper.GetChesterApiToken()}";

        try {
            var response = await HttpModule.Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return content;
        } catch (Exception e) {
            throw new FetchVersionNumberFailedException(
                "Failed to fetch version number.",
                new Dictionary<string, string?> { { "exception", e.Message } }
            );
        }
    }
}