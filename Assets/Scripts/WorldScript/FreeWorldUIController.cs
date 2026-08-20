using System;
using System.Threading;
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

    private CancellationTokenSource refreshCts;
    private const int INITIAL_BACKOFF_MS = 1000;
    private const int MAX_BACKOFF_MS = 30000;

    async void Start()
    {
        await Task.Yield();

        UpdateStaticTexts();
        await UGSInitializer.WaitUntilAuthReady();

        RequestLeaderboardRefresh();
    }

    void OnDestroy()
    {
        refreshCts?.Cancel();
    }

    void Update()
    {
        UpdateDynamicTexts();
    }

    /// <summary>
    /// Call this any time the on-screen leaderboard values might be
    /// stale - not just on scene load. In particular, call this when the
    /// player returns to the start panel after finishing a run, since
    /// that doesn't reload the scene and Start() won't run again.
    ///
    /// Cancels any refresh already retrying in the background first, so
    /// only one retry loop is ever active - calling this repeatedly
    /// (e.g. once per run) doesn't stack up duplicate loops.
    /// </summary>
    public void RequestLeaderboardRefresh()
    {
        refreshCts?.Cancel();
        refreshCts = new CancellationTokenSource();
        _ = RefreshWithRetryLoop(refreshCts.Token);
    }

    private async Task RefreshWithRetryLoop(CancellationToken token)
    {
        int delayMs = INITIAL_BACKOFF_MS;

        while (!token.IsCancellationRequested)
        {
            bool succeeded = await TryRefreshOnce();

            if (succeeded)
                return;

            try
            {
                await Task.Delay(delayMs, token);
            }
            catch (TaskCanceledException)
            {
                return; // a newer refresh request superseded this loop
            }

            delayMs = Mathf.Min(delayMs * 2, MAX_BACKOFF_MS);
        }
    }

    private async Task<bool> TryRefreshOnce()
    {
        if (reader == null || runManager == null)
            return false;

        try
        {
            await GetTopPlayers();
            await GetMyRankAndScore();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Leaderboard refresh failed, will retry in background: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Single-attempt refresh, no retry. Kept for any caller that wants
    /// to await one specific attempt rather than kick off a background
    /// loop - most callers should use RequestLeaderboardRefresh() instead.
    /// </summary>
    public async Task<bool> RefreshLeaderboardUIOnce()
    {
        return await TryRefreshOnce();
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
        if (WorldGameManager.Instance.isGameStarted)
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