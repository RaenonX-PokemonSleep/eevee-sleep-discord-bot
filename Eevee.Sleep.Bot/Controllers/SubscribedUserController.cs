using Discord.WebSocket;
using Eevee.Sleep.Bot.Controllers.Mongo;
using Eevee.Sleep.Bot.Extensions;
using Eevee.Sleep.Bot.Models;
using Microsoft.AspNetCore.Mvc;

namespace Eevee.Sleep.Bot.Controllers;

[ApiController]
[Route("/subscribed-users")]
public class SubscribedUserController(
    ILogger<SubscribedUserController> logger,
    DiscordSocketClient client
) : ControllerBase {
    [HttpGet(Name = "GetSubscribedUsers")]
    public IEnumerable<SubscribedUserModel> Get() {
        var taggedRoleIds = ActivationPresetController.GetTaggedRolesSubscribersOnly()
            .Select(x => x.RoleId)
            .ToHashSet();

        logger.LogInformation(
            "Getting users with role ID ({taggedRoleCount}): {taggedRoleIds}",
            taggedRoleIds.MergeToSameLine(),
            taggedRoleIds.Count
        );

        return client.GetCurrentWorkingGuild()
            .Roles
            .Where(x => taggedRoleIds.Contains(x.Id))
            .SelectMany(
                x => x.Members.Select(
                    member => new {
                        Member = member,
                        RoleId = x.Id,
                    }
                )
            )
            .DistinctBy(x => x.Member.Id)
            .Select(
                x => new SubscribedUserModel {
                    RoleId = x.RoleId.ToString(),
                    UserId = x.Member.Id.ToString(),
                    Discriminator = x.Member.DiscriminatorValue,
                    Username = x.Member.Username,
                }
            );
    }
}