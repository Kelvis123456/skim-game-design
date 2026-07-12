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

    string SavePath => Path.Combine(Application.persistentDataPath, FILENAME);

    void OnApplicationPause(bool p) { if (p) Save(); }
    void OnApplicationQuit() => Save();
}
