using UnityEngine;

public readonly struct FlickInput
{
    public readonly float AngleDegrees;
    public readonly float Force;
    public readonly float Spin;

    public FlickInput(float angle, float force, float spin)
    {
        AngleDegrees = Mathf.Clamp(angle, 0f, 360f);
        Force = Mathf.Clamp01(force);
        Spin = Mathf.Clamp(spin, -1f, 1f);
    }

    public static FlickInput Default => new FlickInput(45f, 0.7f, 0f);
}
