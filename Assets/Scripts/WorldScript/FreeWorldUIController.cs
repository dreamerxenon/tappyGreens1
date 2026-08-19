using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class FreeWorldUIController : MonoBehaviour
{
    public TextMeshProUGUI challengeDecriptionText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI strokesText;
    public TextMeshProUGUI courseNumberText;
    public TextMeshProUGUI dailyCountdownText;
    public TextMeshProUGUI topPlayerScoreText;
    public TextMeshProUGUI myScoreText;
    public FreeWorldRunManager runManager;
    public FreeWorldLeaderboardReader reader;
   async void Start()
    {

        await Task.Yield();

        UpdateStaticTexts();
        await UGSInitializer.WaitUntilAuthReady();
        await RefreshLeaderboardUI();
    }

    void Update()
    {
        UpdateDynamicTexts();
    }

    public async Task RefreshLeaderboardUI()
    {
        if (reader == null || runManager == null)
            return;

        try
        {
            await GetTopPlayers();
            await GetMyRankAndScore();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Leaderboard not ready yet: " + e.Message);
        }
    }

    public void UpdateStaticTexts()
    {
        challengeDecriptionText.text =
            runManager.ChallengeType == FreeWorldChallengeType.FastestTime
            ? "Fastest Time"
            : "Lowest Strokes";
        switch (runManager.ChallengeType)
        {
            case FreeWorldChallengeType.FastestTime:
                WorldGameManager.Instance.fastestTime = true;
                break;
            case FreeWorldChallengeType.LowestStrokes:
                WorldGameManager.Instance.lowestStrokes = true;
                break;
        }
        Debug.Log(runManager.ChallengeType);
    }

    void UpdateDynamicTexts()
    {
        // Time
        timerText.text = FormatTime(runManager.CurrentTime);

        // Strokes
        strokesText.text = runManager.CurrentStrokes.ToString();

        // Course progress
        if(WorldGameManager.Instance.isGameStarted)
        courseNumberText.text = $"{runManager.CurrentCourseNumber} / {runManager.TotalCourses}";


        // Daily reset countdown
        TimeSpan remaining = DailyResetManager.Instance.TimeUntilReset();
        dailyCountdownText.text =
            $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
    }

    string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return t.ToString(@"mm\:ss");
    }

    public async Task GetTopPlayers()
    {
        var topPlayers = await reader.GetTopPlayers(
            runManager.ChallengeType,
            1
        );

        if (topPlayers.Count > 0)
        {
            topPlayerScoreText.text =
                FormatLeaderboardScore(topPlayers[0].Score);
        }
        else
        {
            topPlayerScoreText.text = "----";
        }
    }



    public async Task GetMyRankAndScore()
    {
        var me = await reader.GetMyRankSafe(
            runManager.ChallengeType
        );

        if (me != null)
        {
            myScoreText.text = FormatLeaderboardScore(me.Score);
        }
        else
        {
            myScoreText.text = "----";
        }
    }


    public async Task GetFullLeaderBoard()
    {
       
        FreeWorldChallengeType todaysChallengeType = runManager.ChallengeType;
        int offset = 0;
        int pageSize = 20;

        var page = await reader.GetLeaderboardPage(
            todaysChallengeType,
            offset,
            pageSize
        );

    }

    string FormatLeaderboardScore(int score)
    {
        if (runManager.ChallengeType == FreeWorldChallengeType.FastestTime)
        {
           
            float seconds = score / 1000f;
            return seconds.ToString("0.00") + "s";
        }
        else
        {
           
            return score + " strokes";
        }
    }

}
