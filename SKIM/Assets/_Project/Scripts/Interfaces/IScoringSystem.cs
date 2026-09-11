using System;

public interface IScoringSystem
{
    int SessionScore { get; }
    float CurrentMultiplier { get; }
    float SessionBestDistance { get; }
    void RegisterImpact(StoneState impactState, ClimateData climate);
    // Returns the flat points just awarded (0 for Rainbow, which grants a multiplier boost instead).
    int RegisterBonusZoneHit(BonusZoneCategory category);
    LaunchResult FinalizeLaunch(StoneState finalState, ClimateData climate);
    void ResetForNewLaunch();
    event Action<float> OnMultiplierChanged;
    event Action<LaunchResult> OnLaunchCompleted;
    event Action<float> OnNewSessionRecord;
}
