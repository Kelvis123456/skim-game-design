using System.Collections;
using UnityEngine;
using static IAudioSystem;

public class AudioSystemImpl : MonoBehaviour, IAudioSystem
{
    AudioSource _musicSource;
    AudioSource _sfxSource;
    AudioSource _ambienceSource;

    AudioClip[] _noteClips;
    AudioClip _splashClip;
    AudioClip _chordClip;

    static readonly float[] NOTE_FREQS = { 261.6f, 293.7f, 329.6f, 392f, 440f, 523.3f };
    const int SAMPLE_RATE = 44100;
    const float NOTE_DURATION = 0.12f;

    public float MusicVolume
    {
        get => _musicSource ? _musicSource.volume : 1f;
        set { if (_musicSource) _musicSource.volume = value; }
    }
    public float SFXVolume
    {
        get => _sfxSource ? _sfxSource.volume : 1f;
        set { if (_sfxSource) _sfxSource.volume = value; }
    }

    void Awake()
    {
        _musicSource    = CreateSource("Music",    0.7f, false);
        _sfxSource      = CreateSource("SFX",      1.0f, false);
        _ambienceSource = CreateSource("Ambience", 0.4f, true);
        GenerateClips();
    }

    AudioSource CreateSource(string label, float vol, bool loop)
    {
        var go = new GameObject(label + "Source");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.volume = vol;
        return src;
    }

    void GenerateClips()
    {
        _noteClips = new AudioClip[NOTE_FREQS.Length];
        for (int i = 0; i < NOTE_FREQS.Length; i++)
            _noteClips[i] = SynthNote(NOTE_FREQS[i], NOTE_DURATION);

        _splashClip = SynthSplash();
        _chordClip = SynthChord();
    }

    public void PlaySkipNote(int skipNumber)
    {
        int idx = skipNumber % _noteClips.Length;
        float vol = Mathf.Clamp(1f - skipNumber * 0.025f, 0.4f, 1f) * SFXVolume;
        _sfxSource?.PlayOneShot(_noteClips[idx], vol);
    }

    public void PlayImpactSound(float force)
    {
        float vol = Mathf.Clamp01(force / 12f) * SFXVolume;
        if (vol > 0.05f) _sfxSource?.PlayOneShot(_splashClip, vol);
    }

    public void PlayChordResolution() => _sfxSource?.PlayOneShot(_chordClip, 0.7f * SFXVolume);

    public void SetClimateAmbience(ClimateData climate)
    {
        // Ambience clips loaded via Addressables in full build
        // For VS: just adjust pitch to simulate rougher seas
        if (_ambienceSource == null) return;
        float pitch = 1f + (climate.DifficultyRating - 1) * 0.08f;
        _ambienceSource.pitch = pitch;
        if (!_ambienceSource.isPlaying) _ambienceSource.Play();
    }

    public MusicalNote GetNoteForSkip(int skipNumber)
    {
        var notes = new[] { MusicalNote.C, MusicalNote.D, MusicalNote.E, MusicalNote.G, MusicalNote.A };
        return notes[skipNumber % notes.Length];
    }

    static AudioClip SynthNote(float freq, float duration)
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * duration);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float env = Envelope(t, duration, 0.005f, 0.02f, 0.7f, 0.05f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        }
        var clip = AudioClip.Create("note", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip SynthSplash()
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * 0.06f);
        var data = new float[samples];
        var rng = new System.Random(42);
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float env = Envelope(t, 0.06f, 0.002f, 0.01f, 0.5f, 0.04f);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float lowpass = Mathf.Sin(2f * Mathf.PI * 180f * t);
            data[i] = (noise * 0.6f + lowpass * 0.4f) * env * 0.5f;
        }
        var clip = AudioClip.Create("splash", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip SynthChord()
    {
        int samples = Mathf.RoundToInt(SAMPLE_RATE * 0.5f);
        var data = new float[samples];
        float[] chord = { 261.6f, 329.6f, 392f };
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float env = Envelope(t, 0.5f, 0.01f, 0.05f, 0.6f, 0.3f);
            float s = 0f;
            foreach (var f in chord) s += Mathf.Sin(2f * Mathf.PI * f * t);
            data[i] = s / chord.Length * env * 0.6f;
        }
        var clip = AudioClip.Create("chord", samples, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Envelope(float t, float total, float attack, float decay, float sustain, float release)
    {
        if (t < attack) return t / attack;
        if (t < attack + decay) return 1f - (1f - sustain) * ((t - attack) / decay);
        float releaseStart = total - release;
        if (t < releaseStart) return sustain;
        return sustain * (1f - (t - releaseStart) / release);
    }
}
