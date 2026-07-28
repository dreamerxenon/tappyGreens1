using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (claimButton != null)
                claimButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            if (DailyRewardManager.Instance == null)
                return;

            DailyRewardManager.Instance.OnRewardClaimed += HandleRewardClaimed;

            // Covers the case where CheckAndClaimDailyRewardAsync already
            // finished (e.g. during a loading screen) before this UI existed.
            if (DailyRewardManager.Instance.TryGetPendingReward(out var pending))
            {
                HandleRewardClaimed(pending);
            }
        }

        private void OnDisable()
        {
            if (DailyRewardManager.Instance != null)
                DailyRewardManager.Instance.OnRewardClaimed -= HandleRewardClaimed;
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
