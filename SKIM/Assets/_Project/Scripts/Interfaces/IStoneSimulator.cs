using System;
using UnityEngine;

public interface IStoneSimulator
{
    StoneState CurrentState { get; }
    void Launch(FlickInput input, StoneData stone, bool assisted = false);
    void Reset();
    void SetOceanReference(IOceanSystem ocean);
    Vector3[] GetProjectedArc(FlickInput input, StoneData stone, int points = 3);
    event Action<StoneState> OnImpact;
    event Action<LaunchResult> OnSunk;
}
