using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    // Set as soon as we're signed in - this is all Cloud Code / Cloud Save
    // callers (like Free World) actually need. Don't make them wait for
    // currency or daily-reward, which are unrelated systems. Once true,
    // stays true for the whole session - signing in isn't repeated.
    public static bool IsAuthReady { get; private set; }

    // Set once currency + daily-reward have also finished syncing. Unlike
    // IsAuthReady, this gets reset to false and re-earned every time the
    // Home scene reloads (see OnSceneLoaded), so it always reflects
    // whether the CURRENT sync attempt has completed - use this for
    // menu/HUD code that displays currency on screen.
    public static bool IsFullyReady { get; private set; }

    private static UGSInitializer _instance;
    private bool isSyncing = false;

    [Header("Sign-in / Sync UI")]
    [Tooltip("Shown from the moment a sync starts until it finishes - both at first launch and every time the Home scene reloads.")]
    public GameObject signInLoadingPanel;
    [Tooltip("Shown if a sync fails (auth, currency, or daily-reward). Player must retry to proceed.")]
    public GameObject signInErrorPanel;
    public TextMeshProUGUI signInErrorText;
    public string initFailedMessage = "Couldn't sync with the server. Please check your connection and try again.";
    public Button retryButton;

    [Header("Re-sync on Home Reload")]
    [Tooltip("Every time a scene with this name loads, currency and daily-reward are re-fetched so the player is never looking at stale state.")]
    public string homeSceneName = "Home";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetryClicked);
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        _ = RunFullSync();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.IsNullOrEmpty(homeSceneName) && scene.name == homeSceneName)
        {
            _ = RunFullSync();
        }
    }

    /// <summary>
    /// Runs the whole sync chain in one go: auth (only if not already
    /// signed in this session), then currency + daily-reward together.
    /// Safe to call repeatedly - re-entrant calls (e.g. Home reloading
    /// again while a previous sync is still in flight) are ignored, and
    /// already-completed steps (auth) are cheap no-ops on repeat calls.
    /// </summary>
    private async Task RunFullSync()
    {
        if (isSyncing) return;
        isSyncing = true;

        // Currency/daily-reward are being re-fetched - anything gating on
        // IsFullyReady should wait for THIS attempt, not treat a prior
        // session's success as still current. IsAuthReady is untouched;
        // there's no need to re-authenticate on every Home reload.
        IsFullyReady = false;
        ShowLoading();

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

            // Kick this off now, in parallel with currency/reward below.
            if (CloudCodeManager.Instance != null)
            {
                CloudCodeManager.Instance.PrefetchDailyFreeWorld();

                // DailyResetManager needs to be anchored to the server's
                // real date as early as possible - not just whenever
                // FreeWorldRunManager happens to load - so that Home-scene
                // UI (e.g. a locked Free World button) has correct lock
                // timing from the moment the app boots, not only after
                // the player has visited Free World once this session.
                _ = AnchorDailyResetOnceDailyDataResolves();
            }

            // Currency load and the reward-claim network call run
            // concurrently - they're independent; only applying the
            // reward needs currentMoney loaded. Both were already started
            // above (calling an async method begins executing it
            // immediately, up to its first await), so awaiting them
            // separately below doesn't lose that concurrency.
            Task<DailyRewardResult> claimTask = DailyRewardManager.Instance != null
                ? DailyRewardManager.Instance.FetchClaimResultAsync()
                : Task.FromResult<DailyRewardResult>(null);

            Task moneyTask = CurrencyManager.Instance != null
                ? CurrencyManager.Instance.Initialize()
                : Task.CompletedTask;

            // IMPORTANT: these are awaited separately, NOT via
            // Task.WhenAll(claimTask, moneyTask). If moneyTask throws
            // while claimTask succeeded, Task.WhenAll would throw before
            // ApplyClaimResult ever ran - silently dropping a genuinely
            // successful, server-recorded reward claim (the popup and
            // event would never fire, and since the server already wrote
            // its idempotency key, that reward is gone for good on retry
            // too). Awaiting separately guarantees a successful claim is
            // always applied, regardless of what happens to currency sync.
            Exception firstFailure = null;

            try
            {
                await moneyTask;
            }
            catch (Exception e)
            {
                firstFailure = e;
                Debug.LogError($"[UGSInitializer] Currency sync failed: {e}");
            }

            try
            {
                DailyRewardResult claimResult = await claimTask;
                DailyRewardManager.Instance?.ApplyClaimResult(claimResult);
            }
            catch (Exception e)
            {
                firstFailure ??= e;
                Debug.LogError($"[UGSInitializer] Reward claim failed: {e}");
            }

            if (firstFailure != null)
                throw firstFailure;

            IsFullyReady = true;
            Debug.Log("UGS sync complete");

            HideAllPanels();
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGSInitializer] Sync failed: {e}");
            ShowError();
        }
        finally
        {
            isSyncing = false;
        }
    }

    private void ShowLoading()
    {
        if (signInErrorPanel != null) signInErrorPanel.SetActive(false);
        if (signInLoadingPanel != null) signInLoadingPanel.SetActive(true);
    }

    private void ShowError()
    {
        if (signInLoadingPanel != null) signInLoadingPanel.SetActive(false);

        if (signInErrorPanel != null) signInErrorPanel.SetActive(true);
        if (signInErrorText != null) signInErrorText.text = initFailedMessage;
        if (retryButton != null) retryButton.interactable = true;
    }

    private void HideAllPanels()
    {
        if (signInLoadingPanel != null) signInLoadingPanel.SetActive(false);
        if (signInErrorPanel != null) signInErrorPanel.SetActive(false);
    }

    private async void OnRetryClicked()
    {
        if (retryButton != null) retryButton.interactable = false;
        await RunFullSync();
    }

    private async Task AnchorDailyResetOnceDailyDataResolves()
    {
        if (CloudCodeManager.Instance == null || DailyResetManager.Instance == null)
            return;

        // Same task PrefetchDailyFreeWorld() already started (or an
        // already-resolved one), so this is usually near-instant rather
        // than a second network call.
        DailyFreeWorldData daily = await CloudCodeManager.Instance.GetDailyFreeWorld();

        if (daily != null)
        {
            DailyResetManager.Instance.SetAnchorFromServerDate(daily.date);
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

    // Use this where currency/daily-reward state is actually needed
    // (e.g. main menu HUD), not for Free World. Since IsFullyReady is
    // reset on every Home reload, this correctly waits for the CURRENT
    // sync attempt rather than a stale prior success.
    public static async Task<bool> WaitUntilFullyReady(int timeoutMs = 20000)
    {
        int waited = 0;
        while (!IsFullyReady && waited < timeoutMs)
        {
            await Task.Delay(100);
            waited += 100;
        }

        if (!IsFullyReady)
            Debug.LogError("[UGSInitializer] Timed out waiting for full sync.");

        return IsFullyReady;
    }
}