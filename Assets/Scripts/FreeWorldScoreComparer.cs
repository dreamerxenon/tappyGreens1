using System.Collections.Generic;

public class FreeWorldScoreComparer : IComparer<FreeWorldRunResult>
{
    public int Compare(FreeWorldRunResult a, FreeWorldRunResult b)
    {
        if (a.challengeType == FreeWorldChallengeType.FastestTime)
        {
            return a.totalTime.CompareTo(b.totalTime);
        }
        else
        {
            return a.totalStrokes.CompareTo(b.totalStrokes);
        }
    }
}