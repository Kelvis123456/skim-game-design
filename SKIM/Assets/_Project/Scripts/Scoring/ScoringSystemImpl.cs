using System;
using UnityEngine;

public class ScoringSystemImpl : MonoBehaviour, IScoringSystem
{
    public int SessionScore { get; private set; }
    public float CurrentMultiplier { get; private set; } = 1f;
    public float SessionBestDistance { get; private set; }

    public event Action<float> OnMultiplierChanged;
    public event Action<LaunchResult> OnLaunchCompleted;
    public event Action<float> OnNewSessionRecord;

    int _launchScore;
    int _bonusZones;
    float _maxMultiplier;
    float _sessionAllTimeRecord;

    public void RegisterImpact(StoneState state, ClimateData climate)
    {
        CurrentMultiplier = StoneSimulatorImpl.GetMultiplier(state.SkipCount);
        if (CurrentMultiplier > _maxMultiplier) _maxMultiplier = CurrentMultiplier;
        OnMultiplierChanged?.Invoke(CurrentMultiplier);

        float climateBonus = climate != null ? climate.ClimateMultiplier - 1f : 0f;
        int pts = Mathf.RoundToInt(state.TotalDistance * 10f * CurrentMultiplier);
        pts += Mathf.RoundToInt(pts * climateBonus);
        _launchScore = pts;
    }

    public LaunchResult FinalizeLaunch(StoneState final, ClimateData climate)
    {
        float climateBonus = climate != null ? climate.ClimateMultiplier - 1f : 0f;
        int score = Mathf.RoundToInt(final.TotalDistance * 10f * CurrentMultiplier
                                     + _bonusZones * 500
                                     + final.TotalDistance * 10f * CurrentMultiplier * climateBonus);

        SessionScore += score;

        bool newSession = final.TotalDistance > SessionBestDistance;
        bool newAllTime = final.TotalDistance > _sessionAllTimeRecord;

        if (newSession)
        {
            SessionBestDistance = final.TotalDistance;
            OnNewSessionRecord?.Invoke(final.TotalDistance);
        }
        if (newAllTime) _sessionAllTimeRecord = final.TotalDistance;

        var result = new LaunchResult(final.TotalDistance, final.SkipCount, score,
                                      _maxMultiplier, newSession, newAllTime);
        OnLaunchCompleted?.Invoke(result);
        return result;
    }

    public void ResetForNewLaunch()
    {
        _launchScore = 0;
        _bonusZones = 0;
        _maxMultiplier = 1f;
        CurrentMultiplier = 1f;
    }

    public void SetAllTimeRecord(float record) => _sessionAllTimeRecord = record;
}
