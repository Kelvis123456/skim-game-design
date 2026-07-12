# FASE 8 — ARQUITECTURA DETALLADA
## Proyecto: SKIM

**Fecha:** 2026-06-30
**Stack:** Unity 2022.3 LTS + URP + IL2CPP + ARM64

---

## 1. Principios de Arquitectura

**Tres reglas que gobiernan todo el código de SKIM:**

1. **Los sistemas no se conocen entre sí — solo conocen sus interfaces.** `StoneSimulator` no importa `AudioSystem`. Emite un evento. `AudioSystem` escucha ese evento. Si uno se elimina, el otro sigue compilando.

2. **Los datos de configuración son ScriptableObjects, no constantes en código.** Las propiedades físicas de las piedras, los parámetros de los climas, las notas musicales — todo editable sin recompilar.

3. **El StoneSimulator es el corazón. Todo lo demás reacciona.** El simulador de física corre a 120Hz y es la única fuente de verdad sobre el estado de la piedra. Nada más toca la posición de la piedra directamente.

---

## 2. Modelos de Datos

### 2.1 FlickInput — El gesto del jugador

```csharp
// El contrato entre InputController y StoneSimulator
public readonly struct FlickInput
{
    public readonly float AngleDegrees;    // 0-360: dirección del lanzamiento
    public readonly float Force;           // 0.0-1.0: normalizado según max swipe speed
    public readonly float Spin;            // -1.0 a +1.0: curvatura del swipe (negativo = left spin)

    public FlickInput(float angle, float force, float spin)
    {
        AngleDegrees = Mathf.Clamp(angle, 0f, 360f);
        Force = Mathf.Clamp01(force);
        Spin = Mathf.Clamp(spin, -1f, 1f);
    }
}
```

### 2.2 StoneState — El estado completo de la piedra en un momento

```csharp
// Inmutable — el simulador produce un nuevo StoneState cada tick
public readonly struct StoneState
{
    public readonly Vector3 Position;
    public readonly Vector3 Velocity;
    public readonly float AngularVelocity;    // rad/s — para la rotación visual
    public readonly int SkipCount;            // Cuántos rebotes ha dado
    public readonly StonePhase Phase;         // En vuelo, en impacto, hundida
    public readonly float TotalDistance;      // Distancia horizontal acumulada (metros)
    public readonly float CurrentHeight;      // Altura sobre la superficie del agua
    
    public enum StonePhase { InFlight, Impacting, Sunk }
}
```

### 2.3 LaunchResult — El resumen de un lanzamiento completo

```csharp
public readonly struct LaunchResult
{
    public readonly float Distance;        // Metros desde el origen
    public readonly int SkipCount;         // Número de rebotes
    public readonly int Score;             // Puntuación calculada
    public readonly float MaxMultiplier;   // Multiplicador máximo alcanzado
    public readonly bool IsNewRecord;      // Si superó el récord de la sesión
    public readonly bool IsNewAllTimeRecord; // Si superó el récord histórico
}
```

### 2.4 StoneData — ScriptableObject por tipo de piedra

```csharp
[CreateAssetMenu(menuName = "SKIM/Stone Data")]
public class StoneData : ScriptableObject
{
    [Header("Identidad")]
    public string StoneName;
    public Sprite Icon;
    public GameObject Prefab;
    public int UnlockDistanceMeters;     // 0 = disponible desde el inicio
    
    [Header("Física")]
    [Range(0.5f, 1.0f)]
    public float ReboundCoefficient;     // Guijarro:0.72 Esquisto:0.85 Basalto:0.60 Cuarzo:0.78
    
    [Range(0.4f, 0.9f)]
    public float ElasticityCoefficient; // Guijarro:0.65 Esquisto:0.55 Basalto:0.80 Cuarzo:0.70
    
    [Range(0.1f, 1.5f)]
    public float SpinSensitivity;        // Guijarro:0.30 Esquisto:0.80 Basalto:0.20 Cuarzo:1.20
    
    [Range(0.05f, 0.30f)]
    public float Radius;                 // Radio para colisión con el agua
    
    [Range(0.1f, 1.0f)]
    public float Mass;                   // Afecta la inercia y la penetración en agua
}
```

### 2.5 ClimateData — ScriptableObject por clima

