using UnityEngine;

public readonly struct StoneState
{
    public readonly Vector3 Position;
    public readonly Vector3 Velocity;
    public readonly float AngularVelocity;
    public readonly int SkipCount;
    public readonly StonePhase Phase;
    public readonly float TotalDistance;
    public readonly float CurrentHeight;

    public enum StonePhase { Sunk, InFlight, Impacting }

    public StoneState(Vector3 position, Vector3 velocity, float angularVelocity,
                      int skipCount, StonePhase phase, float totalDistance, float currentHeight)
    {
        Position = position;
        Velocity = velocity;
        AngularVelocity = angularVelocity;
        SkipCount = skipCount;
        Phase = phase;
        TotalDistance = totalDistance;
        CurrentHeight = currentHeight;
    }

    public bool IsActive => Phase != StonePhase.Sunk;
}
