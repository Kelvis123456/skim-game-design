using System;
using System.Collections.Generic;
using UnityEngine;

public class StoneSimulatorImpl : MonoBehaviour, IStoneSimulator
{
    StoneState _state;
    StoneData _stone;
    IOceanSystem _ocean;
    float _accumulator;

    const float TIMESTEP = 1f / 120f;
    const float GRAVITY = 9.8f;
    const float AIR_DRAG = 0.018f;
    const float MIN_SKIP_SPEED = 1.5f;
    const float MAX_SKIPS = 25;
    const float MAX_LAUNCH_SPEED = 16f;

    static readonly float[] MULTIPLIER_TABLE = { 1f, 1f, 1.3f, 1.7f, 2.2f, 2.8f, 3.5f };
    const float MULTIPLIER_INCREMENT = 0.9f;
    const float MULTIPLIER_CAP = 15f;

    public StoneState CurrentState => _state;
    public event Action<StoneState> OnImpact;
    public event Action<LaunchResult> OnSunk;

    void Awake()
    {
        // Default enum value is InFlight (0) — force Sunk so physics loop doesn't run before Launch()
        _state = new StoneState(Vector3.zero, Vector3.zero, 0f, 0, StoneState.StonePhase.Sunk, 0f, 0f);
    }

    public void SetOceanReference(IOceanSystem ocean) => _ocean = ocean;

    const float ASSIST_FORCE_FLOOR = 0.45f;

    public void Launch(FlickInput input, StoneData stone, float assistFraction = 0f)
    {
        _stone = stone;
        _accumulator = 0f;

        float force = Mathf.Lerp(input.Force, Mathf.Max(input.Force, ASSIST_FORCE_FLOOR), assistFraction);
        float speed = force * MAX_LAUNCH_SPEED;
        float rad = input.AngleDegrees * Mathf.Deg2Rad;

        var vel = new Vector3(
            Mathf.Cos(rad) * speed,
            speed * 0.55f,
            Mathf.Sin(rad) * speed * 0.1f
        );

        float startY = _ocean != null ? _ocean.GetHeightAt(0f, _ocean.SessionTime) + 0.3f : 0.3f;
        _state = new StoneState(new Vector3(0f, startY, 0f), vel,
                                input.Spin * 10f, 0, StoneState.StonePhase.InFlight, 0f, 0.3f);
    }

    public void Reset()
    {
        _state = new StoneState(Vector3.zero, Vector3.zero, 0f, 0,
                                StoneState.StonePhase.Sunk, 0f, 0f);
        _accumulator = 0f;
    }

    void Update()
    {
        if (_state.Phase != StoneState.StonePhase.InFlight) return;
        _accumulator += Time.deltaTime;
        while (_accumulator >= TIMESTEP)
        {
            Tick(TIMESTEP);
            _accumulator -= TIMESTEP;
        }
    }

    public void Tick(float dt)
    {
        if (_state.Phase != StoneState.StonePhase.InFlight) return;

        var vel = _state.Velocity;
        var pos = _state.Position;

        vel.y -= GRAVITY * dt;
        vel.x *= 1f - AIR_DRAG * dt;
        vel.z *= 1f - AIR_DRAG * dt;
        pos += vel * dt;

        float t = _ocean?.SessionTime ?? 0f;
        float waterY = _ocean?.GetHeightAt(pos.x, t) ?? 0f;

        if (pos.y <= waterY)
            ProcessImpact(ref pos, ref vel, waterY, t);
        else
        {
            float dist = _state.TotalDistance + new Vector2(vel.x, vel.z).magnitude * dt;
            _state = new StoneState(pos, vel, _state.AngularVelocity, _state.SkipCount,
                                    StoneState.StonePhase.InFlight, dist, pos.y - waterY);
        }
    }