```csharp
[CreateAssetMenu(menuName = "SKIM/Climate Data")]
public class ClimateData : ScriptableObject
{
    [Header("Identidad")]
    public string ClimateName;
    public int UnlockDistanceMeters;
    
    [Header("Ondas")]
    [Range(2, 8)]
    public int HarmonicCount;            // N armónicos en la ecuación de olas
    
    public WaveHarmonic[] Harmonics;     // Aᵢ, kᵢ, ωᵢ por armónico
    
    [Header("Visual — agua")]
    public Color WaterSurfaceColor;
    public Color WaterDepthColor;
    public Color WaterReflectionColor;
    public Color SkyHorizonColor;
    
    [Header("Score")]
    [Range(1.0f, 3.0f)]
    public float ClimateMultiplier;      // Calma:1.0x Tormenta:2.5x
    
    [Header("Dificultad estimada")]
    [Range(1, 5)]
    public int DifficultyRating;

    [System.Serializable]
    public struct WaveHarmonic
    {
        public float Amplitude;          // Aᵢ en metros
        public float WaveLength;         // λᵢ en metros
        public float AngularFrequency;   // ωᵢ en rad/s
        public float InitialPhase;       // φᵢ — se randomiza al inicio de cada sesión
    }
}
```

---

## 3. Interfaces de los Módulos

### 3.1 IStoneSimulator

```csharp
public interface IStoneSimulator
{
    // Estado actual (actualizado a 120Hz)
    StoneState CurrentState { get; }
    
    // Lanza la piedra con el input del jugador
    // assisted: true durante los primeros 3 lanzamientos (Fase 3, decisión #1)
    void Launch(FlickInput input, StoneData stone, bool assisted = false);
    
    // Reinicia el simulador para un nuevo lanzamiento
    void Reset();
    
    // Actualiza la simulación (llamar desde un ciclo a 120Hz, NO desde Update)
    void Tick(float deltaTime);
    
    // Inyecta la referencia al sistema de océano (para obtener altura del agua)
    void SetOceanReference(IOceanSystem ocean);
    
    // Evento: se emite cada vez que la piedra impacta el agua
    event Action<StoneState> OnImpact;
    
    // Evento: se emite cuando la piedra se hunde definitivamente
    event Action<LaunchResult> OnSunk;
}
```

### 3.2 IOceanSystem

```csharp
public interface IOceanSystem
{
    // La MISMA ecuación que usa el vertex shader
    // Esta es la función más crítica del proyecto — debe ser idéntica al shader
    float GetHeightAt(float x, float time);
    
    // Derivada de la altura — para calcular el ángulo de rebote en la superficie inclinada
    float GetSlopeAt(float x, float time);
    
    // Cambia el clima activo con transición suave
    void SetClimate(ClimateData climate, float transitionDuration = 2f);
    
    ClimateData CurrentClimate { get; }
    
    // Tiempo de sesión (se pasa al shader para sincronizar animación)
    float SessionTime { get; }
}
```

### 3.3 IInputController

```csharp
public interface IInputController
{
    // El input está listo para ser leído (se emite al levantar el dedo)
    event Action<FlickInput> OnFlickDetected;
    
    // El jugador está arrastrando — usado para mostrar el arco de proyección
    event Action<Vector2> OnFlickDrag;
    
    bool IsEnabled { get; set; }
    
    // Proyección estimada del lanzamiento (para los dots de trayectoria en el HUD)
    // Devuelve los puntos de impacto estimados según el gesto actual
    Vector3[] GetProjectedArc(FlickInput currentInput, StoneData stone, int points = 3);
}
```

### 3.4 IAudioSystem

```csharp
public interface IAudioSystem
{
    // Toca la nota correspondiente al skip número N
    // La nota es determinista: misma entrada = misma nota siempre
    void PlaySkipNote(int skipNumber);
    
    // Chord resolution cuando se cierra una melodía (combo >= 5)
    void PlayChordResolution();
    
    // Audio ambiental del océano según el clima
    void SetClimateAmbience(ClimateData climate);
    
    // Para efectos de impacto (splash)
    void PlayImpactSound(float impactForce);
    
    float MusicVolume { get; set; }
    float SFXVolume { get; set; }
    
    // Escala pentatónica usada (C-D-E-G-A, octava determinada por skip count)
    // Expuesta para que VFXSystem pueda sincronizar colores de anillos
    Note GetNoteForSkip(int skipNumber);
    
    public enum Note { C, D, E, G, A }
}
```

