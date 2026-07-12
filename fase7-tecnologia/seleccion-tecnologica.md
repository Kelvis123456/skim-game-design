# FASE 7 — SELECCIÓN TECNOLÓGICA
## Proyecto: SKIM

**Fecha:** 2026-06-30
**Decisión:** Todas las selecciones en esta fase son definitivas hasta el Vertical Slice (Fase 9).
**Revisión permitida:** Solo si una herramienta demuestra bloquear el performance target.

---

## 1. Motor de Juego — Unity 2022.3 LTS

### Selección
**Unity 2022.3.x LTS** (Long Term Support)

### Por qué Unity (vs alternativas)

| Criterio | Unity 2022 LTS | Godot 4 | Unreal 5 |
|---------|----------------|---------|----------|
| Mobile performance | ✅ Excelente | ✅ Bueno | ⚠️ Pesado |
| URP/shader custom | ✅ ShaderGraph | ⚠️ Limitado | ✅ Excelente |
| IL2CPP ARM64 | ✅ Nativo | ✅ Nativo | ✅ Nativo |
| Tamaño de app base | ✅ ~25MB | ✅ ~15MB | ❌ ~100MB+ |
| Store de assets | ✅ Enorme | ⚠️ Pequeño | ✅ Grande |
| Curva de aprendizaje | ✅ Conocida | ✅ Accesible | ❌ Empinada |
| Riesgo para indie | ✅ Bajo | ✅ Bajo | ❌ Alto |

**Decisión:** Unity por familiaridad, ecosistema mobile, y que el equipo ya tiene experiencia.

### Por qué 2022 LTS (vs Unity 6)

- Unity 6 (2023+) aún tiene issues no resueltos en mobile ARM64 reportados en foros
- 2022.3 LTS tiene ciclo de soporte garantizado hasta 2025 Q2 — suficiente para lanzar
- El proyecto no requiere características exclusivas de Unity 6 (no VR, no DOTS crítico)
- **Regla:** Si hay bugs críticos que Unity 6 resuelve y 2022 no, se migra en Fase 9

### Versión exacta a usar
```
Unity 2022.3.52f1 (o la última patch de 2022.3 al momento de setup)
```

---

## 2. Render Pipeline — URP

### Selección
**Universal Render Pipeline (URP)** — no Built-in, no HDRP

### Justificación

| Criterio | Built-in | URP | HDRP |
|---------|----------|-----|------|
| Performance mobile | ⚠️ Legacy | ✅ Optimizado | ❌ No mobile |
| ShaderGraph | ⚠️ Limitado | ✅ Completo | ✅ Completo |
| Materiales Unlit | ✅ | ✅ | ✅ |
| VFX Graph | ❌ | ✅ | ✅ |
| Batch rendering | ⚠️ | ✅ SRP Batcher | ✅ |
| Tamaño de runtime | Mediano | ✅ Pequeño | ❌ Grande |

**URP** es el único pipeline que combina: rendimiento mobile real + ShaderGraph para el shader de agua + VFX Graph para partículas.

### Configuración URP para SKIM

```yaml
# UniversalRenderPipelineAsset — SKIM settings
Rendering:
  Render Scale: 1.0          # Sin downsampling — pantallas modernas lo soportan
  Depth Texture: true        # Necesario para efectos de borde de agua
  Opaque Texture: false      # No necesario
  
Antialiasing (en UCamera):
  Mode: MSAA 2x              # Balance calidad/performance para low-poly

Shadows:
  Main Light Shadows: false  # Materiales Unlit — no proyectan sombras
  Shadow Distance: 0         # No hay sombras en el juego

Post-Processing:
  Enabled: true
  Vignette: muy sutil (0.2 intensidad) solo en Tormenta
  Color Grading: false        # El color es intencional en los materiales, no en post
  Bloom: false                # CRÍTICO: Bloom destruye el look low-poly. NUNCA activar.
  Depth of Field: false

HDR:
  Enabled: false              # No necesario y consume memoria
```

---

## 3. Build Settings — Mobile

### Android

```
Target API Level:     34 (Android 14)
Minimum API Level:    26 (Android 8.0 — abarca 97%+ de dispositivos activos)
Scripting Backend:    IL2CPP
Target Architecture:  ARM64 only (eliminar ARMv7 — aumenta compatibilidad y performance)
Compression Format:   LZ4HC (tamaño vs. velocidad de carga óptimo)
Split APK:            Enabled (APK + Expansion Files si > 100MB)
```

