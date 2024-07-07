namespace Eevee.Sleep.Bot.Exceptions;

public class ContentStructureChangedException(string message, Dictionary<string, string?> context) : DocumentProcessingException(message, context);