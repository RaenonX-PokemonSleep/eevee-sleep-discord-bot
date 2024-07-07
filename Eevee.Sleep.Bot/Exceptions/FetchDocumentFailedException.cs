namespace Eevee.Sleep.Bot.Exceptions;

public class FetchDocumentFailedException(string message, Dictionary<string, string?> context) : DocumentProcessingException(message, context);