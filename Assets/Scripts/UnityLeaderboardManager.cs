using Unity.Services.Leaderboards;
using UnityEngine;
using System.Threading.Tasks;

public static class UnityLeaderboardManager
{
    public static async Task SubmitScore(
     int score,
     FreeWorldChallengeType challengeType,
     int coursesCompleted)
    {
        string leaderboardId = GetLeaderboardId(challengeType);

        var metadata = new
        {
            challenge = challengeType.ToString(),
            courses = coursesCompleted
        };

        await LeaderboardsService.Instance.AddPlayerScoreAsync(
            leaderboardId,
            score,
            new AddPlayerScoreOptions
            {
                Metadata = metadata
            }
        );

        Debug.Log("Score submitted to leaderboard: " + leaderboardId);
    }

    private static string GetLeaderboardId(FreeWorldChallengeType type)
    {
        return type == FreeWorldChallengeType.FastestTime
            ? "freeworld_fastest_time"
            : "freeworld_lowest_strokes";
    }
}
