using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class GameBootstrapper : MonoBehaviour
{
    Transform _stoneVisualTransform;

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

        // Must run after Load() replaces the save-data reference, or the economy
        // system ends up bound to a stale/default blob and conchas never persist.
        economy.Init(progression.RawData);

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

            bool isCombo = state.SkipCount >= 3;
            vfx.SpawnScorePopup(state.Position, scoring.LastImpactScoreDelta, isCombo);

            int comboLevel = state.SkipCount switch
            {
                >= 7 => 3,
                >= 5 => 2,
                >= 3 => 1,
                _ => 0,
            };
            vfx.UpdateComboTrail(FindStoneVisualTransform(), comboLevel);
        };

        stone.OnSunk += _ =>
        {
            // FinalizeLaunch may fire OnNewSessionRecord synchronously below, which
            // already pulses the PB line before moving it — skip the plain move here
            // so a record launch doesn't jump the line before the pulse plays.
            var result = scoring.FinalizeLaunch(stone.CurrentState, ocean.CurrentClimate);
            progression.RegisterLaunch(result);
            progression.RegisterDailyChallengeLaunch(result);
            if (!result.IsNewSessionRecord) vfx.UpdatePBLine(progression.AllTimeRecord);
            vfx.ClearComboTrail();
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

    Transform FindStoneVisualTransform()
    {
        if (_stoneVisualTransform == null)
        {
            var go = GameObject.Find("Stone");
            if (go != null) _stoneVisualTransform = go.transform;
        }
        return _stoneVisualTransform;
    }
}
