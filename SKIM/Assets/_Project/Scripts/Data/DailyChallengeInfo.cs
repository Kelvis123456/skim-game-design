public enum DailyChallengeType { DistanceSingleThrow, SkipsSingleThrow, SessionDistance }

// Read-only snapshot handed to UI — the mutable tracking lives in SaveData.DailyChallengeProgress.
public readonly struct DailyChallengeInfo
{
    public readonly DailyChallengeType Type;
    public readonly string Description;
    public readonly float Progress;
    public readonly float Target;
    public readonly int RewardConchas;
    public readonly bool Completed;
    public readonly System.TimeSpan TimeUntilReset;

    public DailyChallengeInfo(DailyChallengeType type, string description, float progress, float target,
                              int rewardConchas, bool completed, System.TimeSpan timeUntilReset)
    {
        Type = type;
        Description = description;
        Progress = progress;
        Target = target;
        RewardConchas = rewardConchas;
        Completed = completed;
        TimeUntilReset = timeUntilReset;
    }
}
