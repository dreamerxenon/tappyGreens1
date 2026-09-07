using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class FreeWorldDailySceneSelector : MonoBehaviour
{
    [Header("Free World Scenes (Rotation Order)")]
    [SerializeField] private string[] freeWorldScenes;

    [Header("Optional Debug")]
    [SerializeField] private bool forceDayIndex;
    [SerializeField] private int debugDayIndex;

    [SerializeField] private GameObject loadingObj;

    [Header("Lock UI")]
    [Tooltip("Shown instead of loading the scene while DailyResetManager reports Free World as locked.")]
    [SerializeField] private GameObject lockPanel;
    [SerializeField] private TextMeshProUGUI lockText;
    [SerializeField] private string lockMessageFormat = "Free World is resetting.\nUnlocks in {0}";
    [Tooltip("Optional - wire to a close/dismiss button on lockPanel.")]
    [SerializeField] private Button lockPanelCloseButton;

    private string todaysScene;

    void Awake()
    {
        SelectTodaysScene();

        if (lockPanel != null)
            lockPanel.SetActive(false);

        if (lockPanelCloseButton != null)
            lockPanelCloseButton.onClick.AddListener(HideLockedMessage);
    }

    void Update()
    {
        // Keep the countdown live while the panel is showing, and auto-hide
        // the moment the lock window actually ends rather than waiting for
        // another button tap.
        if (lockPanel != null && lockPanel.activeSelf)
        {
            if (DailyResetManager.Instance == null || !DailyResetManager.Instance.IsFreeWorldLocked())
            {
                HideLockedMessage();
                return;
            }

            UpdateLockText();
        }
    }

    private void SelectTodaysScene()
    {
        if (freeWorldScenes == null || freeWorldScenes.Length == 0)
        {
            Debug.LogError("FreeWorldDailySceneSelector: No scenes assigned!");
            return;
        }

        int dayIndex = forceDayIndex
            ? debugDayIndex
            : GetDaysSinceEpoch();

        int sceneIndex = dayIndex % freeWorldScenes.Length;
        todaysScene = freeWorldScenes[sceneIndex];

        Debug.Log($"[FreeWorld] Day {dayIndex} -> Scene: {todaysScene}");
    }

    private int GetDaysSinceEpoch()
    {
        DateTime epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (int)(DateTime.UtcNow - epoch).TotalDays;
    }

    public void PlayFreeWorld()
    {
        if (string.IsNullOrEmpty(todaysScene))
        {
            Debug.LogError("Today's Free World scene not set!");
            return;
        }

        // DailyResetManager may not exist yet if UGSInitializer's boot
        // sync hasn't reached the point of creating/anchoring it - treat
        // "unknown" as "not locked" rather than blocking play over a
        // timing gap that isn't actually the reset window.
        if (DailyResetManager.Instance != null && DailyResetManager.Instance.IsFreeWorldLocked())
        {
            ShowLockedMessage();
            return;
        }

        loadingObj.SetActive(true);
        SceneManager.LoadScene(todaysScene);
    }

    private void ShowLockedMessage()
    {
        if (lockPanel != null) lockPanel.SetActive(true);
        UpdateLockText();
    }

    private void UpdateLockText()
    {
        if (lockText == null || DailyResetManager.Instance == null) return;

        TimeSpan remaining = DailyResetManager.Instance.TimeUntilUnlocked();
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        int totalMinutes = (int)remaining.TotalMinutes;
        string timeStr = $"{totalMinutes:D2}:{remaining.Seconds:D2}";

        lockText.text = string.Format(lockMessageFormat, timeStr);
    }

    public void HideLockedMessage()
    {
        if (lockPanel != null) lockPanel.SetActive(false);
    }
}