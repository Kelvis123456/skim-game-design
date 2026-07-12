public interface IAudioSystem
{
    void PlaySkipNote(int skipNumber);
    void PlayChordResolution();
    void PlayImpactSound(float impactForce);
    void SetClimateAmbience(ClimateData climate);
    float MusicVolume { get; set; }
    float SFXVolume { get; set; }
    MusicalNote GetNoteForSkip(int skipNumber);

    public enum MusicalNote { C, D, E, G, A }
}