### 3.5 IScoringSystem

```csharp
public interface IScoringSystem
{
    // Score actual de la sesión en curso
    int SessionScore { get; }
    
    // Multiplicador activo (1.0x - 15.0x)
    float CurrentMultiplier { get; }
    
    // Mejor distancia de la sesión actual
    float SessionBestDistance { get; }
    
    // Registra un impacto y actualiza score + multiplicador
    void RegisterImpact(StoneState impactState, ClimateData climate);
    
    // Finaliza el lanzamiento y calcula el LaunchResult
    LaunchResult FinalizeLaunch(StoneState finalState, ClimateData climate);
    
    // Reinicia el scoring para un nuevo lanzamiento
    void ResetForNewLaunch();
    
    // Eventos
    event Action<float> OnMultiplierChanged;       // nuevo multiplicador
    event Action<LaunchResult> OnLaunchCompleted;
    event Action<float> OnNewSessionRecord;        // nueva mejor distancia en sesión
}
```

### 3.6 IProgressionSystem

```csharp
public interface IProgressionSystem
{
    // Distancia acumulada a través de TODAS las sesiones (desbloquea piedras y climas)
    float TotalAccumulatedDistance { get; }
    
    // Récord personal histórico (mejor lanzamiento individual)
    float AllTimeRecord { get; }
    
    // Mejor sesión histórica (score total de una sesión)
    int AllTimeSessionScore { get; }
    
    // Actualiza la distancia acumulada después de un lanzamiento
    void RegisterLaunch(LaunchResult result);
    
    // Qué está desbloqueado según la distancia acumulada
    bool IsStoneUnlocked(StoneData stone);
    bool IsClimateUnlocked(ClimateData climate);
    
    // Lista ordenada de piedras y climas disponibles
    IReadOnlyList<StoneData> AvailableStones { get; }
    IReadOnlyList<ClimateData> AvailableClimates { get; }
    
    // Persistencia
    void Save();
    void Load();
}
```

### 3.7 IVFXSystem

```csharp
public interface IVFXSystem
{
    // Crea un anillo de agua en la posición de impacto
    // Persiste 60s, se expande, hace fade gradual
    void SpawnWaterRing(Vector3 worldPosition, IAudioSystem.Note note);
    
    // Splash de partículas en el impacto
    void SpawnImpactSplash(Vector3 worldPosition, float impactForce);
    
    // Trail de combo en la piedra (se actualiza cada frame)
    void UpdateComboTrail(Transform stoneTransform, int comboLevel);
    void ClearComboTrail();
    
    // Score pop-up en world space
    void SpawnScorePopup(Vector3 worldPosition, int scoreValue, bool isCombo);
    
    // Efecto de nuevo récord (estrellas doradas + pulse de la línea PB)
    void TriggerNewRecordEffect(float recordDistance);
    
    // Pulso de chord resolution (todos los anillos simultáneamente)
    void TriggerChordResolutionPulse();
    
    // Limpia todos los anillos de la sesión (al reiniciar sesión)
    void ClearSessionRings();
    
    // Referencia a la línea de PB en el océano
    void UpdatePBLine(float distance);
}
```

### 3.8 IEconomySystem

```csharp
public interface IEconomySystem
{
    int ConchaBalance { get; }
    
    // Gana Conchas (por completar desafíos diarios, etc.)
    void EarnConchas(int amount, string reason);
    
    // Gasta Conchas en cosmética
    // Devuelve false si el balance es insuficiente
    bool SpendConchas(int amount, string itemId);
    
    // Estado de cosmética
    bool IsCosmeticUnlocked(string cosmeticId);
    string GetEquippedStoneSkin(string stoneId);
    void EquipCosmeticSkin(string stoneId, string cosmeticId);
    
    // IAP (delega a la plataforma correcta — Google o Apple)
    void PurchaseConchaBundle(string bundleId);
    event Action<string> OnPurchaseCompleted;
    event Action<string> OnPurchaseFailed;
}
```

---

## 4. ServiceLocator — Implementación

