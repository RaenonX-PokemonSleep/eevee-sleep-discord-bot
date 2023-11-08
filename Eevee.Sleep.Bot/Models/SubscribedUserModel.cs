using JetBrains.Annotations;

namespace Eevee.Sleep.Bot.Models;

public record SubscribedUserModel {
    [UsedImplicitly]
    public required ulong RoleId { get; init; }
    
    [UsedImplicitly]
    public required ulong UserId { get; init; }
    
    [UsedImplicitly]
    public required ushort Discriminator { get; init; }
    
    [UsedImplicitly]
    public required string Username { get; init; }
};