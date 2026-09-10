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

    // (type, target, rewardConchas, description) — rotates by day-of-epoch so every
    // player sees the same challenge on a given day without needing a server.
    static readonly (DailyChallengeType type, float target, int reward, string desc)[] CHALLENGE_TEMPLATES =
    {
        (DailyChallengeType.DistanceSingleThrow, 100f, 120, "Recorre 100m en una sola tirada"),
        (DailyChallengeType.SkipsSingleThrow,     15f,  100, "Logra 15 rebotes en una sola tirada"),
        (DailyChallengeType.SessionDistance,      300f, 150, "Acumula 300m de distancia hoy"),
    };

    public SaveData Data => _data;

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

    public bool VibrationEnabled
    {
        get => _data.VibrationEnabled;
        set => _data.VibrationEnabled = value;
    }

    public bool ReduceEffects
    {
        get => _data.ReduceEffects;
        set => _data.ReduceEffects = value;
    }

    public bool PermanentAssist
    {
        get => _data.PermanentAssist;
        set => _data.PermanentAssist = value;
    }

    public void RegisterLaunch(LaunchResult result)
    {
        _data.TotalAccumulatedDistance += result.Distance;
        if (result.Distance > _data.AllTimeRecord) _data.AllTimeRecord = result.Distance;
        _data.AllTimeSessionScore += result.Score;
        UpdateDailyChallenge(result);

        if (_selectedStone != null) UpdateRecord(_data.StoneRecords, _selectedStone.StoneName, result.Distance);
        if (_selectedClimate != null) UpdateRecord(_data.ClimateRecords, _selectedClimate.ClimateName, result.Distance);
        if (result.SkipCount > _data.AllTimeBestSkipCount) _data.AllTimeBestSkipCount = result.SkipCount;

        EvaluateAchievements();
    }

    static void UpdateRecord(List<SaveData.StatRecord> records, string id, float distance)
    {
        var rec = records.Find(r => r.Id == id);
        if (rec == null) { records.Add(new SaveData.StatRecord { Id = id, BestDistance = distance }); return; }
        if (distance > rec.BestDistance) rec.BestDistance = distance;
    }

    public float BestDistanceForStone(string stoneId) =>
        _data.StoneRecords.Find(r => r.Id == stoneId)?.BestDistance ?? 0f;

    public float BestDistanceForClimate(string climateId) =>
        _data.ClimateRecords.Find(r => r.Id == climateId)?.BestDistance ?? 0f;

    // ─────────────────────────── DAILY CHALLENGE ───────────────────────

    static long TodayIndex() => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400L;

    void EnsureDailyChallenge()
    {
        _data.LastDailyChallenge ??= new SaveData.DailyChallengeProgress();
        long today = TodayIndex();
        if (_data.LastDailyChallenge.DateTimestamp == today) return;

        int idx = (int)(today % CHALLENGE_TEMPLATES.Length);
        _data.LastDailyChallenge.ChallengeId = idx.ToString();
        _data.LastDailyChallenge.Progress = 0f;
        _data.LastDailyChallenge.Completed = false;
        _data.LastDailyChallenge.DateTimestamp = today;
    }

    public DailyChallengeInfo CurrentDailyChallenge
    {
        get
        {
            EnsureDailyChallenge();
            var p = _data.LastDailyChallenge;
            var def = CHALLENGE_TEMPLATES[int.Parse(p.ChallengeId)];
            long secondsIntoDay = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 86400L;
            var reset = System.TimeSpan.FromSeconds(86400L - secondsIntoDay);
            return new DailyChallengeInfo(def.type, def.desc, p.Progress, def.target, def.reward, p.Completed, reset);
        }
    }

    void UpdateDailyChallenge(LaunchResult result)
    {
        EnsureDailyChallenge();
        var p = _data.LastDailyChallenge;
        if (p.Completed) return;

        var def = CHALLENGE_TEMPLATES[int.Parse(p.ChallengeId)];
        p.Progress = def.type switch
        {
            DailyChallengeType.DistanceSingleThrow => Mathf.Max(p.Progress, result.Distance),
            DailyChallengeType.SkipsSingleThrow    => Mathf.Max(p.Progress, result.SkipCount),
            DailyChallengeType.SessionDistance     => p.Progress + result.Distance,
            _ => p.Progress
        };

        if (p.Progress >= def.target)
        {
            p.Completed = true;
            _data.TotalDailyChallengesCompleted++;
            if (ServiceLocator.TryGet<IEconomySystem>(out var economy))
                economy.EarnConchas(def.reward, "daily_challenge");
        }
    }

    // ─────────────────────────── ACHIEVEMENTS ───────────────────────────

    public event System.Action<AchievementDefinition> OnAchievementUnlocked;

    public bool IsAchievementUnlocked(string achievementId) =>
        _data.UnlockedAchievementIds.Contains(achievementId);

    void EvaluateAchievements()
    {
        foreach (var def in AchievementCatalog.All)
        {
            if (_data.UnlockedAchievementIds.Contains(def.Id)) continue;

            float value = def.Metric switch
            {
                AchievementMetric.TotalDistance             => _data.TotalAccumulatedDistance,
                AchievementMetric.BestDistance               => _data.AllTimeRecord,
                AchievementMetric.BestSkipCount               => _data.AllTimeBestSkipCount,
                AchievementMetric.DailyChallengesCompleted   => _data.TotalDailyChallengesCompleted,
                AchievementMetric.StonesUnlocked             => _allStones.Count(IsStoneUnlocked),
                AchievementMetric.ClimatesUnlocked           => _allClimates.Count(IsClimateUnlocked),
                _ => 0f
            };

            // Unlock counts compare against how many exist right now rather than the
            // catalog's static Target, so adding a stone/climate later doesn't strand it.
            float target = def.Metric switch
            {
                AchievementMetric.StonesUnlocked   => _allStones.Length,
                AchievementMetric.ClimatesUnlocked => _allClimates.Length,
                _ => def.Target
            };

            if (value >= target)
            {
                _data.UnlockedAchievementIds.Add(def.Id);
                OnAchievementUnlocked?.Invoke(def);
            }
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
    }

    public void ResetData()
    {
        _data = new SaveData();
        _selectedStone = null;
        _selectedClimate = null;
        Save();
    }

    string SavePath => Path.Combine(Application.persistentDataPath, FILENAME);

    void OnApplicationPause(bool p) { if (p) Save(); }
    void OnApplicationQuit() => Save();
}
