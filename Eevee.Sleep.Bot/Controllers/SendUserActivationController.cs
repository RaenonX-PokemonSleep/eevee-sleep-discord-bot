using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Eevee.Sleep.Bot.Controllers;

[ApiController]
[Route("/send-activation")]
public class SendUserActivationController(
    ILogger<SubscribedUserController> logger,
    DiscordSocketClient client,
    IHostEnvironment env
) : ControllerBase {
    [HttpPost(Name = "SendActivationMessage")]
    public OkResult Post(DiscordActivationMessageModel[] activationMessages) {
        logger.LogInformation(
            "Received {Count} activation messages to send",
            activationMessages.Length
        );

        Task.WhenAll(activationMessages.Select(async x => {
            var userId = Convert.ToUInt64(x.UserId);
            var user = await client.GetUserAsync(userId);

            if (user is null) {
                logger.LogInformation(
                    "Failed to send activation message to {UserId} - User not found",
                    x.UserId
                );
                await client.SendMessageInAdminAlertChannel(
                    $"Failed to send activation message to {MentionUtils.MentionUser(userId)} - User not found"
                );
                return;
            }

            logger.LogInformation(
                "Activation message sent to {UserId} with link {ActivationLink} - requested via API",
                x.UserId,
                x.Link
            );
            // Prevent accidentally sending messages to the users
            if (env.IsProduction()) {
                await user.SendMessageAsync(
                    x.Link,
                    embeds: DiscordMessageMaker.MakeActivationNote()
                );
            }

            await client.SendMessageInAdminAlertChannel(
                $"Activation message sent to {MentionUtils.MentionUser(userId)} with [link]({x.Link}) - requested via API"
            );
        }));

        return Ok();
    }
}