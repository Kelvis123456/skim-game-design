public readonly struct LaunchResult
{
    public readonly float Distance;
    public readonly int SkipCount;
    public readonly int Score;
    public readonly float MaxMultiplier;
    public readonly bool IsNewSessionRecord;
    public readonly bool IsNewAllTimeRecord;

    public LaunchResult(float distance, int skipCount, int score, float maxMultiplier,
                        bool isNewSessionRecord, bool isNewAllTimeRecord)
    {
        Distance = distance;
        SkipCount = skipCount;
        Score = score;
        MaxMultiplier = maxMultiplier;
        IsNewSessionRecord = isNewSessionRecord;
        IsNewAllTimeRecord = isNewAllTimeRecord;
    }
}
