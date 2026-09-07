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
    /// How long ago the most recent daily reset happened. Since resets
    /// are exactly 24h apart, the last reset was nextResetUtc - 24h -
    /// equivalently, 24h minus however long remains until the next one.
    /// </summary>
    public TimeSpan TimeSinceLastReset()
    {
        return TimeSpan.FromHours(24) - TimeUntilReset();
    }

    [Header("Free World Lock Window")]
    [Tooltip("Lock Free World starting this many minutes before the daily reset.")]
    public float lockBeforeResetMinutes = 10f;
    [Tooltip("Keep Free World locked for this many minutes after the daily reset.")]
    public float lockAfterResetMinutes = 60f;

    /// <summary>
    /// True during the blackout window around the daily reset - the last
    /// lockBeforeResetMinutes before it, through lockAfterResetMinutes
    /// after it. Use this both to block entering Free World from Home
    /// and to kick a player already inside back out (see
    /// FreeWorldRunManager.CheckForLockWindow).
    /// </summary>
    public bool IsFreeWorldLocked()
    {
        TimeSpan untilReset = TimeUntilReset();
        if (untilReset.TotalMinutes <= lockBeforeResetMinutes)
            return true;

        TimeSpan sinceReset = TimeSinceLastReset();
        if (sinceReset.TotalMinutes >= 0 && sinceReset.TotalMinutes <= lockAfterResetMinutes)
            return true;

        return false;
    }

    /// <summary>
    /// How much longer the lock will last, for display purposes (e.g. "Free
    /// World unlocks in 42:10"). Returns TimeSpan.Zero if not currently locked.
    /// </summary>
    public TimeSpan TimeUntilUnlocked()
    {
        TimeSpan untilReset = TimeUntilReset();
        if (untilReset.TotalMinutes <= lockBeforeResetMinutes)
        {
            // Locked in the pre-reset window - unlocks lockAfterResetMinutes
            // after the reset itself, i.e. untilReset + lockAfterResetMinutes.
            return untilReset + TimeSpan.FromMinutes(lockAfterResetMinutes);
        }

        TimeSpan sinceReset = TimeSinceLastReset();
        if (sinceReset.TotalMinutes >= 0 && sinceReset.TotalMinutes <= lockAfterResetMinutes)
        {
            return TimeSpan.FromMinutes(lockAfterResetMinutes) - sinceReset;
        }

        return TimeSpan.Zero;
    }

    /// <summary>
    /// True once TimeUntilReset() reflects the server's actual date
    /// rather than the Awake() fallback guess.
    /// </summary>
    public bool HasServerAnchor => hasServerAnchor;
}