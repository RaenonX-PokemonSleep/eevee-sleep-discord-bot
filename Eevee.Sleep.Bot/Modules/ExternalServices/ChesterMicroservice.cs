using Discord.WebSocket;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

namespace Eevee.Sleep.Bot.Modules.ExternalServices;

public static class ChesterMicroservice {
    public static async Task<string> FetchVersionNumber(DiscordSocketClient client) {
        var url = $"https://pks.yuh926.com/api/sleep/getInv?token={ConfigHelper.GetChesterApiToken()}";

        try {
            var response = await HttpModule.Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            return content;
        } catch (Exception e) {
            await client.SendMessageInAdminAlertChannel(
                message: "Failed to fetch in-game data version number!",
                embed: DiscordMessageMakerForError.MakeGeneralException(e)
            );

            throw new FetchVersionNumberFailedException(
                "Failed to fetch version number.",
                new Dictionary<string, string?> { { "exception", e.Message } }
            );
        }
    }
}