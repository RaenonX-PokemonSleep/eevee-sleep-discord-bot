using System.Net;
using System.Text.Json;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Exceptions;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models.ChesterMicroService;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;

namespace Eevee.Sleep.Bot.Modules.ExternalServices;

public static class ChesterMicroservice {
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(10);

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] Hostnames = [
        "https://stable-pks.yuh926.com",
        "http://nokotools.sytes.net",
    ];

    public static async Task<ChesterCurrentVersion> FetchCurrentVersion(DiscordSocketClient client) {
        Exception? lastException = null;

        foreach (var hostname in Hostnames) {
            var url = $"{hostname}/api/sleep/getOfficalCurrentVersion?token={ConfigHelper.GetChesterApiToken()}";

            try {
                return await TryFetchFromUrl(url, client);
            } catch (OfficialServerInMaintenanceException) {
                var message = await client.SendMessageInAdminAlertChannel(
                    message: "Official Server is in Maintenance!"
                );
                await message.AutoDeleteAfterSeconds(10);

                throw;
            } catch (Exception e) when (e is JsonException or HttpRequestException) {
                lastException = e;
            }
        }

        switch (lastException) {
            case JsonException:
                await client.SendMessageInAdminAlertChannel(
                    message: "Failed to parse game data version JSON response!",
                    embed: DiscordMessageMakerForError.MakeGeneralException(lastException)
                );

                throw new FetchVersionNumberFailedException(
                    $"Failed to fetch version number ({lastException.GetType().Name}).",
                    new Dictionary<string, string?> { { "exception", lastException.Message } }
                );
            case not null:
                await client.SendMessageInAdminAlertChannel(
                    message: $"Failed to fetch game data version number! Retrying in {RetryInterval} secs...",
                    embed: DiscordMessageMakerForError.MakeGeneralException(lastException)
                );

                // Keep running until the attempt limit is exceeded
                await Task.Delay(RetryInterval);
                return await FetchCurrentVersion(client);
        }

        throw new FetchVersionNumberFailedException(
            "All hosts were not able to fetch version number.",
            new Dictionary<string, string?> { { "hostNames", Hostnames.MergeToSameLine() } }
        );
    }

    private static async Task<ChesterCurrentVersion> TryFetchFromUrl(string url, DiscordSocketClient client) {
        var response = await HttpModule.Client.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable) {
            throw new OfficialServerInMaintenanceException();
        }

        response.EnsureSuccessStatusCode();

        var newCurrentVersion = await JsonSerializer.DeserializeAsync<ChesterCurrentVersion>(
            await response.Content.ReadAsStreamAsync(),
            JsonOptions
        );

        if (newCurrentVersion is null) {
            await client.SendMessageInAdminAlertChannel(
                message: "Failed to fetch current version! The parsed JSON is null."
            );

            throw new FetchVersionNumberFailedException(
                "Failed to fetch version number (JSON being null).",
                new Dictionary<string, string?>()
            );
        }

        var originalCurrentVersion = await ChesterCurrentVersionController.UpdateAndGetOriginal(newCurrentVersion);

        if (newCurrentVersion.IsVersionUpdated(originalCurrentVersion)) {
            await client.SendMessageInAdminAlertChannel(
                message: "<@503484431437398016>",
                embed: DiscordMessageMakerForCurrentVersion.MakeCurrentVersionUpdated(
                    originalCurrentVersion,
                    newCurrentVersion
                )
            );
        }

        return newCurrentVersion;
    }
}