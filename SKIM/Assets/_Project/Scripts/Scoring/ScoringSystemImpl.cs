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
    int _bonusPoints;
    float _bonusMultiplierAdd;
    float _rainbowMultiplierBoost = 1f;
    float _maxMultiplier;
    float _sessionAllTimeRecord;

    // GDD §7.3 — Green/Blue/Gold add flat points, Blue/Gold also add to the multiplier;
    // Rainbow ("Especial", very rare) adds no points and instead doubles the multiplier
    // for the rest of THIS launch. Returns the points just awarded (0 for Rainbow) so the
    // caller can size a score popup without re-deriving the reward table.
    public int RegisterBonusZoneHit(BonusZoneCategory category)
    {
        _bonusZones++;
        int points;
        switch (category)
        {
            case BonusZoneCategory.Green: points = 200; break;
            case BonusZoneCategory.Blue: points = 500; _bonusMultiplierAdd += 0.3f; break;
            case BonusZoneCategory.Gold: points = 1500; _bonusMultiplierAdd += 0.8f; break;
            case BonusZoneCategory.Rainbow: points = 0; _rainbowMultiplierBoost = 2f; break;
            default: points = 0; break;
        }
        _bonusPoints += points;
        return points;
    }

    public void RegisterImpact(StoneState state, ClimateData climate)
    {
        CurrentMultiplier = (StoneSimulatorImpl.GetMultiplier(state.SkipCount) + _bonusMultiplierAdd) * _rainbowMultiplierBoost;
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
                                     + _bonusPoints
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
        _bonusPoints = 0;
        _bonusMultiplierAdd = 0f;
        _rainbowMultiplierBoost = 1f;
        _maxMultiplier = 1f;
        CurrentMultiplier = 1f;
    }

    public void SetAllTimeRecord(float record) => _sessionAllTimeRecord = record;
}
