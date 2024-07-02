namespace Eevee.Sleep.Bot.Exceptions;

public class ContentStructureChangedException(string message, Dictionary<string, string?> context) : Exception(message) {
    public Dictionary<string, string?> Context { get; init; } = context;
}