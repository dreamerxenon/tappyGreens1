using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    // Set as soon as we're signed in - this is all Cloud Code / Cloud Save
    // callers (like Free World) actually need. Don't make them wait for
    // currency or daily-reward, which are unrelated systems.
    public static bool IsAuthReady { get; private set; }

    // Set once everything (currency, daily reward) has also finished -
    // use this for menu/HUD code that displays currency on screen.
    public static bool IsFullyReady { get; private set; }

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

            IsAuthReady = true;
            Debug.Log("UGS auth ready");

            if (CloudCodeManager.Instance != null)
                CloudCodeManager.Instance.PrefetchDailyFreeWorld();

            // Run currency load and the reward-claim network call concurrently -
            // they're independent; only applying the reward needs currentMoney loaded.
            Task<DailyRewardResult> claimTask = DailyRewardManager.Instance != null
                ? DailyRewardManager.Instance.FetchClaimResultAsync()
                : Task.FromResult<DailyRewardResult>(null);

            Task moneyTask = CurrencyManager.Instance != null
                ? CurrencyManager.Instance.Initialize()
                : Task.CompletedTask;

            await Task.WhenAll(claimTask, moneyTask);

            DailyRewardManager.Instance?.ApplyClaimResult(claimTask.Result);

            IsFullyReady = true;
            Debug.Log("UGS fully ready");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGSInitializer] Initialization failed: {e}");
        }
    }

    // Free World and other Cloud Code consumers should call THIS, not
    // WaitUntilFullyReady - it removes currency/daily-reward from their
    // critical path entirely. Bounded with a timeout so a bad connection
    // can't hang the caller forever.
    public static async Task<bool> WaitUntilAuthReady(int timeoutMs = 15000)
    {
        int waited = 0;
        while (!IsAuthReady && waited < timeoutMs)
        {
            await Task.Delay(100);
            waited += 100;
        }

        if (!IsAuthReady)
            Debug.LogError("[UGSInitializer] Timed out waiting for auth.");

        return IsAuthReady;
    }

    // Use this only where currency/daily-reward state is actually needed
    // (e.g. main menu HUD), not for Free World.
    public static async Task<bool> WaitUntilFullyReady(int timeoutMs = 20000)
    {
        int waited = 0;
        while (!IsFullyReady && waited < timeoutMs)
        {
            await Task.Delay(100);
            waited += 100;
        }

        if (!IsFullyReady)
            Debug.LogError("[UGSInitializer] Timed out waiting for full init.");

        return IsFullyReady;
    }
}