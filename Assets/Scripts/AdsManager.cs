
using UnityEngine;
using GoogleMobileAds.Api;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

public class AdsManager : MonoBehaviour
{
    public static AdsManager instance;

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    [Header("Ad Unit IDs")]
    private string bannerId = "ca-app-pub-3108419078557634/7556422704";
    private string interstitialId = "ca-app-pub-3108419078557634/9991014353";
    private string rewardedId = "ca-app-pub-3108419078557634/7791408180";

    public static event Action OnRewardedAdCompleted;

    private void Awake()
    {
        // Make singleton
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
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob Initialized");

            RequestBanner();
            RequestInterstitial();
            RequestRewarded();

            StartCoroutine(ShowInterstitialRoutine());
        });
    }

    #region Banner
    private void RequestBanner()
    {
        bannerView = new BannerView(bannerId, AdSize.Banner, AdPosition.Bottom);
        var request = new AdRequest();
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            bannerView.LoadAd(request);
        }
    }

    public void ShowBanner() => bannerView?.Show();
    public void HideBanner() => bannerView?.Hide();
    #endregion

    #region Interstitial
    private void RequestInterstitial()
    {
        var request = new AdRequest();
        InterstitialAd.Load(interstitialId, request, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("Interstitial failed to load: " + error);
                return;
            }
            interstitialAd = ad;
            Debug.Log("Interstitial loaded");
        });
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            

 
                interstitialAd.Show();
                RequestInterstitial();
            
        }
        else
            Debug.Log("Interstitial not ready yet");
    }
    public void ShowInterstitialforLevels()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            

                interstitialAd.Show();
            RequestInterstitial();
                PlayerPrefs.SetString("CanShowAd", "false");
                

            

        }
        else
            Debug.Log("Interstitial not ready yet");
    }
    private IEnumerator ShowInterstitialRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(300f);
            // 300 seconds = 5 minutes
            if(SceneManager.GetActiveScene().buildIndex == 0)
            ShowInterstitial();
            


                PlayerPrefs.SetString("CanShowAd", "true");
        }
    }
    #endregion

    #region Rewarded
    public void RequestRewarded()
    {
        var request = new AdRequest();
        RewardedAd.Load(rewardedId, request, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("Rewarded failed to load: " + error);
                return;
            }

            rewardedAd = ad;
            Debug.Log("Rewarded loaded");
        });
    }

    public void ShowRewarded()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log($"User earned reward: {reward.Amount} {reward.Type}");
                OnRewardedAdCompleted?.Invoke();
            });
        }
        else
        {
            Debug.Log("Rewarded not ready yet, requesting new one...");
            RequestRewarded();
        }
    }
    #endregion
}
