using System.Collections.Generic;

public interface IProgressionSystem
{
    float TotalAccumulatedDistance { get; }
    float AllTimeRecord { get; }
    int AllTimeSessionScore { get; }
    int TotalSessionCount { get; }
    void RegisterLaunch(LaunchResult result);
    bool IsStoneUnlocked(StoneData stone);
    bool IsClimateUnlocked(ClimateData climate);
    IReadOnlyList<StoneData> AvailableStones { get; }
    IReadOnlyList<ClimateData> AvailableClimates { get; }
    StoneData SelectedStone { get; set; }
    ClimateData SelectedClimate { get; set; }

    DailyChallenge CurrentDailyChallenge { get; }
    float DailyChallengeProgress { get; }
    bool DailyChallengeCompleted { get; }
    void RegisterDailyChallengeLaunch(LaunchResult result);

    // Exposed so GameBootstrapper can hand the same save blob to IEconomySystem —
    // conchas otherwise never persist between sessions.
    SaveData RawData { get; }

    void Save();
    void Load();
}
