using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class FreeWorldDailySceneSelector : MonoBehaviour
{
    [Header("Free World Scenes (Rotation Order)")]
    [SerializeField] private string[] freeWorldScenes;

    [Header("Optional Debug")]
    [SerializeField] private bool forceDayIndex;
    [SerializeField] private int debugDayIndex;

    [SerializeField] private GameObject loadingObj;

    private string todaysScene;

    void Awake()
    {
        SelectTodaysScene();
    }

    /* =====================================================
     *  🌍 DAILY ROTATION (NO REPEATS)
     * ===================================================== */
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

        Debug.Log($"[FreeWorld] Day {dayIndex} → Scene: {todaysScene}");
    }

    private int GetDaysSinceEpoch()
    {
        DateTime epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (int)(DateTime.UtcNow - epoch).TotalDays;
    }

    /* =====================================================
     *  🎮 BUTTON CALL
     * ===================================================== */
    public void PlayFreeWorld()
    {
        if (string.IsNullOrEmpty(todaysScene))
        {
            Debug.LogError("Today's Free World scene not set!");
            return;
        }
        loadingObj.SetActive(true);
        SceneManager.LoadScene(todaysScene);
    }
}
