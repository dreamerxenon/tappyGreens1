using TMPro;
using UnityEngine;

public class LeaderboardRowUi : MonoBehaviour
{
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    private FreeWorldRunManager runManager; 

    void Awake()
    {
        runManager = FindFirstObjectByType<FreeWorldRunManager>();
    }
    public void Setup(LeaderboardEntryData data, bool isPlayer)
    {
        rankText.text = data.Rank.ToString();
        nameText.text = data.PlayerName;
        scoreText.text = FormatLeaderboardScore(data.Score);

        if (isPlayer)
        {
            nameText.color = Color.yellow;
            rankText.color = Color.yellow;
            scoreText.color = Color.yellow;
        }
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