```csharp
// Assets/_Project/Scripts/Core/ServiceLocator.cs
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service 
            ?? throw new ArgumentNullException(nameof(service));
    }

    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;
        
        throw new InvalidOperationException(
            $"Service {typeof(T).Name} not registered. " +
            $"Check GameBootstrapper registration order.");
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var s))
        {
            service = (T)s;
            return true;
        }
        service = null;
        return false;
    }

    // Llamar en teardown de tests o al reiniciar la aplicación
    public static void Clear() => _services.Clear();
}
```

---

## 5. GameBootstrapper — Secuencia de Arranque

```csharp
// Assets/_Project/Scripts/Core/GameBootstrapper.cs
// Este es el único MonoBehaviour en Boot.unity
// [DefaultExecutionOrder(-1000)] garantiza que corre ANTES que cualquier otro script

[DefaultExecutionOrder(-1000)]
public class GameBootstrapper : MonoBehaviour
{
    [Header("Sistemas")]
    [SerializeField] private StoneSimulatorImpl _stoneSimulator;
    [SerializeField] private OceanSystemImpl _oceanSystem;
    [SerializeField] private InputControllerImpl _inputController;
    [SerializeField] private AudioSystemImpl _audioSystem;
    [SerializeField] private ScoringSystemImpl _scoringSystem;
    [SerializeField] private ProgressionSystemImpl _progressionSystem;
    [SerializeField] private VFXSystemImpl _vfxSystem;
    [SerializeField] private EconomySystemImpl _economySystem;

    [Header("Datos")]
    [SerializeField] private StoneData[] _allStones;
    [SerializeField] private ClimateData[] _allClimates;

    private void Awake()
    {
        // 1. Cargar estado persistido PRIMERO
        _progressionSystem.Load();
        
        // 2. Registrar todos los servicios
        ServiceLocator.Register<IStoneSimulator>(_stoneSimulator);
        ServiceLocator.Register<IOceanSystem>(_oceanSystem);
        ServiceLocator.Register<IInputController>(_inputController);
        ServiceLocator.Register<IAudioSystem>(_audioSystem);
        ServiceLocator.Register<IScoringSystem>(_scoringSystem);
        ServiceLocator.Register<IProgressionSystem>(_progressionSystem);
        ServiceLocator.Register<IVFXSystem>(_vfxSystem);
        ServiceLocator.Register<IEconomySystem>(_economySystem);
        
        // 3. Conectar dependencias que no pueden ser inyectadas en Inspector
        _stoneSimulator.SetOceanReference(_oceanSystem);
        
        // 4. Conectar eventos (wiring)
        WireEvents();
        
        // 5. Cargar la escena de juego de forma aditiva
        SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
    }
    
    private void WireEvents()
    {
        // StoneSimulator → Scoring, VFX, Audio
        _stoneSimulator.OnImpact += state =>
        {
            _scoringSystem.RegisterImpact(state, _oceanSystem.CurrentClimate);
            _vfxSystem.SpawnImpactSplash(state.Position, state.Velocity.magnitude);
            _audioSystem.PlayImpactSound(state.Velocity.magnitude);
        };
        
        _stoneSimulator.OnSunk += result =>
        {
            _scoringSystem.FinalizeLaunch(
                ServiceLocator.Get<IStoneSimulator>().CurrentState,
                _oceanSystem.CurrentClimate);
        };
        
        // StoneSimulator.OnImpact → VFX rings (con nota del audio)
        _stoneSimulator.OnImpact += state =>
        {
            var note = _audioSystem.GetNoteForSkip(state.SkipCount);
            _audioSystem.PlaySkipNote(state.SkipCount);
            _vfxSystem.SpawnWaterRing(state.Position, note);
        };
        
        // Scoring → VFX (score popups, récords)
        _scoringSystem.OnLaunchCompleted += result =>
        {
            if (result.IsNewSessionRecord)
                _vfxSystem.TriggerNewRecordEffect(result.Distance);
        };
        
        _scoringSystem.OnMultiplierChanged += multiplier =>
        {
            // Chord resolution al llegar al nivel 5 de multiplicador
            if (multiplier >= 2.8f) // 5 skips consecutivos
                _audioSystem.PlayChordResolution();
                _vfxSystem.TriggerChordResolutionPulse();
        };
        
        // Input → Simulator
        _inputController.OnFlickDetected += input =>
        {
            var progression = ServiceLocator.Get<IProgressionSystem>();
            var stone = progression.AvailableStones[0]; // TODO: piedra seleccionada
            bool assisted = _totalLaunchCount < 3;
            _stoneSimulator.Launch(input, stone, assisted);
            _scoringSystem.ResetForNewLaunch();
        };
    }
    
    private int _totalLaunchCount = 0;
    
    private void OnApplicationPause(bool paused)
    {
        if (paused)
            ServiceLocator.Get<IProgressionSystem>().Save();
    }
    
    private void OnApplicationQuit()
    {
        ServiceLocator.Get<IProgressionSystem>().Save();
    }
}
```

