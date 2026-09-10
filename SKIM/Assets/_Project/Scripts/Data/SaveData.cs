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
    public bool VibrationEnabled = true;
    public bool ReduceEffects;

    // A list, not a Dictionary — JsonUtility silently serializes Dictionary
    // fields as empty (see EquippedSkins above, which already hits this).
    public List<StatRecord> StoneRecords = new();
    public List<StatRecord> ClimateRecords = new();

    [System.Serializable]
    public class StatRecord
    {
        public string Id;
        public float BestDistance;
    }

    [System.Serializable]
    public class DailyChallengeProgress
    {
        public string ChallengeId;
        public float Progress;
        public bool Completed;
        public long DateTimestamp;
    }
}
