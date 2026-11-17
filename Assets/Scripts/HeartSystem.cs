using System;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
using Unity.Notifications.Android;
#endif

public class HeartSystem : MonoBehaviour
{
    public static HeartSystem Instance;

    [Header("Heart Settings")]
    public int maxHeart = 6;
    public int currentHeart;
    public float refillTime = 300f;

    [Header("Notification Settings")]
    public string channelId = "heart_refill_channel";
    public string notificationTitle = "Hearts Full!";
    public string notificationText = "Your hearts have refilled. Come back and play!";

    private DateTime lastUpdateTime;
    private const string HeartKey = "CurrentHeart";
    private const string TimeKey = "LastUpdateTimeTicks";
    private const int NotificationId = 1001;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

#if UNITY_ANDROID
        RegisterNotificationChannel();
        RequestNotificationPermission(); // add this line
#endif
        LoadFuelData();
    }

#if UNITY_ANDROID
    private void RequestNotificationPermission()
    {
        const string permission = "android.permission.POST_NOTIFICATIONS";

        if (!Permission.HasUserAuthorizedPermission(permission))
        {
            Permission.RequestUserPermission(permission);
        }
    }
#endif
    void Update()
    {
        RefillHeartOverTime();
    }

    private void RefillHeartOverTime()
    {
        if (currentHeart >= maxHeart) return;

        TimeSpan timePassed = DateTime.UtcNow - lastUpdateTime;
        int heartToAdd = (int)(timePassed.TotalSeconds / refillTime);

        if (heartToAdd > 0)
        {
            currentHeart = Mathf.Min(currentHeart + heartToAdd, maxHeart);

            double leftoverSeconds = timePassed.TotalSeconds % refillTime;
            lastUpdateTime = DateTime.UtcNow.AddSeconds(-leftoverSeconds);

            SaveHeartData();

            if (currentHeart >= maxHeart)
                CancelFullHeartNotification();
        }
    }

    public bool UseHeart(int amount = 1)
    {
        if (currentHeart >= amount)
        {
            currentHeart -= amount;
            if (currentHeart < maxHeart)
                lastUpdateTime = DateTime.UtcNow;

            SaveHeartData();
            ScheduleFullHeartNotification();
            return true;
        }
        return false;
    }

    public TimeSpan GetNextHeartTime()
    {
        if (currentHeart >= maxHeart) return TimeSpan.Zero;

        TimeSpan elapsed = DateTime.UtcNow - lastUpdateTime;
        double remaining = refillTime - elapsed.TotalSeconds;
        if (remaining < 0) remaining = 0;

        return TimeSpan.FromSeconds(remaining);
    }

    // Time until ALL hearts (not just the next one) are full
    private TimeSpan GetTimeUntilFull()
    {
        int heartsNeeded = maxHeart - currentHeart;
        if (heartsNeeded <= 0) return TimeSpan.Zero;

        double secondsUntilNext = refillTime - (DateTime.UtcNow - lastUpdateTime).TotalSeconds;
        if (secondsUntilNext < 0) secondsUntilNext = 0;

        double totalSeconds = secondsUntilNext + (heartsNeeded - 1) * refillTime;
        return TimeSpan.FromSeconds(totalSeconds);
    }

    public void SaveHeartData()
    {
        PlayerPrefs.SetInt(HeartKey, currentHeart);
        PlayerPrefs.SetString(TimeKey, lastUpdateTime.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private void LoadFuelData()
    {
        currentHeart = PlayerPrefs.GetInt(HeartKey, maxHeart);
        string savedTicks = PlayerPrefs.GetString(TimeKey, string.Empty);

        if (string.IsNullOrEmpty(savedTicks) || !long.TryParse(savedTicks, out long ticks))
            lastUpdateTime = DateTime.UtcNow;
        else
            lastUpdateTime = new DateTime(ticks, DateTimeKind.Utc);

        RefillHeartOverTime();

        // Re-sync notification with current state on every load/resume
        if (currentHeart >= maxHeart)
            CancelFullHeartNotification();
        else
            ScheduleFullHeartNotification();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) // app going to background
        {
            SaveHeartData();
            if (currentHeart < maxHeart)
                ScheduleFullHeartNotification();
        }
        else // app coming back to foreground
        {
            LoadFuelData(); // this cancels the notification if no longer needed
        }
    }

    void OnApplicationQuit()
    {
        SaveHeartData();
        if (currentHeart < maxHeart)
            ScheduleFullHeartNotification();
    }

#if UNITY_ANDROID
    private void RegisterNotificationChannel()
    {
        var channel = new AndroidNotificationChannel()
        {
            Id = channelId,
            Name = "Heart Refill Notifications",
            Importance = Importance.Default,
            Description = "Notifies when hearts are fully refilled",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    private void ScheduleFullHeartNotification()
    {
        // Always clear the old one first so you never stack duplicates
        CancelFullHeartNotification();

        TimeSpan timeUntilFull = GetTimeUntilFull();
        if (timeUntilFull <= TimeSpan.Zero) return;

        var notification = new AndroidNotification
        {
            Title = notificationTitle,
            Text = notificationText,
            FireTime = DateTime.Now.Add(timeUntilFull), // local time, that's what the API expects
        };

        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channelId, NotificationId);
    }

    private void CancelFullHeartNotification()
    {
        AndroidNotificationCenter.CancelNotification(NotificationId);
    }
#else
    private void ScheduleFullHeartNotification() { }
    private void CancelFullHeartNotification() { }
#endif
}