using UnityEngine;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FreeWorldLeaderboardReader : MonoBehaviour
{
    /* =====================================================
     *  PUBLIC API
     * ===================================================== */

    // Top N players
    public async Task<List<LeaderboardEntryData>> GetTopPlayers(
        FreeWorldChallengeType challengeType,
        int limit)
    {
        await UGSInitializer.WaitUntilReady();

        var scores = await LeaderboardsService.Instance.GetScoresAsync(
            GetLeaderboardId(challengeType),
            new GetScoresOptions { Limit = limit }
        );

        return ConvertResults(challengeType, scores.Results);
    }

    // Current player rank + score (SAFE)
    public async Task<LeaderboardEntryData?> GetMyRankSafe(
        FreeWorldChallengeType challengeType)
    {
        await UGSInitializer.WaitUntilReady();

        try
        {
            var result = await LeaderboardsService.Instance
                .GetPlayerScoreAsync(GetLeaderboardId(challengeType));

            return new LeaderboardEntryData
            {
                Rank = result.Rank + 1,
                PlayerId = result.PlayerId,
                PlayerName = result.PlayerName,
                Score = DecodeScore(challengeType, (int)result.Score)
            };
        }
        catch
        {
            // Player has not submitted today
            return null;
        }
    }

    // Paginated leaderboard
    public async Task<List<LeaderboardEntryData>> GetLeaderboardPage(
        FreeWorldChallengeType challengeType,
        int offset,
        int limit)
    {
        await UGSInitializer.WaitUntilReady();

        var scores = await LeaderboardsService.Instance.GetScoresAsync(
            GetLeaderboardId(challengeType),
            new GetScoresOptions
            {
                Offset = offset,
                Limit = limit
            }
        );

        return ConvertResults(challengeType, scores.Results);
    }

    /* =====================================================
     *  INTERNAL
     * ===================================================== */

    private string GetLeaderboardId(FreeWorldChallengeType type)
    {
        return type == FreeWorldChallengeType.FastestTime
            ? "freeworld_fastest_time"
            : "freeworld_lowest_strokes";
    }

    private int DecodeScore(
        FreeWorldChallengeType type,
        int leaderboardScore)
    {
        // You are using score inversion
        return int.MaxValue - leaderboardScore;
    }

    private List<LeaderboardEntryData> ConvertResults(
        FreeWorldChallengeType challengeType,
        IList<LeaderboardEntry> results)
    {
        List<LeaderboardEntryData> list = new();

        foreach (var entry in results)
        {
            list.Add(new LeaderboardEntryData
            {
                Rank = entry.Rank + 1,
                PlayerId = entry.PlayerId,
                PlayerName = entry.PlayerName,
                Score = DecodeScore(challengeType, (int)entry.Score)
            });
        }

        return list;
    }
}

/* =====================================================
 *  DATA CLASSES
 * ===================================================== */

[Serializable]
public class LeaderboardEntryData
{
    public int Rank;
    public string PlayerId;
    public string PlayerName;
    public int Score;
}