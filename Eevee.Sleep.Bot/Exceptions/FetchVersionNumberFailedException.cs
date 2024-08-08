namespace Eevee.Sleep.Bot.Exceptions;

public class FetchVersionNumberFailedException(
    string message,
    Dictionary<string, string?> context
) : DocumentProcessingException(message, context);