**Por qué IL2CPP sobre Mono:**
- IL2CPP compila a código nativo C++ → 15-30% más rápido en runtime
- Mono usa JIT (Just-In-Time) que no está permitido en iOS App Store
- Consistencia: misma configuración en Android e iOS evita bugs platform-specific
- El único costo es el tiempo de compilación (más lento) — aceptable

**Por qué ARM64 only:**
- ARMv7 = dispositivos pre-2014 con < 2GB RAM → no son nuestro target
- Eliminar ARMv7 reduce el tamaño del APK en ~15-20MB
- Google Play recomienda ARM64 desde 2019

### iOS

```
Target iOS: 14.0+    (abarca 99%+ de iPhones activos con captura 2024)
Architecture: ARM64
Metal API: enabled   (no OpenGL ES — deprecated en iOS 12+)
Bitcode: disabled    (Apple lo deprecó en Xcode 14)
```

---

## 4. Performance Targets

### Definición de "dispositivo mínimo soportado"
```
Android: Samsung Galaxy A52 (Snapdragon 720G, 4GB RAM, 2021)
iOS:     iPhone XR (A12 Bionic, 3GB RAM, 2018)
```
Estos son los dispositivos más lentos en los que SKIM DEBE funcionar a 30fps constantes.

### Definición de "dispositivo objetivo"
```
Android: Pixel 7 (Tensor G2, 8GB RAM, 2022)
iOS:     iPhone 14 (A15 Bionic, 6GB RAM, 2022)
```
En dispositivos objetivo, SKIM DEBE funcionar a 60fps constantes.

### Budgets por frame (a 60fps = 16.67ms/frame)

```
CPU (total):          < 10ms
  PhysicsEngine:      < 2ms   (StoneSimulator custom a 120Hz, interpolado al frame rate)
  Gameplay logic:     < 1ms
  Ocean mesh update:  < 3ms   (actualizar vértices del plano procedural)
  UI/Canvas:          < 2ms
  Render + other:     < 2ms

GPU (total):          < 8ms
  Draw calls máximos: 50 / frame
  Triángulos máximos: 50,000 / frame
  Fill rate objetivo: < 3 overdraw (crítico en mobile)
  
Memoria RAM:
  Textures:           < 64MB
  Meshes:             < 16MB
  Audio:              < 32MB  (música procedural + samples)
  Total máximo:       < 200MB (para dejar headroom en dispositivos de 3GB)

Tamaño de app:
  APK / IPA inicial:  < 80MB
  Con assets extra:   < 150MB (sin OBB/expansion files si es posible)
```

### Métrica de "frametime budget" para StoneSimulator
El simulador de física corre a **120Hz** (cada 8.33ms) y es interpolado al frame rate visual. Esto garantiza:
- Precisión física independiente del framerate del dispositivo
- Consistencia entre dispositivos de 30fps y 60fps
- Los rebotes ocurren en el momento correcto aunque el frame sea lento

---

## 5. Patrón de Arquitectura

### ServiceLocator + ScriptableObject Events

**Por qué ServiceLocator (en lugar de Singletons):**
- Los Singletons crean dependencias ocultas difíciles de testear
- ServiceLocator permite swapear implementaciones (ej: MockAudioService en tests)
- Inicialización controlada en el boot sequence

