using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers.EventHandlers;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using IResult = Discord.Interactions.IResult;

namespace Eevee.Sleep.Bot.Handlers;

public class InteractionHandler(
    DiscordSocketClient client,
    InteractionService handler,
    IServiceProvider services,
    IHostEnvironment env
) {
    public async Task InitializeAsync() {
        client.Ready += ReadyAsync;
        handler.Log += message => OnLogHandler.OnLogAsync(client, message);

        await handler.AddModulesAsync(Assembly.GetEntryAssembly(), services);

        client.InteractionCreated += OnInteractionCreated;
        client.ButtonExecuted += OnButtonClicked;
        client.ModalSubmitted += OnModalSubmitted;
        client.GuildMemberUpdated += (cached, updated) =>
            GuildMemberUpdatedEventHandler.OnEvent(client, env, cached, updated);
        client.UserLeft += (_, user) =>
            GuildMemberLeftEventHandler.OnEvent(client, user);

        handler.SlashCommandExecuted += OnSlashCommandExecuted;
    }

    private async Task ReadyAsync() {
        await handler.RegisterCommandsGloballyAsync();
    }

    private static Task OnInteractionExecuted(IInteractionContext context, IResult result) {
        if (result.IsSuccess || result.Error == InteractionCommandError.UnknownCommand) {
            // Ignoring unknown command since it's possible to happen for follow up modal interactions
            return Task.CompletedTask;
        }

        return context.Interaction.RespondAsync(
            "Bot error occurred!",
            embed: DiscordMessageMakerForError.MakeError(result),
            ephemeral: true
        );
    }

    private async Task OnInteractionCreated(SocketInteraction interaction) {
        try {
            var context = new SocketInteractionContext(client, interaction);

            var result = await handler.ExecuteCommandAsync(context, services);

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

                using var client1 = new HttpClient();

                var emoteHttpResponse = await client1.GetAsync(emoteLink);
                var stream = await emoteHttpResponse.Content.ReadAsStreamAsync();

                await client.GetGuild(guildId.Value).CreateEmoteAsync(
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

    private static async Task OnButtonClicked(SocketMessageComponent component) {
        var info = ButtonInteractionInfoSerializer.Deserialize(component.Data.CustomId);
        var buttonId = info?.ButtonId;

        if (info is null || component.User is not SocketGuildUser user) {
            throw new ArgumentException("Button interaction info or user is null!");
        }

        switch (buttonId) {
            case ButtonId.RoleChanger:
                await ButtonClickedHandler.DisplayRoleButtonClicked(info, user);

                await component.RespondAsync(
                    StringHelper.MergeLines([
                        $"{MentionUtils.MentionRole(info.CustomId)} has been selected to display.",
                        "Ownership of the other tracked roles on Discord are removed. " +
                        "You can add them back using `/role add` or `/role display`."
                    ]),
                    ephemeral: true
                );
                break;
            case ButtonId.RoleAdder:
                await ButtonClickedHandler.AddRoleButtonClicked(info, user);
                await component.RespondAsync(
                    $"{MentionUtils.MentionRole(info.CustomId)} has been added.",
                    ephemeral: true
                );
                break;
            case ButtonId.RoleRemover:
                await ButtonClickedHandler.RemoveRoleButtonClicked(info, user);

                await component.RespondAsync(
                    StringHelper.MergeLines([
                        $"{MentionUtils.MentionRole(info.CustomId)} has been removed.",
                        "The actual ownership of the role is unaffected. " +
                        "You can add them back using `/role add` or `/role display`."
                    ]),
                    ephemeral: true
                );
                break;
            default:
                throw new ArgumentException($"Unhandled button ID: {buttonId}.");
        }
    }
}