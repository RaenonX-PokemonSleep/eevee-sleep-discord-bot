using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Utils;
using IResult = Discord.Interactions.IResult;

namespace Eevee.Sleep.Bot.Handlers;

public class InteractionHandler {
    private readonly DiscordSocketClient _client;

    private readonly InteractionService _handler;

    private readonly IServiceProvider _services;

    public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services) {
        _client = client;
        _handler = handler;
        _services = services;
    }

    public async Task InitializeAsync() {
        _client.Ready += ReadyAsync;
        _handler.Log += OnLogHandler.OnLogAsync;

        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.InteractionCreated += OnInteractionCreated;
        _client.ModalSubmitted += OnModalSubmitted;

        _handler.SlashCommandExecuted += OnSlashCommandExecuted;
    }

    private async Task ReadyAsync() {
        await _handler.RegisterCommandsGloballyAsync();
    }

    private static Task OnInteractionExecuted(IInteractionContext context, IResult result) {
        if (result.IsSuccess || result.Error == InteractionCommandError.UnknownCommand) {
            // Ignoring unknown command since it's possible to happen for follow up modal interactions
            return Task.CompletedTask;
        }

        return context.Interaction.RespondAsync(
            "Bot error occurred!",
            embed: DiscordMessageMaker.MakeError(result),
            ephemeral: true
        );
    }

    private async Task OnInteractionCreated(SocketInteraction interaction) {
        try {
            var context = new SocketInteractionContext(_client, interaction);

            var result = await _handler.ExecuteCommandAsync(context, _services);

            await OnInteractionExecuted(context, result);
        } catch {
            // If Slash Command execution fails, most likely the original interaction acknowledgement will persist.
            // It is a good idea to delete the original response,
            // or at least let the user know that something went wrong during the command execution.
            if (interaction.Type is InteractionType.ApplicationCommand) {
                await interaction
                    .GetOriginalResponseAsync()
                    .ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        }
    }

    private static Task OnSlashCommandExecuted(SlashCommandInfo command, IInteractionContext context, IResult result) {
        return OnInteractionExecuted(context, result);
    }

    private async Task OnModalSubmitted(SocketModal modal) {
        var modalId = modal.Data.CustomId.ToModalId();
        var components = modal.Data.Components.ToList();

        switch (modalId) {
            case ModalId.EmoteStealer: {
                var emoteName = components
                    .First(x => x.CustomId.ToModalFieldId() == ModalFieldId.EmoteName).Value;

                if (emoteName is null) {
                    throw new ArgumentException("Emote name should not be empty! (Emote name not found)");
                }

                var emoteLink = components
                    .First(x => x.CustomId.ToModalFieldId() == ModalFieldId.EmoteLink).Value;

                if (emoteLink is null) {
                    throw new ArgumentException("Emote link should not be empty! (Emote link not found)");
                }

                var guildId = modal.GuildId;

                if (guildId is null) {
                    throw new ArgumentException("Guild ID is null from modal!");
                }

                using var client = new HttpClient();

                var emoteHttpResponse = await client.GetAsync(emoteLink);
                var stream = await emoteHttpResponse.Content.ReadAsStreamAsync();

                await _client.GetGuild(guildId.Value).CreateEmoteAsync(
                    emoteName,
                    new Image(stream)
                );
                await modal.RespondAsync($"Emote stolen as **{emoteName}**!");
                break;
            }
            case null:
                return;
            default:
                throw new ArgumentException($"Unhandled modal ID: {modalId}");
        }
    }
}