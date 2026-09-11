using UnityEngine;
using static IAudioSystem;

public interface IVFXSystem
{
    void SpawnWaterRing(Vector3 worldPosition, MusicalNote note);
    void SpawnImpactSplash(Vector3 worldPosition, float impactForce);
    void UpdateComboTrail(Transform stoneTransform, int comboLevel);
    void ClearComboTrail();
    void SpawnScorePopup(Vector3 worldPosition, int scoreValue, bool isCombo);
    void TriggerNewRecordEffect(float recordDistance);
    void TriggerChordResolutionPulse();
    void ClearSessionRings();
    void UpdatePBLine(float distance);
    void ShowTrajectoryArc(Vector3[] points);
    void HideTrajectoryArc();
}
