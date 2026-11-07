namespace ClubDoorman;

internal static class SemaphoreHelper
{
    public static async Task<IDisposable> AwaitAsync(SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        return new SemaphoreReleaser(semaphore);
    }

    private class SemaphoreReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose()
        {
            semaphore.Release();
        }
    }
}
