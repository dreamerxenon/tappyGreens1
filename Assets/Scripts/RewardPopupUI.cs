using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace TappyGreens.FreeWorld
{
    /// <summary>
    /// Shows the "you earned a daily reward" popup. Attach to a Canvas panel
    /// in whichever scene the player lands on after boot (e.g. Home).
    /// Assign panelRoot to the visual container (starts hidden), wire the
    /// text fields, and hook claimButton to your existing button prefab.
    /// </summary>
    public class RewardPopupUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI challengeText;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private Button claimButton;

        private bool isSubscribed = false;

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (claimButton != null)
                claimButton.onClick.AddListener(Hide);
        }

        private async void OnEnable()
        {
            // Don't assume DailyRewardManager.Instance already exists -
            // script execution order across different GameObjects isn't
            // guaranteed, so if this panel enables before
            // DailyRewardManager's Awake() has run, waiting here (instead
            // of bailing out immediately) means we still catch the
            // subscription rather than silently never firing for the
            // rest of this object's lifetime.
            int waited = 0;
            const int timeoutMs = 5000;

            while (DailyRewardManager.Instance == null && waited < timeoutMs)
            {
                await Task.Delay(50);
                waited += 50;

                // Bail if we got disabled again while waiting - OnDisable
                // already handled cleanup, there's nothing to subscribe to.
                if (this == null || !isActiveAndEnabled)
                    return;
            }

            if (DailyRewardManager.Instance == null)
            {
                Debug.LogWarning("[RewardPopupUI] DailyRewardManager never became available this session - reward popups won't show.");
                return;
            }

            DailyRewardManager.Instance.OnRewardClaimed += HandleRewardClaimed;
            isSubscribed = true;

            // Covers the case where a claim already resolved (e.g. during
            // a loading screen, or an earlier sync this session) before
            // this UI existed or was re-enabled.
            if (DailyRewardManager.Instance.TryGetPendingReward(out var pending))
            {
                HandleRewardClaimed(pending);
            }
        }

        private void OnDisable()
        {
            if (isSubscribed && DailyRewardManager.Instance != null)
                DailyRewardManager.Instance.OnRewardClaimed -= HandleRewardClaimed;

            isSubscribed = false;
        }

        private void HandleRewardClaimed(DailyRewardResult result)
        {
            if (challengeText != null)
                challengeText.text = result.challenge == "LowestStrokes" ? "Lowest Strokes" : "Fastest Time";

            if (rankText != null)
                rankText.text = $"Rank #{result.rank}";

            if (rewardText != null)
                rewardText.text = $"+{result.reward}";

            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        private void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}