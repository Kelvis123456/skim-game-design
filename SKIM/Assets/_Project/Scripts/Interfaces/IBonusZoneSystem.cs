using System;
using UnityEngine;

public interface IBonusZoneSystem
{
    void SetOceanReference(IOceanSystem ocean);
    void GenerateForLaunch();
    BonusZoneCategory? CheckHit(Vector3 impactPosition);
    void ClearZones();
    event Action<Vector3, BonusZoneCategory> OnZoneHit;
}
