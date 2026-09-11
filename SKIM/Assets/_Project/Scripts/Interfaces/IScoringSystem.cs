using System;

public interface IScoringSystem
{
    int SessionScore { get; }
    float CurrentMultiplier { get; }
    float SessionBestDistance { get; }
    // Points gained on the impact just processed by RegisterImpact — feeds the
    // per-skip score popup.
    int LastImpactScoreDelta { get; }
    void RegisterImpact(StoneState impactState, ClimateData climate);
    LaunchResult FinalizeLaunch(StoneState finalState, ClimateData climate);
    void ResetForNewLaunch();
    event Action<float> OnMultiplierChanged;
    event Action<LaunchResult> OnLaunchCompleted;
    event Action<float> OnNewSessionRecord;
}
