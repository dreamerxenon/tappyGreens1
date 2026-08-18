using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    private static UGSInitializer _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Fire-and-forget from a non-async Awake, running the actual
        // work in a separate async Task method. A community-reported
        // Unity Services quirk has LeaderboardsService.Instance throw
        // "not initialized" even when UnityServices.State already reads
        // Initialized, when the awaited init chain runs directly inline
        // inside an async void Awake(). Moving it out fixed it for others
        // hitting the same error.
        _ = InitializeServicesAsync();
    }

    private async Task InitializeServicesAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Debug.Log("Signed in! PlayerID: " +
                          AuthenticationService.Instance.PlayerId);
            }

            // Initialize currency after login
            if (CurrencyManager.Instance != null)
            {
                await CurrencyManager.Instance.Initialize();
                Debug.Log("Cloud Money: " + CurrencyManager.Instance.GetMoney());
            }

            // Check for an unclaimed daily reward once currency is ready, so
            // AddMoney has a valid loaded balance to add on top of.
            if (DailyRewardManager.Instance != null)
            {
                await DailyRewardManager.Instance.CheckAndClaimDailyRewardAsync();
            }

            IsReady = true;
            Debug.Log("UGS Ready");
        }
        catch (Exception e)
        {
            // async void Awake() would have swallowed this silently.
            // Logging here surfaces whatever's actually failing in the chain.
            Debug.LogError($"[UGSInitializer] Initialization failed: {e}");
        }
    }

    public static async Task WaitUntilReady()
    {
        while (!IsReady)
        {
            await Task.Delay(100);
        }
    }
}