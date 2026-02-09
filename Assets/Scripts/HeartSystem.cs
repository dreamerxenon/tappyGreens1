using System;
using UnityEngine;

public class HeartSystem : MonoBehaviour
{

    public static HeartSystem Instance;

    [Header("Heart Settings")]
    public int maxHeart = 6;              // Max fuel capacity
    public int currentHeart;               // Current fuel amount
    public float refillTime = 300f;       // Seconds to refill 1 fuel (e.g., 5 minutes)

    private DateTime lastUpdateTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (!PlayerPrefs.HasKey("CurrentHeart"))
        {
            PlayerPrefs.SetInt("CurrentHeart", 6);
        }

        LoadFuelData();
    }
    private void Start()
    {
        LoadFuelData();
    }


    void Update()
    {
        RefillHeartOverTime();
        LoadFuelData();
    }

    private void RefillHeartOverTime()
    {
        currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (currentHeart >= maxHeart) return;

        TimeSpan timePassed = DateTime.Now - lastUpdateTime;
        int heartToAdd = (int)(timePassed.TotalSeconds / refillTime);

        if (heartToAdd > 0)
        {
            currentHeart = Mathf.Min(currentHeart + heartToAdd, maxHeart);

            double leftoverSeconds = timePassed.TotalSeconds % refillTime;
            lastUpdateTime = DateTime.Now.AddSeconds(-leftoverSeconds);

            SaveHeartData();
        }
    }

    public bool UseHeart(int amount = 1)
    {
        if (currentHeart >= amount)
        {
            currentHeart -= amount;
            if (currentHeart < maxHeart)
                lastUpdateTime = DateTime.Now; // restart timer
            SaveHeartData();
            return true;
        }
        return false;
    }

    public TimeSpan GetNextHeartTime()
    {
        currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (currentHeart >= maxHeart) return TimeSpan.Zero;

        TimeSpan elapsed = DateTime.Now - lastUpdateTime;
        double remaining = refillTime - elapsed.TotalSeconds;
        if (remaining < 0) remaining = 0;

        return TimeSpan.FromSeconds(remaining);
    }

    private void SaveHeartData()
    {
        PlayerPrefs.SetInt("CurrentHeart", currentHeart);
        PlayerPrefs.SetString("LastUpdateTime", lastUpdateTime.ToString());
        PlayerPrefs.Save();
    }

    private void LoadFuelData()
    {
        currentHeart = PlayerPrefs.GetInt("CurrentHeart", maxHeart);
        string savedTime = PlayerPrefs.GetString("LastUpdateTime", string.Empty);

        if (string.IsNullOrEmpty(savedTime))
            lastUpdateTime = DateTime.Now;
        else
            lastUpdateTime = DateTime.Parse(savedTime);

        RefillHeartOverTime();
    }


}
