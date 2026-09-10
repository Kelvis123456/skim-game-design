using System;

public interface IScoringSystem
{
    int SessionScore { get; }
    float CurrentMultiplier { get; }
    float SessionBestDistance { get; }
    void RegisterImpact(StoneState impactState, ClimateData climate);
    void RegisterBonusZoneHit(BonusZoneCategory category);
    LaunchResult FinalizeLaunch(StoneState finalState, ClimateData climate);
    void ResetForNewLaunch();
    event Action<float> OnMultiplierChanged;
    event Action<LaunchResult> OnLaunchCompleted;
    event Action<float> OnNewSessionRecord;
}
