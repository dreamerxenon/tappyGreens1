using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.LevelPlay;
using System;
using System.Collections;

public class AdsManager : MonoBehaviour
{
    public static AdsManager instance;

    [Header("Unity Dashboard App Key")]
    private string appKey = "27d41468d";

    [Header("Ad Unit IDs (from Unity Dashboard > Monetization)")]
    private string bannerId = "zkvotty49c7j82fa";
    private string interstitialId = "empcvombdokezx88";
    private string rewardedId = "y0ebm5ggsne59ubs";

    private LevelPlayBannerAd bannerAd;
    private LevelPlayInterstitialAd interstitialAd;
    private LevelPlayRewardedAd rewardedAd;
    private bool isRewardedLoading = false;

    public static event Action OnRewardedAdCompleted;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Same lesson as before: run this after UGS core init, not in parallel with it.
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay Initialized");

        SetUpBanner();
        SetUpInterstitial();
        SetUpRewarded();

        StartCoroutine(ShowInterstitialRoutine());
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError("LevelPlay init failed: " + error);
    }

    #region Banner
    private void SetUpBanner()
    {
        var bannerConfig = new LevelPlayBannerAd.Config.Builder()
            .SetSize(LevelPlayAdSize.BANNER)
            .SetPosition(LevelPlayBannerPosition.BottomCenter)
            .SetDisplayOnLoad(false) // we control show/hide manually below, matching old behavior
            .Build();

        bannerAd = new LevelPlayBannerAd(bannerId, bannerConfig);
        bannerAd.OnAdLoaded += (info) => Debug.Log("Banner loaded");
        bannerAd.OnAdLoadFailed += (error) => Debug.LogError("Banner failed to load: " + error);
        bannerAd.LoadAd();

        if (SceneManager.GetActiveScene().buildIndex == 0)
            bannerAd.ShowAd();
        else
            bannerAd.HideAd();
    }

    public void ShowBanner() => bannerAd?.ShowAd();
    public void HideBanner() => bannerAd?.HideAd();
    #endregion

    #region Interstitial
    private void SetUpInterstitial()
    {
        interstitialAd = new LevelPlayInterstitialAd(interstitialId);
        interstitialAd.OnAdLoaded += (info) => Debug.Log("Interstitial loaded");
        interstitialAd.OnAdLoadFailed += (error) => Debug.LogError("Interstitial failed to load: " + error);
        interstitialAd.OnAdClosed += (info) => interstitialAd.LoadAd(); // reload only after it's actually closed
        interstitialAd.LoadAd();
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.IsAdReady())
            interstitialAd.ShowAd();
        else
            Debug.Log("Interstitial not ready yet");
    }

    public void ShowInterstitialforLevels()
    {
        if (interstitialAd != null && interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
            PlayerPrefs.SetString("CanShowAd", "false");
        }
        else
        {
            Debug.Log("Interstitial not ready yet");
        }
    }

    private IEnumerator ShowInterstitialRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(300f);

            if (SceneManager.GetActiveScene().buildIndex == 0)
                ShowInterstitial();

            PlayerPrefs.SetString("CanShowAd", "true");
        }
    }
    #endregion

    #region Rewarded
    private void SetUpRewarded()
    {
        rewardedAd = new LevelPlayRewardedAd(rewardedId);
        rewardedAd.OnAdLoaded += (info) =>
        {
            isRewardedLoading = false;
            Debug.Log("Rewarded loaded");
        };
        rewardedAd.OnAdLoadFailed += (error) =>
        {
            isRewardedLoading = false;
            Debug.LogError("Rewarded failed to load: " + error);
            Invoke(nameof(RequestRewarded), 3f); // avoid hammering on failure
        };
        rewardedAd.OnAdRewarded += (reward, info) =>
        {
            Debug.Log($"User earned reward: {reward} {reward}");
            OnRewardedAdCompleted?.Invoke();
        };
        rewardedAd.OnAdClosed += (info) => RequestRewarded(); // preload the next one

        RequestRewarded();
    }

    public void RequestRewarded()
    {
        // Guard against overlapping LoadAd() calls (LevelPlay error 627,
        // "Load is already called") which happens when a delayed retry
        // and a manual "watch ad" tap both try to load at once.
        if (isRewardedLoading) return;

        isRewardedLoading = true;
        rewardedAd?.LoadAd();
    }

    public void ShowRewarded()
    {
        if (rewardedAd != null && rewardedAd.IsAdReady())
        {
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.Log("Rewarded not ready yet, requesting new one...");
            RequestRewarded();
        }
    }
    #endregion

    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }
}