---

## 6. StoneSimulator — Implementación Core

El corazón del juego. Todo lo que sigue es pseudocódigo de implementación para guiar el desarrollo en Fase 9.

```csharp
// Assets/_Project/Scripts/Physics/StoneSimulatorImpl.cs
public class StoneSimulatorImpl : MonoBehaviour, IStoneSimulator
{
    private StoneState _currentState;
    private StoneData _activeStone;
    private IOceanSystem _ocean;
    private float _accumulator; // Para el loop a 120Hz
    
    private const float PHYSICS_TIMESTEP = 1f / 120f;   // 8.33ms
    private const float GRAVITY = 9.8f;
    private const float AIR_DRAG = 0.02f;               // Fricción del aire
    private const float MIN_SKIP_SPEED = 1.5f;          // m/s — por debajo de esto, se hunde
    
    public StoneState CurrentState => _currentState;
    public event Action<StoneState> OnImpact;
    public event Action<LaunchResult> OnSunk;

    public void Launch(FlickInput input, StoneData stone, bool assisted = false)
    {
        _activeStone = stone;
        
        // Asistencia en los primeros 3 lanzamientos (Decisión de Diseño #1, Fase 3)
        // El modificador oculto garantiza al menos 2 rebotes
        float forceMultiplier = assisted ? Mathf.Max(input.Force, 0.5f) : input.Force;
        float angleMultiplier = assisted ? 1f : 1f; // El ángulo no se modifica
        
        // Convertir el FlickInput a velocidad inicial
        float angleRad = input.AngleDegrees * Mathf.Deg2Rad;
        float speed = forceMultiplier * 15f; // 15 m/s máximo con fuerza total
        
        _currentState = new StoneState(
            position: new Vector3(0, 0.3f, 0),  // 30cm sobre el origen
            velocity: new Vector3(
                Mathf.Cos(angleRad) * speed,
                0.8f * speed,                    // Componente vertical inicial
                Mathf.Sin(angleRad) * speed
            ),
            angularVelocity: input.Spin * 10f,   // rad/s proporcional al spin
            skipCount: 0,
            phase: StoneState.StonePhase.InFlight,
            totalDistance: 0,
            currentHeight: 0.3f
        );
    }

    private void Update()
    {
        if (_currentState.Phase == StoneState.StonePhase.Sunk) return;
        
        _accumulator += Time.deltaTime;
        
        while (_accumulator >= PHYSICS_TIMESTEP)
        {
            Tick(PHYSICS_TIMESTEP);
            _accumulator -= PHYSICS_TIMESTEP;
        }
    }

    public void Tick(float dt)
    {
        if (_currentState.Phase != StoneState.StonePhase.InFlight) return;
        
        // Integración Euler
        var vel = _currentState.Velocity;
        var pos = _currentState.Position;
        
        // Aplicar gravedad y drag del aire
        vel.y -= GRAVITY * dt;
        vel.x *= (1f - AIR_DRAG);
        vel.z *= (1f - AIR_DRAG);
        
        // Integrar posición
        pos += vel * dt;
        
        // Comprobar colisión con el agua
        float waterHeight = _ocean.GetHeightAt(pos.x, _ocean.SessionTime);
        
        if (pos.y <= waterHeight)
        {
            // IMPACTO — calcular rebote
            ProcessImpact(ref pos, ref vel, waterHeight);
        }
        
        // Actualizar distancia total
        float newDistance = _currentState.TotalDistance + 
                           new Vector2(vel.x, vel.z).magnitude * dt;
        
        _currentState = new StoneState(pos, vel, _currentState.AngularVelocity,
                                       _currentState.SkipCount, StoneState.StonePhase.InFlight,
                                       newDistance, pos.y - waterHeight);
    }
    
    private void ProcessImpact(ref Vector3 pos, ref Vector3 vel, float waterHeight)
    {
        float slope = _ocean.GetSlopeAt(pos.x, _ocean.SessionTime);
        float incomingSpeed = vel.magnitude;
        
        // La velocidad mínima para rebotar depende de la piedra y del spin
        float skipThreshold = MIN_SKIP_SPEED / _activeStone.ElasticityCoefficient;
        
        if (incomingSpeed < skipThreshold || _currentState.SkipCount >= 25)
        {
            // La piedra se hunde
            _currentState = new StoneState(pos, Vector3.zero, 0,
                                          _currentState.SkipCount, StoneState.StonePhase.Sunk,
                                          _currentState.TotalDistance, 0);
            
            var result = new LaunchResult(
                _currentState.TotalDistance, _currentState.SkipCount,
                CalculateScore(), 0, false, false);
            
            OnSunk?.Invoke(result);
            return;
        }
        
        // REBOTE — aplicar coeficientes de la piedra + efecto del spin
        pos.y = waterHeight + 0.01f; // Evitar tunneling
        
        // Invertir componente vertical con pérdida de energía
        vel.y = Mathf.Abs(vel.y) * _activeStone.ReboundCoefficient;
        
        // El spin modifica el ángulo de salida horizontal
        float spinEffect = _currentState.AngularVelocity * _activeStone.SpinSensitivity * 0.1f;
        vel.x += spinEffect;
        
        // La pendiente del agua afecta el rebote (si la ola está subiendo, ayuda; si baja, dificulta)
        vel.y += slope * vel.x * 0.1f;
        
        int newSkipCount = _currentState.SkipCount + 1;
        
        // Emitir evento con el nuevo estado
        var impactState = new StoneState(pos, vel, _currentState.AngularVelocity,
                                        newSkipCount, StoneState.StonePhase.Impacting,
                                        _currentState.TotalDistance, 0);
        OnImpact?.Invoke(impactState);
        
        // Inmediatamente volver a InFlight
        _currentState = new StoneState(pos, vel, _currentState.AngularVelocity,
                                       newSkipCount, StoneState.StonePhase.InFlight,
                                       _currentState.TotalDistance, pos.y - waterHeight);
    }
}
```

