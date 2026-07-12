using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameBootstrapper : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        ServiceLocator.Clear(); // always start fresh — clears any stale references from prior sessions

        var progression = GetComponent<ProgressionSystemImpl>();
        var stone       = GetComponent<StoneSimulatorImpl>();
        var ocean       = GetComponent<OceanSystemImpl>();
        var input       = GetComponent<InputControllerImpl>();
        var audio       = GetComponent<AudioSystemImpl>();
        var scoring     = GetComponent<ScoringSystemImpl>();
        var vfx         = GetComponent<VFXSystemImpl>();
        var economy     = GetComponent<EconomySystemImpl>();

        // Validate — log clearly if any component is missing
        bool ok = Validate("ProgressionSystem", progression)
               && Validate("StoneSimulator",    stone)
               && Validate("OceanSystem",       ocean)
               && Validate("InputController",   input)
               && Validate("AudioSystem",       audio)
               && Validate("ScoringSystem",     scoring)
               && Validate("VFXSystem",         vfx)
               && Validate("EconomySystem",     economy);

        if (!ok) { Debug.LogError("[Boot] Missing components — aborting."); return; }

        try { progression.Load(); }
        catch (System.Exception e) { Debug.LogWarning("[Boot] progression.Load() failed: " + e.Message); }

        ServiceLocator.Register<IProgressionSystem>(progression);
        ServiceLocator.Register<IStoneSimulator>(stone);
        ServiceLocator.Register<IOceanSystem>(ocean);
        ServiceLocator.Register<IInputController>(input);
        ServiceLocator.Register<IAudioSystem>(audio);
        ServiceLocator.Register<IScoringSystem>(scoring);
        ServiceLocator.Register<IVFXSystem>(vfx);
        ServiceLocator.Register<IEconomySystem>(economy);

        stone.SetOceanReference(ocean);
        WireEvents(stone, ocean, input, audio, scoring, vfx, progression);

        SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);

        Debug.Log("[Boot] All services registered. Loading Game scene...");
    }

    static bool Validate(string label, Object obj)
    {
        if (obj != null) return true;
        Debug.LogError($"[Boot] {label} component is MISSING on Systems GameObject.");
        return false;
    }

    void WireEvents(StoneSimulatorImpl stone, OceanSystemImpl ocean, InputControllerImpl input,
                    AudioSystemImpl audio, ScoringSystemImpl scoring, VFXSystemImpl vfx,
                    ProgressionSystemImpl progression)
    {
        stone.OnImpact += state =>
        {
            var note = audio.GetNoteForSkip(state.SkipCount);
            audio.PlaySkipNote(state.SkipCount);
            audio.PlayImpactSound(state.Velocity.magnitude);
            vfx.SpawnWaterRing(state.Position, note);
            vfx.SpawnImpactSplash(state.Position, state.Velocity.magnitude);
            scoring.RegisterImpact(state, ocean.CurrentClimate);
        };

        stone.OnSunk += _ =>
        {
            var result = scoring.FinalizeLaunch(stone.CurrentState, ocean.CurrentClimate);
            progression.RegisterLaunch(result);
            vfx.UpdatePBLine(progression.AllTimeRecord);
            input.IsEnabled = true;
        };

        scoring.OnMultiplierChanged += mult =>
        {
            if (mult >= 2.8f)
            {
                audio.PlayChordResolution();
                vfx.TriggerChordResolutionPulse();
            }
        };

        scoring.OnNewSessionRecord += dist => vfx.TriggerNewRecordEffect(dist);

        input.OnFlickDetected += flickInput =>
        {
            var selectedStone = progression.SelectedStone;
            if (selectedStone == null) return;
            bool assisted = progression.TotalSessionCount <= 3;
            stone.Launch(flickInput, selectedStone, assisted);
            scoring.ResetForNewLaunch();
            input.IsEnabled = false;
        };

        if (ocean.CurrentClimate != null)
            audio.SetClimateAmbience(ocean.CurrentClimate);
    }
}
