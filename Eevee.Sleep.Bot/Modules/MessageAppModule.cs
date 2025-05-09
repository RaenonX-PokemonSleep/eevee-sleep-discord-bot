﻿using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Eevee.Sleep.Bot.Enums;
using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Modules;

[RequireUserPermission(GuildPermission.Administrator)]
public partial class MessageAppModule : InteractionModuleBase<SocketInteractionContext> {
    [GeneratedRegex(@"<a?:(\w+):(?:\w+)>")]
    private static partial Regex SingleEmojiRegex();

    [MessageCommand("Steal Emoji")]
    [UsedImplicitly]
    public async Task StealEmojiAsync(IMessage message) {
        if (message is not IUserMessage userMessage) {
            await RespondAsync("Can't steal emoji from non-user messages!");
            return;
        }

        var content = userMessage.Content;

        if (!content.StartsWith('<') || !content.EndsWith('>')) {
            await RespondAsync("Single emoji message is required!");
            return;
        }

        var contentRegexMatch = SingleEmojiRegex().Match(content);

        if (!contentRegexMatch.Success) {
            await RespondAsync("Emoji regex matching failed!");
            return;
        }

        var emoteName = contentRegexMatch.Groups[1].Value;
        var emote = Emote.Parse(content);

        await RespondWithModalAsync(
            new ModalBuilder()
                .WithTitle("Emote Stealer")
                .WithCustomId(ModalId.EmoteStealer.ToString())
                .AddTextInput("Emote Name", ModalFieldId.EmoteName.ToString(), value: emoteName)
                .AddTextInput("Emote Link", ModalFieldId.EmoteLink.ToString(), value: emote.Url)
                .Build()
        );
    }

    [MessageCommand("Steal Sticker")]
    [UsedImplicitly]
    public async Task StealStickerAsync(IMessage message) {
        if (message is not IUserMessage userMessage) {
            await RespondAsync("Can't steal sticker from non-user messages!");
            return;
        }

        var sticker = userMessage.Stickers.FirstOrDefault();
        if (sticker is null) {
            await RespondAsync("Message doesn't contain any stickers!");
            return;
        }

        if (sticker is not ISticker castSticker) {
            await RespondAsync("Sticker failed to parse!");
            return;
        }

        await RespondWithModalAsync(
            new ModalBuilder()
                .WithTitle("Sticker Stealer")
                .WithCustomId(ModalId.StickerStealer.ToString())
                .AddTextInput("Sticker Name", ModalFieldId.StickerName.ToString(), value: castSticker.Name)
                .AddTextInput(
                    "Sticker Description",
                    ModalFieldId.StickerDescription.ToString(),
                    value: string.IsNullOrEmpty(castSticker.Description) ? castSticker.Name : castSticker.Description,
                    required: false
                )
                .AddTextInput("Sticker Link", ModalFieldId.StickerLink.ToString(), value: castSticker.GetStickerUrl())
                .Build()
        );
    }
}