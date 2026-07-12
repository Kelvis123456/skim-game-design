using UnityEngine;

[CreateAssetMenu(menuName = "SKIM/Daily Challenge", fileName = "DailyChallengeData")]
public class DailyChallengeData : ScriptableObject
{
    public ChallengeType Type;
    public float Target;
    public int ConchaReward;
    [TextArea] public string Description;
    public string DescriptionEn;

    public enum ChallengeType
    {
        Distance,
        SkipCount,
        ConsecutiveSkips,
        TotalSessionDistance,
        ClimateChallenge
    }
}
