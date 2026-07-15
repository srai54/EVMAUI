namespace EVSwap.Mobile.Helpers;

public static class TaskExtensions
{
    public static async void FireAndForget(this Task task, Action<Exception>? onError = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}
