using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;


/// <summary>
/// Result payload returned by the ClaimDailyReward Cloud Code script.
/// </summary>
[Serializable]
public class DailyRewardResult
{
    public bool success;
    public string status;      // "claimed" | "already_claimed" | "no_event_found"
    public int rank;
    public int reward;
    public string leaderboard; // "lowest_strokes_leaderboard" | "fastest_time_leaderboard"
    public string challenge;   // "LowestStrokes" | "FastestTime"
    public string date;        // yyyyMMdd
    public string version;     // pinned leaderboard version id used for the lookup
}

/// <summary>
/// Drives the server-authoritative daily reward claim.
///
/// Split into two steps so the network call (FetchClaimResultAsync) can
/// run concurrently with CurrencyManager.Initialize() instead of after
/// it - only ApplyClaimResult actually needs currentMoney to be loaded.
/// See UGSInitializer for how these are orchestrated together.
/// </summary>
public class DailyRewardManager : MonoBehaviour
{
    public static DailyRewardManager Instance { get; private set; }

    /// <summary>Fired when a nonzero reward was granted this call.</summary>
    public event Action<DailyRewardResult> OnRewardClaimed;

    /// <summary>Fired when there was nothing to claim (already claimed, no event, rank outside tiers, etc).</summary>
    public event Action OnNoRewardAvailable;

    private const string ClaimScriptName = "ClaimDailyReward";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private DailyRewardResult _pendingReward;
    private bool _hasPendingReward;

    /// <summary>
    /// Lets a popup UI that initializes AFTER the claim already resolved
    /// (e.g. it ran during a loading screen before the Home scene existed)
    /// still pick up the result instead of missing the event entirely.
    /// </summary>
    public bool TryGetPendingReward(out DailyRewardResult result)
    {
        result = _pendingReward;
        bool had = _hasPendingReward;
        _hasPendingReward = false;
        _pendingReward = null;
        return had;
    }

    /// <summary>
    /// Just calls the claim endpoint - no dependency on CurrencyManager,
    /// safe to run in parallel with CurrencyManager.Initialize(). Throws
    /// on failure (logged first) so the caller - UGSInitializer - can
    /// tell "network/service failure" apart from "nothing to claim",
    /// which a null return can't distinguish.
    /// </summary>
    public async Task<DailyRewardResult> FetchClaimResultAsync()
    {
        try
        {
            return await CloudCodeService.Instance.CallEndpointAsync<DailyRewardResult>(
                ClaimScriptName, new Dictionary<string, object>());
        }
        catch (CloudCodeException e)
        {
            Debug.LogError($"[DailyRewardManager] Claim call failed: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Applies an already-fetched result. Local-only currency update -
    /// the Cloud Code script already wrote the authoritative total
    /// server-side, so this does NOT re-save to Cloud Save.
    /// </summary>
    public void ApplyClaimResult(DailyRewardResult result)
    {
        if (result == null)
            return;

        if (result.success && result.reward > 0)
        {
            CurrencyManager.Instance.AddMoneyLocalOnly(result.reward);

            _pendingReward = result;
            _hasPendingReward = true;
            OnRewardClaimed?.Invoke(result);
        }
        else
        {
            OnNoRewardAvailable?.Invoke();
        }
    }

    /// <summary>
    /// Convenience wrapper kept for any callers that don't need the
    /// fetch/apply split - equivalent to the old single-step version,
    /// just without the redundant Cloud Save write.
    /// </summary>
    public async Task CheckAndClaimDailyRewardAsync()
    {
        var result = await FetchClaimResultAsync();
        ApplyClaimResult(result);
    }
}