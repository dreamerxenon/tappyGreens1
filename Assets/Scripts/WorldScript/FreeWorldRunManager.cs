using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Services.Leaderboards;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;


public class FreeWorldRunManager : MonoBehaviour
{
    public FreeWorldChallengeType todaysChallengeType;
    public CourseDatabase courseDatabase;
    public Transform courseSpawnPoint;
    public WorldBallController ball;
    private List<CourseData> dailyCourses = new List<CourseData>();
    private int currentCourseIndex = 0;
    public FreeWorldUIController uIController;
    private GameObject currentCourseInstance;
    private const string SAVE_DATE_KEY = "FreeWorld_Date";
    private const string SAVE_INDEX_KEY = "FreeWorld_CourseIndex";
    private const string SAVE_COMPLETE_KEY = "FreeWorld_Completed";
    private const string SAVE_TOTAL_TIME_KEY = "FreeWorld_TotalTime";
    private const string SAVE_TOTAL_STROKES_KEY = "FreeWorld_TotalStrokes";
    private int totalStrokes = 0;
    private float totalTime = 0f;
    private float courseStartTime;
    public bool timerRunning = false;
    private bool isPaused = false;
    public Button playButton;
    public GameObject loadingScene;
    private int remainingBufferStrokes = 0;
    private bool bufferInitialized = false;
    private string serverDate;

    // NEW: gates any progress logic until the daily fetch has actually completed
    private bool dataReady = false;

    // True from the moment the final course is finished until leaderboard
    // submission actually succeeds. While true, progress is persisted as
    // if the last course were still unfinished (see SaveProgress()), so
    // leaving without a successful submission resumes back at the last
    // course instead of the run silently counting as done.
    private bool finalSubmissionPending = false;
    private int strokesBeforeFinalCourse;
    private float timeBeforeFinalCourse;

    [Header("Error / Fallback")]
    [Tooltip("Shared error UI, reused for both a failed daily-data load and a failed leaderboard submission.")]
    public GameObject errorUi;
    public TextMeshProUGUI errorText;
    [Tooltip("Button the player taps to retry whichever action just failed - its onClick target is swapped at runtime.")]
    public Button retryButton;
    [Tooltip("Optional - lets the player bail out to the home scene instead of retrying.")]
    public Button homeButton;
    [Tooltip("Scene to return to if the player taps homeButton.")]
    public string homeSceneName = "Home";
    [Tooltip("Message shown when the daily data fails to load.")]
    public string loadFailedMessage = "Couldn't load today's courses. Please check your connection and try again.";
    [Tooltip("Message shown when leaderboard submission fails.")]
    public string submissionFailedMessage = "Couldn't submit your score. Please check your connection and try again.";

    public FreeWorldChallengeType ChallengeType => todaysChallengeType;

    public int CurrentCourseNumber => currentCourseIndex + 1;
    public int TotalCourses => dailyCourses.Count;

    public float CurrentTime
    {
        get
        {
            if (!timerRunning) return totalTime;
            if (isPaused) return totalTime;

            float rawTime = Time.time - courseStartTime;

            float timerMultiplier = 1f;

            if (CharacterManager.Instance != null)
                timerMultiplier = CharacterManager.Instance.GetTimerMultiplier();

            return totalTime + (rawTime * timerMultiplier);
        }
    }

    public int CurrentStrokes => totalStrokes;


    private async void Start()
    {
        HideErrorUI();

        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(OnHomeClicked);
            homeButton.onClick.AddListener(OnHomeClicked);
        }

