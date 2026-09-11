using System;
using UnityEngine;

public interface IStoneSimulator
{
    StoneState CurrentState { get; }
    // assistFraction 0-1 blends the launch force toward the assisted floor (see
    // GameBootstrapper's graduated onboarding curve) — 0 is a raw, unassisted throw.
    void Launch(FlickInput input, StoneData stone, float assistFraction = 0f);
    void Reset();
    void SetOceanReference(IOceanSystem ocean);
    Vector3[] GetProjectedArc(FlickInput input, StoneData stone, float assistFraction = 0f, int points = 3);
    event Action<StoneState> OnImpact;
    event Action<LaunchResult> OnSunk;
}
