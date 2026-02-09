using UnityEngine;
using System;

public class DailyResetManager : MonoBehaviour
{
    public static DailyResetManager Instance;

    DateTime nextResetUtc;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CalculateNextReset();
    }

    void CalculateNextReset()
    {
        DateTime now = DateTime.UtcNow;
        nextResetUtc = now.Date.AddDays(1);
    }

    public TimeSpan TimeUntilReset()
    {
        return nextResetUtc - DateTime.UtcNow;
    }
}
