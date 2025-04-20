namespace ClubDoorman;

internal static class TaskExtensions
{
    public static void FireAndForget(this Task task, ILogger? logger = null, string? message = null)
    {
        task.ContinueWith(x =>
        {
            if (task.IsCompletedSuccessfully)
                return;
            if (task.Exception != null && logger != null && !string.IsNullOrWhiteSpace(message))
                logger.LogWarning(task.Exception.InnerException, message);
        });
    }
}
