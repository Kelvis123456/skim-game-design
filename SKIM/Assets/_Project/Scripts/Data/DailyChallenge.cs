// Plain value carried by IProgressionSystem — a lighter-weight stand-in for
// DailyChallengeData (a ScriptableObject) until the game has more than one
// challenge and a real rotation needs authoring in the editor.
public readonly struct DailyChallenge
{
    public readonly DailyChallengeData.ChallengeType Type;
    public readonly float Target;
    public readonly int ConchaReward;
    public readonly string Description;

    public DailyChallenge(DailyChallengeData.ChallengeType type, float target, int conchaReward, string description)
    {
        Type = type;
        Target = target;
        ConchaReward = conchaReward;
        Description = description;
    }
}
