public interface IOceanSystem
{
    float GetHeightAt(float x, float time);
    float GetSlopeAt(float x, float time);
    void SetClimate(ClimateData climate, float transitionDuration = 2f);
    // Briefly pushes the water's crest bioluminescence toward `boost` over `duration`
    // seconds, then eases back — used for the chord-resolution VFX pulse.
    void PulseCrestBoost(float boost, float duration);
    ClimateData CurrentClimate { get; }
    float SessionTime { get; }
}
