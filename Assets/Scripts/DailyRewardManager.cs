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
    /// Drives the server-authoritative daily reward claim. Call
    /// CheckAndClaimDailyRewardAsync() once per session (e.g. from your
    /// UGSInitializer flow, right after Currency Initialization).
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

        public async Task CheckAndClaimDailyRewardAsync()
        {
            DailyRewardResult result;

            try
            {
                result = await CloudCodeService.Instance.CallEndpointAsync<DailyRewardResult>(
                    ClaimScriptName, new Dictionary<string, object>());
            }
            catch (CloudCodeException e)
            {
                Debug.LogError($"[DailyRewardManager] Claim call failed: {e.Message}");
                return;
            }

            if (result.success && result.reward > 0)
            {
                // CurrencyManager.AddMoney both bumps the local cached value
                // and re-saves it to Cloud Save. That second write is
                // redundant here (the Cloud Code script already wrote the new
                // total server-side) but harmless — it just re-writes the
                // same number the server already set, as long as nothing else
                // changed currentMoney in between load and claim.
                await CurrencyManager.Instance.AddMoney(result.reward);

                _pendingReward = result;
                _hasPendingReward = true;
                OnRewardClaimed?.Invoke(result);
            }
            else
            {
                OnNoRewardAvailable?.Invoke();
            }
        }
    }
