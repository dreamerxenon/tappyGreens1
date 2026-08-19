using UnityEngine;
using System;
using System.Globalization;

public class DailyResetManager : MonoBehaviour
{
    public static DailyResetManager Instance;

    private DateTime nextResetUtc;
    private bool hasServerAnchor = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Fallback only, in case TimeUntilReset() is queried before the
        // first daily fetch completes. This still uses the device clock,
        // so it can be off by however much the device's UTC handling is
        // wrong - but it's better than a null/uninitialized value, and
        // SetAnchorFromServerDate() below immediately supersedes it once
        // real data arrives.
        nextResetUtc = DateTime.UtcNow.Date.AddDays(1);
    }

    /// <summary>
    /// Call this every time you get a fresh DailyFreeWorldData back from
    /// CloudCodeManager.GetDailyFreeWorld() (e.g. from
    /// FreeWorldRunManager.GenerateDailyCourses(), right after
    /// serverDate is set). Anchors the reset boundary to the actual date
    /// the server used to generate today's event, rather than whatever
    /// date the device's own clock thinks "today" is.
    /// </summary>
    public void SetAnchorFromServerDate(string yyyyMMdd)
    {
        if (string.IsNullOrEmpty(yyyyMMdd))
            return;

        if (!DateTime.TryParseExact(
                yyyyMMdd, "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime serverDate))
        {
            Debug.LogError($"[DailyResetManager] Couldn't parse server date '{yyyyMMdd}'");
            return;
        }

        // The event dated `serverDate` resets at the following UTC midnight.
        nextResetUtc = serverDate.AddDays(1);
        hasServerAnchor = true;
    }

    public TimeSpan TimeUntilReset()
    {
        return nextResetUtc - DateTime.UtcNow;
    }

    /// <summary>
    /// True once TimeUntilReset() reflects the server's actual date
    /// rather than the Awake() fallback guess.
    /// </summary>
    public bool HasServerAnchor => hasServerAnchor;
}