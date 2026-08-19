using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Services.Leaderboards;
using TMPro;
using System.Threading.Tasks;


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
            Debug.LogError("Could not load Daily Free World after retry. Play stays disabled.");
            if (playButton != null) playButton.enabled = false;
            return;
        }

        CheckIfPlayBtnActive();
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
        PlayerPrefs.SetInt(SAVE_INDEX_KEY, currentCourseIndex);
        PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, totalTime);
        PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, totalStrokes);
        PlayerPrefs.Save();

        if (currentCourseIndex >= dailyCourses.Count)
        {
            CompleteRunLocally();
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
        PlayerPrefs.SetInt(SAVE_INDEX_KEY, dailyCourses.Count);
        PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, totalTime);
        PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, totalStrokes);
        PlayerPrefs.Save();

        Debug.Log("Run completed and saved locally.");
    }

    private async Task SubmitAndFetchLeaderboardAsync()
    {
        loadingScene.SetActive(true);
        try
        {
            var result = await GetTotalScores();
            loadingScene.SetActive(false);
            FreeWorldEndPanel.Instance.Show(result);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Leaderboard failed, but run is complete: " + e);
            loadingScene.SetActive(false);
            FreeWorldEndPanel.Instance.ShowOfflineResult(
                totalTime,
                totalStrokes
            );
        }
    }


    private async Task<LeaderboardEntryData> GetTotalScores()
    {
        int leaderboardScore =
            ConvertToLeaderboardScore(
                todaysChallengeType,
                totalTime,
                totalStrokes
            );

        // MUST await submission
        await UnityLeaderboardManager.SubmitScore(
            leaderboardScore,
            todaysChallengeType,
            dailyCourses.Count
        );

        // Small delay to allow leaderboard to update
        await Task.Delay(1500);

        var reader = FindFirstObjectByType<FreeWorldLeaderboardReader>();
        var me = await reader.GetMyRankSafe(todaysChallengeType);

        return me;
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

        PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, totalTime);
        PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, totalStrokes);
        PlayerPrefs.Save();
        timerRunning = false;
        Time.timeScale = 1f;
    }

    void OnApplicationPause(bool pause)
    {
        if (pause)
            PauseGame();
        else
            ResumeGame();
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, totalTime);
        PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, totalStrokes);
        PlayerPrefs.Save();
    }
}