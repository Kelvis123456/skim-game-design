using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ProgressionSystemImpl : MonoBehaviour, IProgressionSystem
{
    StoneData[] _allStones;
    ClimateData[] _allClimates;

    SaveData _data = new();
    const string FILENAME = "skim_save.json";

    void Awake()
    {
        _allStones   = Resources.LoadAll<StoneData>("Data/Stones")
                                .OrderBy(s => s.UnlockDistanceMeters).ToArray();
        _allClimates = Resources.LoadAll<ClimateData>("Data/Climates")
                                .OrderBy(c => c.UnlockDistanceMeters).ToArray();
    }

    public float TotalAccumulatedDistance => _data.TotalAccumulatedDistance;
    public float AllTimeRecord => _data.AllTimeRecord;
    public int AllTimeSessionScore => _data.AllTimeSessionScore;
    public int TotalSessionCount => _data.TotalSessionCount;

    public IReadOnlyList<StoneData> AvailableStones => _allStones;
    public IReadOnlyList<ClimateData> AvailableClimates => _allClimates;

    StoneData _selectedStone;
    ClimateData _selectedClimate;

    public StoneData SelectedStone
    {
        get => _selectedStone ?? (_allStones.Length > 0 ? _allStones[0] : null);
        set => _selectedStone = value;
    }

    public ClimateData SelectedClimate
    {
        get => _selectedClimate ?? (_allClimates.Length > 0 ? _allClimates[0] : null);
        set => _selectedClimate = value;
    }

    public bool IsStoneUnlocked(StoneData stone) =>
        stone != null && _data.TotalAccumulatedDistance >= stone.UnlockDistanceMeters;

    public bool IsClimateUnlocked(ClimateData climate) =>
        climate != null && _data.TotalAccumulatedDistance >= climate.UnlockDistanceMeters;

    public void RegisterLaunch(LaunchResult result)
    {
        _data.TotalAccumulatedDistance += result.Distance;
        if (result.Distance > _data.AllTimeRecord) _data.AllTimeRecord = result.Distance;
    }

    // Only one challenge exists today (fase5-uxui's "recorre 100m" mock); this is
    // the seam where a real daily rotation would plug in later.
    static readonly DailyChallenge DefaultDailyChallenge = new(
        DailyChallengeData.ChallengeType.Distance, 100f, 120,
        "Recorre 100m en una sola tirada");

    public DailyChallenge CurrentDailyChallenge => DefaultDailyChallenge;
    public float DailyChallengeProgress => _data.LastDailyChallenge?.Progress ?? 0f;
    public bool DailyChallengeCompleted => _data.LastDailyChallenge?.Completed ?? false;
    public SaveData RawData => _data;

    public void RegisterDailyChallengeLaunch(LaunchResult result)
    {
        EnsureDailyChallengeFreshness();
        var progress = _data.LastDailyChallenge;
        if (progress.Completed) return;

        if (result.Distance > progress.Progress)
            progress.Progress = Mathf.Min(result.Distance, CurrentDailyChallenge.Target);

        if (progress.Progress >= CurrentDailyChallenge.Target)
        {
            progress.Completed = true;
            if (ServiceLocator.TryGet<IEconomySystem>(out var economy))
                economy.EarnConchas(CurrentDailyChallenge.ConchaReward, "daily_challenge");
        }
    }

    // Resets progress when the stored challenge is from a previous UTC day.
    void EnsureDailyChallengeFreshness()
    {
        var today = System.DateTimeOffset.UtcNow.UtcDateTime.Date;
        var lastDate = _data.LastDailyChallenge != null
            ? System.DateTimeOffset.FromUnixTimeSeconds(_data.LastDailyChallenge.DateTimestamp).UtcDateTime.Date
            : System.DateTime.MinValue;

        if (_data.LastDailyChallenge == null || lastDate != today)
        {
            _data.LastDailyChallenge = new SaveData.DailyChallengeProgress
            {
                ChallengeId = "distance_100",
                DateTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
        }
    }

    public void Save()
    {
        _data.LastSaveTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string json = JsonUtility.ToJson(_data, false);
        File.WriteAllText(SavePath, json);
    }

    public void Load()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
        }
        catch { _data = new SaveData(); }

        _data.TotalSessionCount++;
        EnsureDailyChallengeFreshness();
    }

    string SavePath => Path.Combine(Application.persistentDataPath, FILENAME);

    void OnApplicationPause(bool p) { if (p) Save(); }
    void OnApplicationQuit() => Save();
}
