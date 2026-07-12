using UnityEngine;

[CreateAssetMenu(menuName = "SKIM/Climate Data", fileName = "ClimateData")]
public class ClimateData : ScriptableObject
{
    [Header("Identity")]
    public string ClimateName;
    public int UnlockDistanceMeters;
    [Range(1, 5)] public int DifficultyRating = 1;

    [Header("Waves")]
    public WaveHarmonic[] Harmonics;

    [Header("Score")]
    [Range(1f, 3f)] public float ClimateMultiplier = 1f;

    [Header("Visual - Water")]
    public Color WaterSurfaceColor = new Color(0f, 0.77f, 0.8f);
    public Color WaterDepthColor = new Color(0.04f, 0.09f, 0.16f);
    public Color SkyHorizonColor = new Color(0.55f, 0.71f, 0.83f);
    public Color AtmosphereColor = new Color(0.04f, 0.09f, 0.16f);

    [System.Serializable]
    public struct WaveHarmonic
    {
        public float Amplitude;
        public float WaveLength;
        public float AngularFrequency;
        [HideInInspector] public float InitialPhase;
    }
}