        await AttemptLoadDailyData();
    }

    /// <summary>
    /// Runs the full load-with-one-retry sequence, then either enables
    /// the Play button or shows the error UI with a Retry option. Safe
    /// to call again from OnRetryClicked() - each call is a fresh
    /// attempt, not a continuation of a previous one.
    /// </summary>
    private async Task AttemptLoadDailyData()
    {
        HideErrorUI();

        // Block interaction immediately - fixes the "no courses on first open" race
        if (playButton != null) playButton.enabled = false;
        if (loadingScene != null) loadingScene.SetActive(true);

        await GenerateDailyCourses();

        // Retry once if the first attempt failed (common on slow Android cold-start network)
        if (dailyCourses == null || dailyCourses.Count == 0 || string.IsNullOrEmpty(serverDate))
        {
            Debug.LogWarning("Daily Free World fetch failed or empty, retrying...");
            await Task.Delay(1500);
            await GenerateDailyCourses();
        }

        if (loadingScene != null) loadingScene.SetActive(false);

        dataReady = dailyCourses != null && dailyCourses.Count > 0 && !string.IsNullOrEmpty(serverDate);

        if (!dataReady)
        {
            Debug.LogError("Could not load Daily Free World after retry.");
            if (playButton != null) playButton.enabled = false;
            ShowErrorUI(loadFailedMessage, OnRetryClicked);
            return;
        }

        CheckIfPlayBtnActive();
    }

    /// <summary>
    /// Shows the shared error UI with the given message, and points
    /// retryButton at whichever action should be retried - reused for
    /// both "daily data failed to load" and "leaderboard submission
    /// failed", since the UI itself is identical either way.
    /// </summary>
    private void ShowErrorUI(string message, UnityEngine.Events.UnityAction retryAction)
    {
        if (errorUi != null) errorUi.SetActive(true);

        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(retryAction);
            retryButton.gameObject.SetActive(true);
            retryButton.interactable = true;
        }

        if (homeButton != null) homeButton.gameObject.SetActive(true);
    }

    private void HideErrorUI()
    {
        if (errorUi != null) errorUi.SetActive(false);
        if (errorText != null) errorText.gameObject.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (homeButton != null) homeButton.gameObject.SetActive(false);
    }

    private async void OnRetryClicked()
    {
        if (retryButton != null) retryButton.interactable = false;

        await AttemptLoadDailyData();

        if (retryButton != null) retryButton.interactable = true;
    }

    private void OnHomeClicked()
    {
        if (!string.IsNullOrEmpty(homeSceneName))
        {
            SceneManager.LoadScene(homeSceneName);
        }
        else
        {
            Debug.LogError("[FreeWorldRunManager] homeSceneName is not set - can't return to home.");
        }
    }

    private void CheckIfPlayBtnActive()
    {
        if (!dataReady) return;

        string today = serverDate;
        string savedDate = PlayerPrefs.GetString(SAVE_DATE_KEY);
        if (savedDate == today)
        {
            bool completed = PlayerPrefs.GetInt(SAVE_COMPLETE_KEY, 0) == 1;

            if (completed)
            {
                Debug.Log("Today's run already completed.");
                playButton.enabled = false;
            }
            else
            {
                Debug.Log("Resuming today's run.");
                playButton.enabled = true;
            }
        }
        else
        {
            // Legitimately a new day (or first run ever) - safe to allow starting fresh
            playButton.enabled = true;
        }
    }

    private void Update()
    {

    }

    public void UpdateStrokes(int index)
    {
        if (remainingBufferStrokes > 0)
        {
            remainingBufferStrokes--;
            Debug.Log("Buffer stroke used!");
            return;
        }

        totalStrokes += index;

        // Persist immediately - previously this only got saved at course
        // completion, so strokes taken mid-course were lost if the app
        // was killed (task-switcher swipe, low-memory kill) before the
        // hole was finished.
        SaveProgress();
    }

    /// <summary>
    /// Single source of truth for writing current run progress to
    /// PlayerPrefs. Call this anywhere totalStrokes/totalTime/
    /// currentCourseIndex changes, so "continue from where it stopped"
    /// holds no matter how the player leaves (Exit button, backgrounding
    /// the app, or the OS killing it outright).
    /// </summary>
    private void SaveProgress()
    {
        if (finalSubmissionPending)
        {
            // The final course has been played but the leaderboard hasn't
            // confirmed it yet - persist as if it's still unfinished, so
            // leaving now (without a successful submission) resumes back
            // at the last course with the totals from before it, rather
            // than silently keeping credit for a run that never actually
            // got submitted.
            PlayerPrefs.SetInt(SAVE_INDEX_KEY, dailyCourses.Count - 1);
            PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, timeBeforeFinalCourse);
            PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, strokesBeforeFinalCourse);
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetInt(SAVE_INDEX_KEY, currentCourseIndex);
        PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, totalTime);
        PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, totalStrokes);
        PlayerPrefs.Save();
    }

    public void StartGame()
    {
        // Hard guard: never let a tap through before the daily fetch has resolved.
        // This is what previously caused serverDate == null, which made
        // LoadProgressOrStartNew() think it was a "new day" and wipe saved progress.
        if (!dataReady)
        {
            Debug.LogWarning("StartGame called before data was ready - ignoring tap.");
            return;
        }

        LoadProgressOrStartNew();
    }

    void LoadProgressOrStartNew()
    {
        if (string.IsNullOrEmpty(serverDate))
        {
            Debug.LogError("serverDate not set - aborting to avoid wiping progress.");
            return;
        }

        string today = serverDate;

        if (PlayerPrefs.HasKey(SAVE_DATE_KEY))
        {
            string savedDate = PlayerPrefs.GetString(SAVE_DATE_KEY);
            bool completed = PlayerPrefs.GetInt(SAVE_COMPLETE_KEY, 0) == 1;

            if (savedDate == today && completed)
            {
                // Already finished today's run - never fall through to
                // StartNewRun from here, no matter how this got called.
                Debug.LogWarning("Today's run is already completed - ignoring start request.");
                if (playButton != null) playButton.enabled = false;
                return;
            }

            if (savedDate == today && !completed)
            {
                playButton.enabled = true;
                currentCourseIndex = PlayerPrefs.GetInt(SAVE_INDEX_KEY, 0);
                totalTime = PlayerPrefs.GetFloat(SAVE_TOTAL_TIME_KEY, 0f);
                totalStrokes = PlayerPrefs.GetInt(SAVE_TOTAL_STROKES_KEY, 0);

                if (currentCourseIndex < dailyCourses.Count)
                {
                    LoadCourse(currentCourseIndex);
                }
                else
                {
                    Debug.LogError("Course index out of range.");
                }
                return;
            }
        }

        StartNewRun(today);
    }

    void StartNewRun(string today)
    {
        currentCourseIndex = 0;

        PlayerPrefs.SetString(SAVE_DATE_KEY, today);
        PlayerPrefs.SetInt(SAVE_INDEX_KEY, 0);
        PlayerPrefs.SetInt(SAVE_COMPLETE_KEY, 0);
        PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, 0);
        PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, 0f);
        PlayerPrefs.Save();

        if (CharacterManager.Instance != null)
        {
            remainingBufferStrokes =
                CharacterManager.Instance.GetBufferStrokes();
        }
        else
        {
            remainingBufferStrokes = 0;
        }

        bufferInitialized = true;

        LoadCourse(currentCourseIndex);
    }


    private async Task GenerateDailyCourses()
    {
        await UGSInitializer.WaitUntilAuthReady();

        DailyFreeWorldData daily =
            await CloudCodeManager.Instance.GetDailyFreeWorld();

        // Always reset the list at the start of a fetch attempt so a failed
        // call can never leave dailyCourses null (which crashes TotalCourses).
        dailyCourses = new List<CourseData>();

        if (daily == null)
        {
            Debug.LogError("Failed to retrieve Daily Free World.");
            return;
        }

        // Today's challenge
        todaysChallengeType =
            daily.challenge == "FastestTime"
                ? FreeWorldChallengeType.FastestTime
                : FreeWorldChallengeType.LowestStrokes;

        // Build today's course list
        foreach (int courseIndex in daily.courses)
        {
            if (courseIndex < 0 || courseIndex >= courseDatabase.allCourses.Count)
            {
                Debug.LogWarning($"Invalid course index received: {courseIndex}");
                continue;
            }

            dailyCourses.Add(courseDatabase.allCourses[courseIndex]);
        }

        // Order by difficulty
        dailyCourses.Sort((a, b) =>
            a.difficulty.CompareTo(b.difficulty));

        if (uIController != null) uIController.UpdateStaticTexts();

        Debug.Log($"Daily FreeWorld ({daily.date})");
        Debug.Log($"Challenge: {daily.challenge}");
        Debug.Log($"Courses Loaded: {dailyCourses.Count}");

        for (int i = 0; i < dailyCourses.Count; i++)
        {
            Debug.Log(
                $"{i + 1}. {dailyCourses[i].courseID}  Difficulty: {dailyCourses[i].difficulty}");
        }

        // Only commit serverDate once everything above succeeded.
        serverDate = daily.date;
        DailyResetManager.Instance?.SetAnchorFromServerDate(daily.date);
    }

    void LoadCourse(int index)
    {
        if (currentCourseInstance != null)
            Destroy(currentCourseInstance);

        // Snapshot totals as they stood before this attempt, so if this
        // turns out to be the final course and the leaderboard submission
        // afterward fails, we know exactly what to roll back to.
        if (index == dailyCourses.Count - 1)
        {
            strokesBeforeFinalCourse = totalStrokes;
            timeBeforeFinalCourse = totalTime;
        }

        currentCourseInstance = Instantiate(
            dailyCourses[index].coursePrefab,
            courseSpawnPoint.position,
            Quaternion.identity
        );

        BallSpawnPoint spawn =
            currentCourseInstance.GetComponentInChildren<BallSpawnPoint>();

        if (spawn == null)
        {
            Debug.LogError("No BallSpawnPoint in course!");
            return;
        }

        ball.MoveToSpawnPoint(spawn.transform);
        Transform currentHole = GameObject.FindWithTag("Hole")?.transform;
        if (currentHole == null) Debug.LogError("Hole not found!");
        WorldGameManager.Instance.currentHolePosition = currentHole.position;
        timerRunning = true;
        isPaused = false;
        courseStartTime = Time.time;
    }


    public void OnCourseCompleted()
    {
        if (todaysChallengeType == FreeWorldChallengeType.FastestTime)
        {
            float rawTime = Time.time - courseStartTime;

            float timerMultiplier = 1f;

            if (CharacterManager.Instance != null)
                timerMultiplier = CharacterManager.Instance.GetTimerMultiplier();

            totalTime += rawTime * timerMultiplier;

            timerRunning = false;
        }

        currentCourseIndex++;

        bool isFinalCourse = currentCourseIndex >= dailyCourses.Count;
        if (isFinalCourse)
        {
            // Set BEFORE SaveProgress() so it persists the rollback state,
            // not the just-earned (unconfirmed) final totals.
            finalSubmissionPending = true;
        }

        SaveProgress();

        if (isFinalCourse)
        {
            _ = SubmitAndFetchLeaderboardAsync(); // async side-effect only
        }
        else
        {
            LoadCourse(currentCourseIndex);
        }
    }

    private void CompleteRunLocally()
    {
        PlayerPrefs.SetInt(SAVE_COMPLETE_KEY, 1);
        currentCourseIndex = dailyCourses.Count;
        SaveProgress();

        // Don't rely on the next Start()/CheckIfPlayBtnActive() to catch
        // this - if the player lands back on this same menu without the
        // scene reloading, playButton would otherwise stay enabled from
        // whatever state it was in before this run started.
        if (playButton != null) playButton.enabled = false;

        Debug.Log("Run completed and saved locally.");
    }

    private async Task SubmitAndFetchLeaderboardAsync()
    {
        HideErrorUI();
        loadingScene.SetActive(true);

        int leaderboardScore = ConvertToLeaderboardScore(todaysChallengeType, totalTime, totalStrokes);

        try
        {
            await UnityLeaderboardManager.SubmitScore(
                leaderboardScore, todaysChallengeType, dailyCourses.Count);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Leaderboard submission failed: " + e);
            loadingScene.SetActive(false);
            ShowErrorUI(submissionFailedMessage, OnSubmissionRetryClicked);
            return; // stays pending - retryButton (now pointed at submission retry) calls this method again
        }

        // Submission confirmed - the run is genuinely done now, regardless
        // of whether the rank lookup below succeeds.
        finalSubmissionPending = false;
        CompleteRunLocally();

        // The start panel's top-player/my-score texts only refresh on
        // scene load otherwise - this scene doesn't reload when returning
        // there after a run, so without this they'd stay stale until the
        // player leaves and re-enters. Fire-and-forget: it retries itself
        // in the background if it fails.
        uIController?.RequestLeaderboardRefresh();

        LeaderboardEntryData me = null;
        try
        {
            await Task.Delay(1500);
            var reader = FindFirstObjectByType<FreeWorldLeaderboardReader>();
            me = await reader.GetMyRankSafe(todaysChallengeType);
        }
        catch (System.Exception e)
        {
            // Score is already safely submitted - only the rank display
            // failed. Not a submission error, so no retry UI here.
            Debug.LogError("Rank fetch failed after a successful submission: " + e);
        }

        loadingScene.SetActive(false);

        if (me != null)
            FreeWorldEndPanel.Instance.Show(me);
        else
            FreeWorldEndPanel.Instance.ShowOfflineResult(totalTime, totalStrokes);
    }

    private async void OnSubmissionRetryClicked()
    {
        if (retryButton != null) retryButton.interactable = false;

        await SubmitAndFetchLeaderboardAsync();

        if (retryButton != null) retryButton.interactable = true;
    }

    public static int ConvertToLeaderboardScore(
    FreeWorldChallengeType type,
    float totalTime,
    int totalStrokes)
    {
        if (type == FreeWorldChallengeType.FastestTime)
        {
            int timeMs = Mathf.RoundToInt(totalTime * 1000f);
            return int.MaxValue - timeMs;
        }
        else
        {
            return int.MaxValue - totalStrokes;
        }
    }


    public void PauseTimer()
    {
        if (!timerRunning || isPaused) return;

        totalTime += Time.time - courseStartTime;
        isPaused = true;
    }

    public void ResumeTimer()
    {
        if (!isPaused) return;

        courseStartTime = Time.time;
        isPaused = false;
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        PauseTimer();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        ResumeTimer();
    }

    public void ExitFreeWorld()
    {
        PauseGame();

        if (currentCourseInstance != null)
            Destroy(currentCourseInstance);

        SaveProgress();
        timerRunning = false;
        Time.timeScale = 1f;
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            PauseGame();
            // This was the real gap: PauseGame() -> PauseTimer() updates
            // totalTime in memory, but nothing wrote it to PlayerPrefs.
            // On Android, backgrounding (not OnApplicationQuit) is the
            // reliable signal the app might be about to die - a
            // task-switcher swipe or a low-memory kill skips
            // OnApplicationQuit entirely, so this is the last guaranteed
            // chance to persist.
            SaveProgress();
        }
        else
        {
            ResumeGame();
        }
    }

    void OnApplicationQuit()
    {
        // Backup for platforms/paths where this does fire - OnApplicationPause
        // above is what actually protects Android against task-switcher kills.
        SaveProgress();
    }
}