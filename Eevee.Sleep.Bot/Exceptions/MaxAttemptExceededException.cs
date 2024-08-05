namespace Eevee.Sleep.Bot.Exceptions;

public class MaxAttemptExceededException(string message, DocumentProcessingException e) : Exception(message) {
    public new DocumentProcessingException InnerException { get; } = e;
}