    void ProcessImpact(ref Vector3 pos, ref Vector3 vel, float waterY, float t)
    {
        float speed = vel.magnitude;
        float threshold = MIN_SKIP_SPEED / _stone.ElasticityCoefficient;

        if (speed < threshold || _state.SkipCount >= MAX_SKIPS)
        {
            _state = new StoneState(pos, Vector3.zero, 0f, _state.SkipCount,
                                    StoneState.StonePhase.Sunk, _state.TotalDistance, 0f);
            var result = BuildResult();
            OnSunk?.Invoke(result);
            return;
        }

        pos.y = waterY + 0.005f;
        vel.y = Mathf.Abs(vel.y) * _stone.ReboundCoefficient;

        float slope = _ocean?.GetSlopeAt(pos.x, t) ?? 0f;
        vel.x += _state.AngularVelocity * _stone.SpinSensitivity * 0.08f;
        vel.y += slope * Mathf.Abs(vel.x) * 0.12f;

        int skips = _state.SkipCount + 1;
        float dist = _state.TotalDistance + new Vector2(vel.x, vel.z).magnitude * TIMESTEP;

        var impactState = new StoneState(pos, vel, _state.AngularVelocity, skips,
                                         StoneState.StonePhase.Impacting, dist, 0f);
        OnImpact?.Invoke(impactState);

        _state = new StoneState(pos, vel, _state.AngularVelocity, skips,
                                StoneState.StonePhase.InFlight, dist, pos.y - waterY);
    }

    // Score/multiplier/record fields are always zero here — the physics layer has no
    // scoring data. The real result comes from ScoringSystemImpl.FinalizeLaunch(),
    // which is what GameBootstrapper actually uses; this one only carries distance/skips.
    LaunchResult BuildResult() =>
        new LaunchResult(_state.TotalDistance, _state.SkipCount, 0, 0f, false, false);

    public Vector3[] GetProjectedArc(FlickInput input, StoneData stone, float assistFraction = 0f, int points = 3)
    {
        if (_ocean == null) return new Vector3[points];

        var ghost = new StoneSimulatorGhost(stone, _ocean);
        ghost.Launch(input, assistFraction);

        var hits = new List<Vector3>();
        float t = 0f;
        const float ghostDt = 1f / 240f;
        const float maxTime = 30f;

        while (hits.Count < points && t < maxTime)
        {
            t += ghostDt;
            ghost.Tick(ghostDt, _ocean.SessionTime + t);
            if (ghost.LastImpact.HasValue)
            {
                hits.Add(ghost.LastImpact.Value);
                ghost.LastImpact = null;
            }
            if (ghost.Sunk) break;
        }

        while (hits.Count < points)
            hits.Add(hits.Count > 0 ? hits[hits.Count - 1] : Vector3.zero);

        return hits.ToArray();
    }

    public static float GetMultiplier(int skipCount)
    {
        if (skipCount < MULTIPLIER_TABLE.Length) return MULTIPLIER_TABLE[skipCount];
        float extra = (skipCount - MULTIPLIER_TABLE.Length + 1) * MULTIPLIER_INCREMENT;
        return Mathf.Min(MULTIPLIER_TABLE[^1] + extra, MULTIPLIER_CAP);
    }
}

class StoneSimulatorGhost
{
    StoneData _stone;
    IOceanSystem _ocean;
    Vector3 _pos, _vel;
    float _angVel;
    bool _sunk;

    const float GRAVITY = 9.8f;
    const float AIR_DRAG = 0.018f;
    const float MIN_SKIP_SPEED = 1.5f;

    public Vector3? LastImpact;
    public bool Sunk => _sunk;

    public StoneSimulatorGhost(StoneData stone, IOceanSystem ocean)
    {
        _stone = stone; _ocean = ocean;
    }

    public void Launch(FlickInput input, float assistFraction = 0f)
    {
        float force = Mathf.Lerp(input.Force, Mathf.Max(input.Force, 0.45f), assistFraction);
        float speed = force * 16f;
        float rad = input.AngleDegrees * Mathf.Deg2Rad;
        _pos = new Vector3(0f, 0.3f, 0f);
        _vel = new Vector3(Mathf.Cos(rad) * speed, speed * 0.55f, 0f);
        _angVel = input.Spin * 10f;
    }

    public void Tick(float dt, float time)
    {
        if (_sunk) return;
        _vel.y -= GRAVITY * dt;
        _vel.x *= 1f - AIR_DRAG * dt;
        _pos += _vel * dt;

        float waterY = _ocean.GetHeightAt(_pos.x, time);
        if (_pos.y > waterY) return;

        float speed = _vel.magnitude;
        if (speed < MIN_SKIP_SPEED / _stone.ElasticityCoefficient)
        { _sunk = true; return; }

        _pos.y = waterY + 0.005f;
        _vel.y = Mathf.Abs(_vel.y) * _stone.ReboundCoefficient;
        _vel.x += _angVel * _stone.SpinSensitivity * 0.08f;
        LastImpact = _pos;
    }
}
