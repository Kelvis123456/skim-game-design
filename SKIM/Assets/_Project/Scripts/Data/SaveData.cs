using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public float TotalAccumulatedDistance;
    public float AllTimeRecord;
    public int AllTimeSessionScore;
    public int ConchaBalance;
    public string EquippedStoneId = "Guijarro";
    public List<string> UnlockedCosmeticIds = new();
    public Dictionary<string, string> EquippedSkins = new();
    public int TotalSessionCount;
    public DailyChallengeProgress LastDailyChallenge;
    public long LastSaveTimestamp;

    [System.Serializable]
    public class DailyChallengeProgress
    {
        public string ChallengeId;
        public float Progress;
        public bool Completed;
        public long DateTimestamp;
    }
}
