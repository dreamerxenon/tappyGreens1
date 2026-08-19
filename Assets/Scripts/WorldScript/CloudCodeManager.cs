using Unity.Services.CloudCode;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class CloudCodeManager : MonoBehaviour
{
    public static CloudCodeManager Instance;

    private Task<DailyFreeWorldData> dailyFetchTask;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called once, early, right after auth succeeds (see UGSInitializer).
    // Not required for correctness - GetDailyFreeWorld() will start the
    // fetch itself if nobody prefetched - but this is what lets Free
    // World's own await resolve near-instantly most of the time.
    public void PrefetchDailyFreeWorld()
    {
        if (dailyFetchTask == null || dailyFetchTask.IsFaulted)
        {
            dailyFetchTask = FetchDailyFreeWorldWithTimeout();

            // This task isn't awaited by anyone here (prefetch is
            // fire-and-forget by design), so observe it ourselves -
            // otherwise a fault here can surface later as an unobserved
            // task exception during GC instead of a clean log line.
            _ = dailyFetchTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogWarning(
                        $"[CloudCodeManager] Prefetch failed, will retry on next request: " +
                        $"{t.Exception?.GetBaseException().Message}");
                }
            }, TaskScheduler.Default);
        }
    }

    public async Task<DailyFreeWorldData> GetDailyFreeWorld()
    {
        if (dailyFetchTask == null || dailyFetchTask.IsFaulted)
            dailyFetchTask = FetchDailyFreeWorldWithTimeout();

        try
        {
            return await dailyFetchTask;
        }
        catch (Exception e)
        {
            // dailyFetchTask is now Faulted, so the NEXT call to
            // GetDailyFreeWorld() or PrefetchDailyFreeWorld() will
            // correctly start a fresh attempt instead of reusing this
            // failed result. Callers just see null, same as before.
            Debug.LogError($"[CloudCodeManager] GetDailyFreeWorld failed: {e.Message}");
            return null;
        }
    }

    private async Task<DailyFreeWorldData> FetchDailyFreeWorldWithTimeout(int timeoutMs = 10000)
    {
        var callTask = CloudCodeService.Instance.CallEndpointAsync<DailyFreeWorldData>(
            "GetDailyFreeWorld");

        var timeoutTask = Task.Delay(timeoutMs);

        var finished = await Task.WhenAny(callTask, timeoutTask);

        if (finished == timeoutTask)
        {
            // Throw (not return null) so this Task ends up Faulted -
            // that's what makes the IsFaulted retry check above actually
            // work. A timeout that resolves "successfully" with a null
            // value gets cached forever and never retried.
            throw new TimeoutException("GetDailyFreeWorld timed out.");
        }

        // Propagates the real exception if the call itself faulted,
        // rather than swallowing it.
        return await callTask;
    }
}