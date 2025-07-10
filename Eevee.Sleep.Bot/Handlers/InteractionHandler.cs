using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Enums;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Handlers.EventHandlers;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Models.Pagination;
using Eevee.Sleep.Bot.Utils;
using Eevee.Sleep.Bot.Utils.DiscordMessageMaker;
using IResult = Discord.Interactions.IResult;

namespace Eevee.Sleep.Bot.Handlers;

public class InteractionHandler(
    ILogger<InteractionHandler> logger,
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
        if (env.IsDevelopment()) {
            var guildId = ConfigHelper.GetDiscordWorkingGuild();
            logger.LogInformation("Registering commands to Guild ID #{GuildId}", guildId);
            await handler.RegisterCommandsToGuildAsync(guildId);
        }

        if (env.IsProduction()) {
            logger.LogInformation("Globally registering commands");
            await handler.RegisterCommandsGloballyAsync();
        }
    }

    private static Task OnInteractionExecuted(IInteractionContext context, IResult result) {
        // Ignoring unknown command since it's possible to happen for follow-up modal interactions
        if (result.IsSuccess || result.Error == InteractionCommandError.UnknownCommand) {
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

                using var httpClient = new HttpClient();

                var emoteHttpResponse = await httpClient.GetAsync(emoteLink);
                var stream = await emoteHttpResponse.Content.ReadAsStreamAsync();

                await client.GetGuild(guildId.Value).CreateEmoteAsync(
                    emoteName,
                    new Image(stream)
                );
                await modal.RespondAsync($"Emote stolen as **{emoteName}**!");
                break;
            }
            case ModalId.StickerStealer: {
                var stickerName = components
                    .First(x => x.CustomId.ToModalFieldId() == ModalFieldId.StickerName).Value;
                if (stickerName is null) {
                    throw new ArgumentException("Sticker name should not be empty! (Sticker name not found)");
                }

                var stickerDescription = components
                    .First(x => x.CustomId.ToModalFieldId() == ModalFieldId.StickerDescription).Value;
                if (stickerDescription is null) {
                    throw new ArgumentException(
                        "Sticker description should not be empty! (Sticker description not found)"
                    );
                }

                var stickerLink = components
                    .First(x => x.CustomId.ToModalFieldId() == ModalFieldId.StickerLink).Value;
                if (stickerLink is null) {
                    throw new ArgumentException("Sticker link should not be empty! (Sticker link not found)");
                }

                var guildId = modal.GuildId;
                if (guildId is null) {
                    throw new ArgumentException("Guild ID is null from modal!");
                }

                using var httpClient = new HttpClient();

                var stickerHttpResponse = await httpClient.GetAsync(stickerLink);
                var stream = await stickerHttpResponse.Content.ReadAsStreamAsync();

                await client.GetGuild(guildId.Value).CreateStickerAsync(
                    name: stickerName,
                    image: new Image(stream),
                    tags: [stickerName],
                    description: stickerDescription
                );
                await modal.RespondAsync($"Sticker stolen as **{stickerName}**!");
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
        var discordPaginationState = DiscordPaginationContext<TrackedRoleModel>.GetState(
            component.User.Id.ToString()
        );

        if (discordPaginationState is null) {
            await component.RespondAsync(
                "The command has expired. Please run the command again.",
                ephemeral: true
            );
            return;
        }

        if (info is null || component.User is not SocketGuildUser user) {
            throw new ArgumentException("Button interaction info or user is null!");
        }

        switch (buttonId) {
            case ButtonId.RoleChanger:
                await ButtonClickedHandler.DisplayRoleButtonClicked(info, user);

                await component.RespondAsync(
                    StringHelper.MergeLines(
                        $"{MentionUtils.MentionRole(info.CustomId)} has been selected to display.",
                        "Ownership of the other tracked roles on Discord are removed. " +
                        "You can add them back using `/role add` or `/role display`."
                    ),
                    ephemeral: true
                );

                if (discordPaginationState is not null) {
                    DiscordPaginationContext<TrackedRoleModel>.RemoveState(component.User.Id.ToString());
                }

                break;
            case ButtonId.RoleAdder:
                await ButtonClickedHandler.AddRoleButtonClicked(info, user);
                await component.RespondAsync(
                    $"{MentionUtils.MentionRole(info.CustomId)} has been added.",
                    ephemeral: true
                );

                if (discordPaginationState is not null) {
                    DiscordPaginationContext<TrackedRoleModel>.RemoveState(component.User.Id.ToString());
                }

                break;
            case ButtonId.RoleRemover:
                await ButtonClickedHandler.RemoveRoleButtonClicked(info, user);

                await component.RespondAsync(
                    StringHelper.MergeLines(
                        $"{MentionUtils.MentionRole(info.CustomId)} has been removed.",
                        "The actual ownership of the role is unaffected. " +
                        "You can add them back using `/role add` or `/role display`."
                    ),
                    ephemeral: true
                );

                if (discordPaginationState is not null) {
                    DiscordPaginationContext<TrackedRoleModel>.RemoveState(component.User.Id.ToString());
                }

                break;
            case ButtonId.PageNext:
                if (discordPaginationState.CurrentPage >= discordPaginationState.TotalPages) {
                    await component.RespondAsync(
                        "You are already on the last page.",
                        ephemeral: true
                    );
                    return;
                }

                await component.RespondAsync(
                    ephemeral: true,
                    components: DiscordMessageMakerForRoleChange.MakeRoleSelectButton(
                        discordPaginationState.Collection.ToArray(),
                        discordPaginationState.ActionButtonId,
                        discordPaginationState.CurrentPage + 1,
                        GlobalConst.DiscordPaginationParams.ItemsPerPage
                    )
                );
                DiscordPaginationContext<TrackedRoleModel>.GotoNextPage(component.User.Id.ToString());

                break;
            case ButtonId.PagePrevious:
                if (discordPaginationState.CurrentPage <= 1) {
                    await component.RespondAsync(
                        "You are already on the first page.",
                        ephemeral: true
                    );
                    return;
                }

                await component.RespondAsync(
                    ephemeral: true,
                    components: DiscordMessageMakerForRoleChange.MakeRoleSelectButton(
                        discordPaginationState.Collection.ToArray(),
                        discordPaginationState.ActionButtonId,
                        discordPaginationState.CurrentPage - 1,
                        GlobalConst.DiscordPaginationParams.ItemsPerPage
                    )
                );
                DiscordPaginationContext<TrackedRoleModel>.GotoPreviousPage(component.User.Id.ToString());

                break;
            default:
                throw new ArgumentException($"Unhandled button ID: {buttonId}.");
        }
    }
}