---

## 7. OceanSystem — Implementación Core

```csharp
// Assets/_Project/Scripts/Physics/OceanSystemImpl.cs
// CRÍTICO: La ecuación GetHeightAt DEBE ser idéntica al vertex shader
public class OceanSystemImpl : MonoBehaviour, IOceanSystem
{
    [SerializeField] private ClimateData _defaultClimate;
    [SerializeField] private MeshFilter _oceanMeshFilter;
    [SerializeField] private Material _oceanMaterial;
    
    private ClimateData _currentClimate;
    private float _sessionTime;
    private Mesh _oceanMesh;
    
    public ClimateData CurrentClimate => _currentClimate;
    public float SessionTime => _sessionTime;
    
    private void Awake()
    {
        _currentClimate = _defaultClimate;
        _oceanMesh = GenerateOceanMesh(80, 40, 50f, 25f); // 80x40 verts, 50x25 metros
        _oceanMeshFilter.mesh = _oceanMesh;
    }
    
    private void Update()
    {
        _sessionTime += Time.deltaTime;
        UpdateMeshVertices();
        
        // Sincronizar tiempo con el shader (para que visual = física exacta)
        _oceanMaterial.SetFloat("_Time", _sessionTime);
    }
    
    // ESTA FUNCIÓN ES EL CONTRATO CENTRAL DEL PROYECTO
    // Si cambia aquí, DEBE cambiar en el shader y viceversa
    public float GetHeightAt(float x, float time)
    {
        float height = 0f;
        var harmonics = _currentClimate.Harmonics;
        
        for (int i = 0; i < harmonics.Length; i++)
        {
            var h = harmonics[i];
            float k = 2f * Mathf.PI / h.WaveLength;
            height += h.Amplitude * Mathf.Sin(k * x + h.AngularFrequency * time + h.InitialPhase);
        }
        
        return height;
    }
    
    // Derivada ∂h/∂x — para calcular el ángulo de la superficie en un punto
    public float GetSlopeAt(float x, float time)
    {
        float slope = 0f;
        var harmonics = _currentClimate.Harmonics;
        
        for (int i = 0; i < harmonics.Length; i++)
        {
            var h = harmonics[i];
            float k = 2f * Mathf.PI / h.WaveLength;
            slope += h.Amplitude * k * Mathf.Cos(k * x + h.AngularFrequency * time + h.InitialPhase);
        }
        
        return slope;
    }
    
    private void UpdateMeshVertices()
    {
        var vertices = _oceanMesh.vertices;
        float time = _sessionTime;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = GetHeightAt(vertices[i].x, time);
        }
        
        _oceanMesh.vertices = vertices;
        _oceanMesh.RecalculateNormals(); // Necesario para el lighting del shader
    }
    
    public void SetClimate(ClimateData climate, float transitionDuration = 2f)
    {
        StartCoroutine(TransitionClimate(climate, transitionDuration));
    }
    
    private IEnumerator TransitionClimate(ClimateData target, float duration)
    {
        // Transición suave de colores de agua y cielo
        var startColors = GetCurrentColors();
        var targetColors = GetClimateColors(target);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            LerpClimateColors(startColors, targetColors, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        
        _currentClimate = target;
        
        // Randomizar las fases iniciales de las olas para que cada sesión se sienta diferente
        for (int i = 0; i < target.Harmonics.Length; i++)
        {
            target.Harmonics[i].InitialPhase = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        }
    }
}
```

