public interface IOceanSystem
{
    float GetHeightAt(float x, float time);
    float GetSlopeAt(float x, float time);
    void SetClimate(ClimateData climate, float transitionDuration = 2f);
    ClimateData CurrentClimate { get; }
    float SessionTime { get; }
}
