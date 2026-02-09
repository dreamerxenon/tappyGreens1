[System.Serializable]
public class FreeWorldRunResult
{
    public string playerId;
    public string date; // yyyyMMdd
    public FreeWorldChallengeType challengeType;

    public int totalStrokes;
    public float totalTime;

    public int coursesCompleted;
}