---

## 8. Sistema de Save

Decisión tomada en esta fase: **JSON en Application.persistentDataPath**.

No se usa PlayerPrefs porque:
- PlayerPrefs está limitado a tipos primitivos — los datos de SKIM son complejos
- PlayerPrefs no es seguro (puede manipularse en Android sin root en algunos casos)
- JSON es legible para debugging durante Fase 9

No se usa Cloud Save (Unity Cloud Save) porque:
- Requiere login — añade fricción en el primer uso
- SKIM es un juego offline-first
- Cloud Save puede añadirse en v1.2 como feature opcional

```csharp
// Assets/_Project/Scripts/Core/SaveData.cs
[System.Serializable]
public class SaveData
{
    public float TotalAccumulatedDistance;
    public float AllTimeRecord;
    public int AllTimeSessionScore;
    public int ConchaBalance;
    public string EquippedStoneId;
    public string[] UnlockedCosmeticIds;
    public DailyChallenge LastDailyChallenge;
    public long LastSaveTimestamp;   // Unix timestamp — para validar el desafío diario
    
    [System.Serializable]
    public class DailyChallenge
    {
        public string ChallengeId;
        public float Progress;
        public bool Completed;
    }
}

// En ProgressionSystemImpl:
private const string SAVE_FILENAME = "skim_save.json";

public void Save()
{
    var data = new SaveData { /* ... populate ... */ };
    string json = JsonUtility.ToJson(data, prettyPrint: false);
    string path = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
    File.WriteAllText(path, json);
}

public void Load()
{
    string path = Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
    if (!File.Exists(path)) return;
    
    string json = File.ReadAllText(path);
    var data = JsonUtility.FromJson<SaveData>(json);
    // ... apply data ...
}
```

---

## 9. Fórmula de Score — Implementación Exacta

```csharp
// En ScoringSystemImpl
private int CalculateScore(StoneState finalState, ClimateData climate)
{
    // Fórmula base del GDD:
    // (distancia_m × 10) × multiplicador_acumulado + bonus_zones + climate_multiplier
    
    float baseScore = finalState.TotalDistance * 10f;
    float multipliedScore = baseScore * _currentMultiplier;
    int bonusZones = _bonusZonesHit * 500;  // 500pts por zona bonus
    float climateBonus = multipliedScore * (climate.ClimateMultiplier - 1f);
    
    return Mathf.RoundToInt(multipliedScore + bonusZones + climateBonus);
}

// Tabla de multiplicadores (del GDD)
private static readonly float[] MULTIPLIER_TABLE = 
{
    1.0f,  // 0 skips
    1.0f,  // 1 skip
    1.3f,  // 2 skips
    1.7f,  // 3 skips
    2.2f,  // 4 skips
    2.8f,  // 5 skips
    3.5f,  // 6 skips
    // A partir de 6: +0.9x por cada skip adicional
};
private const float MULTIPLIER_INCREMENT = 0.9f;
private const float MULTIPLIER_CAP = 15.0f;

private float GetMultiplierForSkipCount(int skipCount)
{
    if (skipCount < MULTIPLIER_TABLE.Length)
        return MULTIPLIER_TABLE[skipCount];
    
    float extra = (skipCount - MULTIPLIER_TABLE.Length + 1) * MULTIPLIER_INCREMENT;
    return Mathf.Min(MULTIPLIER_TABLE[^1] + extra, MULTIPLIER_CAP);
}
```