**Por qué ScriptableObject Events (en lugar de C# delegates directos):**
- Los eventos en ScriptableObjects son visibles en el Inspector de Unity
- Eliminan referencias directas entre sistemas → sin memory leaks por referencias no canceladas
- Pueden dispararse desde animaciones, timelines y coroutines sin código adicional

### Módulos principales y sus responsabilidades

```
GameBootstrapper
  └── Registra todos los servicios en el ServiceLocator al inicio

StoneSimulator (Physics — 120Hz, no MonoBehaviour)
  └── Euler integration
  └── Collision detection con el plano de agua (misma ecuación que el shader)
  └── Output: posición, velocidad, estado (en vuelo / en impacto / hundido)
  └── NO depende de Unity Physics — cero Rigidbodies

OceanSystem
  └── Genera el mesh procedural del agua
  └── Actualiza vértices cada frame usando la ecuación de olas
  └── Expone GetHeightAt(x) para el StoneSimulator

InputController
  └── Detecta el gesto de flick (TouchPhase.Began → Ended)
  └── Calcula: ángulo, fuerza, spin del swipe
  └── Emite evento ScriptableObject → StoneSimulator

AudioSystem
  └── Mapea número de salto → nota en escala pentatónica
  └── Gestiona el tempo dinámico (velocidad de saltos = tempo)
  └── Reproduce chord resolution en combo ≥5

ScoringSystem
  └── Fórmula: (distancia × 10) × multiplicador + bonus_zones + clima_multiplier
  └── Gestiona los multiplicadores (1.0x → 15.0x cap)
  └── Emite evento de nuevo récord

ProgressionSystem
  └── Persiste distancia acumulada (PlayerPrefs / local save)
  └── Evalúa desbloqueos (piedras, climas)
  └── NO tiene lógica de juego — solo estado persistido

VFXSystem
  └── Pool de anillos de agua (persistentes, max 200 en sesión)
  └── Splash particles en cada impacto
  └── Coordina con AudioSystem para sincronía audio-visual

UIController
  └── Gestiona transiciones entre pantallas
  └── Actualiza HUD en cada frame con datos del ScoringSystem

EconomySystem
  └── Conchas: balance, earn, spend
  └── Cosmetics: estado desbloqueado de cada skin
  └── Valida que no haya pay-to-win (las piedras se desbloquean solo por distancia)
```

### Regla de comunicación entre módulos

```
PERMITIDO:  ModuloA → ScriptableObjectEvent → ModuloB (desacoplado)
PERMITIDO:  ModuloA.GetService<IModuloB>() (a través del ServiceLocator)
PROHIBIDO:  GameObject.Find() en runtime
PROHIBIDO:  Referencias directas de Inspector entre módulos de gameplay
PROHIBIDO:  Singleton con estado mutable
```

---

## 6. Herramientas Auxiliares

### Incluidas en el proyecto (sin costo)

| Herramienta | Versión | Uso | Alternativa si falla |
|------------|---------|-----|---------------------|
| Unity TextMeshPro | built-in | Todo el texto en UI | — (no hay alternativa razonable) |
| Unity Input System (new) | 1.7.x | Detección de swipe/touch | Legacy Input (fallback) |
| Unity VFX Graph | built-in URP | Partículas de agua, rain | Shuriken (si VFX Graph es muy pesado) |
| Unity Addressables | 1.21.x | Carga asíncrona de assets (piedras, audio) | Resources.Load (más simple) |

### Third-party (Asset Store / Package)

| Herramienta | Costo | Uso | Justificación |
|------------|-------|-----|---------------|
| DOTween Pro | ~$15 USD | Animaciones de UI (tarjetas, score pop, transiciones) | Alternativa a coroutines — 10x más rápido de escribir y más legible |
| Google Play Billing (Unity Plugin) | Gratis | IAP de Conchas en Android | Requerido por Google Play |
| Apple StoreKit (Unity Plugin) | Gratis | IAP de Conchas en iOS | Requerido por App Store |
| GameAnalytics SDK | Gratis (F2P) | Analytics de sessions, retención, funnel | Alternativa a Unity Analytics — más granular para F2P |

### Herramientas de desarrollo (no entran en el build)

| Herramienta | Uso |
|------------|-----|
| Rider (JetBrains) | IDE principal para C# |
| Unity Profiler | Performance debugging |
| Frame Debugger | GPU debugging |
| Android Studio (solo ADB) | Debugging en dispositivo Android |
| Xcode | Build y signing para iOS |
| Git + GitHub | Control de versiones |

---

## 7. Estructura del Proyecto Unity

```
Assets/
├── _Project/               ← Todo el código y assets del proyecto aquí
│   ├── Scripts/
│   │   ├── Core/           ← ServiceLocator, GameBootstrapper
│   │   ├── Physics/        ← StoneSimulator, OceanSystem
│   │   ├── Gameplay/       ← InputController, ScoringSystem, ProgressionSystem
│   │   ├── Audio/          ← AudioSystem, MusicGenerator
│   │   ├── VFX/            ← VFXSystem, WaterRingPool
│   │   ├── UI/             ← UIController, pantallas individuales
│   │   ├── Economy/        ← EconomySystem, IAP wrappers
│   │   └── Events/         ← ScriptableObject events
│   ├── Art/
│   │   ├── Meshes/         ← Piedras, océano base
│   │   ├── Materials/      ← Materiales URP
│   │   ├── Shaders/        ← ShaderGraph assets
│   │   └── VFX/            ← VFX Graph assets
│   ├── Audio/
│   │   ├── Music/          ← Notas pentatónicas, samples de ambiente
│   │   └── SFX/            ← Splash, impacto, combo
│   ├── Data/
│   │   ├── Stones/         ← ScriptableObjects de datos de piedras
│   │   ├── Climates/       ← ScriptableObjects de datos climáticos
│   │   └── Events/         ← ScriptableObject events instances
│   ├── Scenes/
│   │   ├── Boot.unity      ← Solo inicialización, 0 gameplay
│   │   ├── Game.unity      ← Toda la mecánica de juego
│   │   └── UI.unity        ← Pantallas de menú (additive)
│   └── Prefabs/
│       ├── Stones/
│       ├── VFX/
│       └── UI/
├── Packages/               ← Unity Package Manager
├── ProjectSettings/
└── Builds/                 ← Gitignored
```

**Regla:** Nada va en `Assets/` directamente. Todo va en `Assets/_Project/`. Esto evita conflictos con paquetes de terceros que también depositan assets en la raíz.

---

## 8. Pipeline de Build y CI

### Entorno de desarrollo
```
Unity Hub 3.x
Unity 2022.3.52f1
Rider 2024.x o VS Code con C# extension
Git con .gitignore de Unity (incluir Library/ y Builds/)
```

### Build manual (durante desarrollo)
1. `File → Build Settings → Android/iOS`
2. Activar `Development Build` durante Fase 9 (Vertical Slice)
3. Desactivar `Development Build` en el build de release

### Reglas de version control
```
.gitignore incluye:
  Library/
  Temp/
  Builds/
  *.apk
  *.ipa
  UserSettings/

.gitattributes incluye:
  *.unity merge=unityyamlmerge (Unity Smart Merge)
  Assets/Art/**/*.png filter=lfs diff=lfs (Git LFS para assets binarios)
```

---

## 9. Decisiones Rechazadas (y por qué)

| Opción rechazada | Por qué |
|-----------------|---------|
| Unity Physics (Rigidbody) | No permite los 120Hz de simulación ni el control preciso del "feel" de la piedra. El GDD requiere StoneSimulator custom. |
| HDRP | No tiene soporte mobile. Descartado en la primera evaluación. |
| Unreal Engine 5 | Demasiado pesado para el tamaño de app objetivo (<150MB). Nanite y Lumen no aportan nada a un juego low-poly. |
| React Native / Flutter (juego en WebView) | No tiene acceso al hardware GPU necesario para el shader de agua en 60fps. |
| Godot 4 | Buena opción técnicamente, pero el equipo no tiene experiencia y alargaría el desarrollo. |
| Unity DOTS (ECS completo) | Overkill para este proyecto. StoneSimulator custom es más simple y mantenible. |
| Addressables para todo | Solo para assets grandes (piedras, audio). La escena principal usa carga directa. |

---

## 10. Autocrítica — Fase 7

### Fortalezas

**1. La arquitectura modular es testeable**
El ServiceLocator + ScriptableObject Events permite escribir unit tests para el StoneSimulator sin levantar una escena Unity. Esto es crítico en Fase 9 cuando hay que calibrar la física.

**2. El budget de performance es específico**
Decir "debe ser fluido" no es un criterio de aceptación. Decir "50 draw calls máximos, < 10ms CPU total en iPhone XR" sí lo es. El performance se puede medir objetivamente desde el primer día.

**3. IL2CPP + ARM64 es la configuración correcta**
No hay debate válido aquí en 2026. Mono es legacy, ARMv7 está muerto. La configuración correcta está bien documentada.

### Debilidades encontradas

**1. No se especificó el sistema de save**
La persistencia (récords, progresión, conchas) necesita una decisión: ¿PlayerPrefs? ¿JSON en Application.persistentDataPath? ¿Cloud Save? Deuda para Fase 8.

**2. La sincronía audio-visual no está especificada a nivel técnico**
El GDD dice "cada salto = una nota" y la Fase 6 define el mapeo visual. Pero no se especificó si el audio usa AudioSource.PlayOneShot, AudioMixer, o FMOD. Si el audio tiene latencia en Android (común), el "feel" del juego sufre. Riesgo alto.

**Mitigación:** En Fase 9, el primer test de Vertical Slice debe incluir medición de latencia de audio en un dispositivo Android real. Si supera 80ms de latencia percibida, considerar FMOD Studio (gratuito para indie).

### Decisión de avance

La arquitectura técnica está suficientemente especificada para comenzar la Fase 8. No hay decisiones tecnológicas pendientes que puedan bloquear el desarrollo.

**SKIM avanza a Fase 8 — Arquitectura Detallada.**
