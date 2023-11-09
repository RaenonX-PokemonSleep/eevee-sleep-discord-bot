using Discord;
using Discord.WebSocket;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Eevee.Sleep.Bot.Controllers;

[ApiController]
[Route("/send-activation")]
public class SendUserActivationController : ControllerBase {
    private readonly ILogger<SubscribedUserController> _logger;

    private readonly DiscordSocketClient _client;

    public SendUserActivationController(
        ILogger<SubscribedUserController> logger,
        DiscordSocketClient client
    ) {
        _logger = logger;
        _client = client;
    }

    [HttpPost(Name = "SendActivationMessage")]
    public OkResult Post(DiscordActivationMessageModel[] activationMessages) {
        _logger.LogInformation(
            "Received {Count} activation messages to send",
            activationMessages.Length
        );

        Task.WhenAll(activationMessages.Select(async x => {
            var user = await _client.GetUserAsync(Convert.ToUInt64(x.UserId));

            if (user is null) {
                _logger.LogInformation(
                    "Failed to send activation message to {UserId} - User not found",
                    x.UserId
                );
                await _client.SendMessageInAdminAlertChannel(
                    $"Failed to send activation message to <@{x.UserId}> - User not found"
                );
                return;
            }

            await user.SendMessageAsync(
                x.Link,
                embeds: DiscordMessageMaker.MakeActivationNote()
            );
            _logger.LogInformation(
                "Activation message sent to {UserId} with link {ActivationLink} - requested via API",
                x.UserId,
                x.Link
            );
            await _client.SendMessageInAdminAlertChannel(
                $"Activation message sent to <@{x.UserId}> with [link]({x.Link}) - requested via API"
            );
        }));

        return Ok();
    }
}