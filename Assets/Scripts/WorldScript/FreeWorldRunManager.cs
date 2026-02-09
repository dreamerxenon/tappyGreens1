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
    private List<CourseData> dailyCourses;
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
    public FreeWorldChallengeType ChallengeType => todaysChallengeType;

    public int CurrentCourseNumber => currentCourseIndex + 1;
    public int TotalCourses => dailyCourses.Count;

    public float CurrentTime
    {
        get
        {
            if (!timerRunning) return totalTime;
            if (isPaused) return totalTime;

            return totalTime + (Time.time - courseStartTime);
        }
    }

    public int CurrentStrokes => totalStrokes;



    void Start()
    {

        GenerateDailyCourses();
        CheckIfPlayBtnActive();

    }

    private void CheckIfPlayBtnActive()
    {
        string today = System.DateTime.UtcNow.ToString("yyyyMMdd");
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
    }
    private void Update()
    {
       
    }

    public void UpdateStrokes(int index)
    {
        totalStrokes += index;
    }
    public void StartGame()
    {
       
        LoadProgressOrStartNew();
    }
    void LoadProgressOrStartNew()
    {
        string today = System.DateTime.UtcNow.ToString("yyyyMMdd");

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

                LoadCourse(currentCourseIndex);
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

        LoadCourse(currentCourseIndex);
    }


    void GenerateDailyCourses()
    {
        int seed = int.Parse(System.DateTime.UtcNow.ToString("yyyyMMdd"));
        Random.InitState(seed);

        todaysChallengeType = Random.value < 0.5f
    ? FreeWorldChallengeType.LowestStrokes
    : FreeWorldChallengeType.FastestTime;

        
        List<CourseData> shuffled = new List<CourseData>(courseDatabase.allCourses);

        for (int i = 0; i < shuffled.Count; i++)
        {
            int j = Random.Range(i, shuffled.Count);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        dailyCourses = shuffled.GetRange(0, 9);
        dailyCourses.Sort((a, b) => a.difficulty.CompareTo(b.difficulty));

        uIController.UpdateStaticTexts();
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

        timerRunning = true;
        isPaused = false;
        courseStartTime = Time.time;
    }



    public void OnCourseCompleted()
    {
        if (todaysChallengeType == FreeWorldChallengeType.FastestTime)
        {
            totalTime += Time.time - courseStartTime;
            timerRunning = false;
        }
        
        currentCourseIndex++;
        PlayerPrefs.SetInt(SAVE_INDEX_KEY, currentCourseIndex);
        PlayerPrefs.SetFloat(SAVE_TOTAL_TIME_KEY, totalTime);
        PlayerPrefs.SetInt(SAVE_TOTAL_STROKES_KEY, totalStrokes);
        PlayerPrefs.Save();

        if (currentCourseIndex >= dailyCourses.Count)
        {
            CompleteRunLocally();   // ← new
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

        if(currentCourseInstance != null)
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