---

## 10. Checklist de Setup Inicial del Proyecto Unity

Estos pasos ocurren en la primera hora de Fase 9:

```
□ 1. Crear proyecto nuevo en Unity Hub
     - Unity 2022.3.52f1
     - Template: URP 3D
     - Nombre: "SKIM"
     - Location: separado del folder NuevoJuego

□ 2. Configurar URP Asset según specs de Fase 7
     - MSAA 2x
     - Shadows OFF
     - Post-processing: solo vignette disponible

□ 3. Instalar paquetes necesarios (Package Manager)
     - Input System 1.7.x
     - TextMeshPro (built-in, solo importar Essential Resources)
     - VFX Graph (built-in)
     - Addressables 1.21.x
     - DOTween Pro (Asset Store)

□ 4. Crear estructura de carpetas según Fase 7
     Assets/_Project/{Scripts,Art,Audio,Data,Scenes,Prefabs}

□ 5. Configurar Build Settings
     - Android: API 26+, ARM64, IL2CPP, LZ4HC
     - iOS: 14.0+, ARM64

□ 6. Configurar Player Settings
     - Company: [NombreEstudio]
     - Product: SKIM
     - Bundle ID: com.[estudio].skim
     - Version: 0.1.0

□ 7. Setup de Git
     - git init
     - .gitignore de Unity (unitygitignore.io)
     - .gitattributes con Unity YAML Merge + Git LFS para binarios

□ 8. Crear ScriptableObjects vacíos
     - StoneData × 4 (Guijarro, Esquisto, Basalto, Cuarzo) con valores del GDD
     - ClimateData × 5 (Calma, Brisa, Viento, Marejada, Tormenta)

□ 9. Crear Boot.unity con GameBootstrapper
     - Sin cámaras de gameplay
     - Sin luces
     - Solo el GameBootstrapper y DontDestroyOnLoad

□ 10. Primer commit: "Initial Unity project setup"
```

---

## 11. Autocrítica — Fase 8

### Fortalezas

**1. El StoneSimulator está completamente especificado**
El código pseudocódigo es suficientemente concreto para que un desarrollador lo implemente sin preguntas. La ecuación de olas, los coeficientes de rebote, el loop a 120Hz — todo está aquí.

**2. El wiring de eventos está centralizado**
Poner todo el event wiring en `GameBootstrapper.WireEvents()` significa que hay un único lugar donde ver "qué causa qué". No hay dependencias ocultas.

**3. La decisión de save está justificada**
JSON en persistentDataPath es la elección correcta para un juego offline-first indie. La justificación de por qué no PlayerPrefs y por qué no Cloud Save está documentada.

### Debilidades encontradas

**1. El InputController no está completamente especificado**
Solo se definió la interface. La implementación del swipe detection (cómo calcular ángulo, fuerza y spin desde TouchPhase) no está documentada. Esto es trabajo de Fase 9.

**2. El sistema de Desafío Diario no tiene arquitectura**
El DailyChallenge está en el SaveData como struct pero no hay un `IDailyChallengeSystem`. Deuda para Fase 9 o post-lanzamiento.

**3. El `GetProjectedArc` del InputController es complejo**
Proyectar la trayectoria en tiempo real requiere correr el StoneSimulator "en modo fantasma" a velocidad × 10 o más. No se especificó cómo hacer esto sin impactar el performance del frame. Riesgo técnico a investigar en Fase 9.

### Decisión de avance

Las interfaces están definidas. El StoneSimulator tiene suficiente detalle para implementar. El proyecto Unity puede configurarse. Los ScriptableObjects están especificados.

**SKIM avanza a Fase 9 — Vertical Slice.**

La Fase 9 es la primera vez que hay código ejecutándose. El primer objetivo del Vertical Slice es: **lanzar una piedra y que rebote de manera que se sienta justa y satisfactoria.** Todo lo demás viene después.
