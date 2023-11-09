using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Models;
using Eevee.Sleep.Bot.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Eevee.Sleep.Bot.Controllers;

[ApiController]
[Route("/subscribed-users")]
public class SubscribedUserController : ControllerBase {
    private readonly ILogger<SubscribedUserController> _logger;

    private readonly DiscordSocketClient _client;

    public SubscribedUserController(
        ILogger<SubscribedUserController> logger,
        DiscordSocketClient client
    ) {
        _logger = logger;
        _client = client;
    }

    [HttpGet(Name = "GetSubscribedUsers")]
    public IEnumerable<SubscribedUserModel> Get() {
        var taggedRoleIds = ActivationPresetController.GetTaggedRoleIds();

        _logger.LogInformation(
            "Getting users with role ID ({taggedRoleCount}): {taggedRoleIds}",
            string.Join(" / ", taggedRoleIds),
            taggedRoleIds.Count
        );

        return _client.GetGuild(ConfigHelper.GetDiscordWorkingGuild())
            .Roles
            .Where(x => taggedRoleIds.Contains(x.Id.ToString()))
            .SelectMany(x => x.Members.Select(member => new {
                Member = member,
                RoleId = x.Id
            }))
            .DistinctBy(x => x.Member.Id)
            .Select(x => new SubscribedUserModel {
                RoleId = x.RoleId.ToString(),
                UserId = x.Member.Id.ToString(),
                Discriminator = x.Member.DiscriminatorValue,
                Username = x.Member.Username
            });
    }
}