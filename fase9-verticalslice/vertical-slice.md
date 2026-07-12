# FASE 9 — VERTICAL SLICE
## Proyecto: SKIM

**Fecha:** 2026-06-30
**Objetivo:** Lanzar una piedra y que rebote de manera que se sienta justa y satisfactoria.

---

## Qué es el Vertical Slice de SKIM

El Vertical Slice es una versión mínima pero completamente funcional de la experiencia central. No es un prototipo de papel ni un mockup — es código que corre en un dispositivo real.

**No incluye:** monetización, tienda, desafíos diarios, ranking, tutorial, múltiples climas (solo Calma), múltiples piedras (solo Guijarro).

**Sí incluye:** exactamente lo que hace que SKIM sea SKIM.

### Definición de "listo"

El Vertical Slice está terminado cuando alguien que nunca ha jugado toma el teléfono, hace un flick, ve la piedra saltar sobre el agua, escucha las notas musicales, y dice: **"otra vez."**

Ese momento es el criterio. No los fps. No la arquitectura. No el número de características.

---

## Scope del Vertical Slice

### Lo que se construye

| Sistema | ¿Está en el VS? | Alcance |
|---------|----------------|---------|
| Setup Unity + URP | ✓ | Completo — es el día 1 |
| OceanSystem | ✓ | Clima Calma — 2 armónicos |
| StoneSimulator | ✓ | Física completa a 120Hz |
| InputController | ✓ | Swipe detection — ángulo, fuerza, spin |
| AudioSystem | ✓ | Escala pentatónica — 5 notas |
| VFXSystem | ✓ | Anillos de agua + splash |
| ScoringSystem | ✓ | Distancia + multiplicador básico |
| Arco de proyección | ✓ | 3 puntos de trayectoria en tiempo real |
| HUD mínimo | ✓ | Score + distancia + skip count |
| Post-lanzamiento | ✓ | Card con resultados + botón reiniciar |
| Línea de PB | ✓ | Marca visual persistente en el océano |
| Cámara | ✓ | Seguimiento suave de la piedra |
| Múltiples piedras | ✗ | Solo Guijarro |
| Múltiples climas | ✗ | Solo Calma |
| Progression / Save | ✗ | Sin persistencia entre sesiones |
| Cosmética / Economía | ✗ | |
| Desafío Diario | ✗ | |
| Ranking | ✗ | |
| Tutorial | ✗ | Física asistida activa siempre (testing) |
| Menú principal completo | ✗ | Solo pantalla de título mínima |

---

## Plan de Implementación — Sprints

El VS se construye en 4 sprints de 1 semana cada uno. El orden importa: cada sprint produce algo que se puede tocar.

---

### Sprint 1 — La Piedra Existe (Semana 1)

**Objetivo del sprint:** Una piedra cae sobre el agua y rebota. No hay input, no hay HUD. Solo física.

#### Día 1 — Setup del proyecto
```
1. Crear proyecto Unity 2022.3.52f1 con template URP
2. Configurar URP Asset (MSAA 2x, Shadows OFF, Bloom OFF)
3. Instalar paquetes: Input System 1.7.x, TextMeshPro, VFX Graph, DOTween Pro
4. Crear estructura de carpetas _Project/
5. Configurar Build Settings (Android API 26+, ARM64, IL2CPP)
6. Setup .gitignore + Git LFS
7. Crear ScriptableObjects: GuijarroData, CalmaData
8. Primer commit: "Initial setup"
```

#### Días 2-3 — OceanSystem v1
```
Objetivo: El mar se mueve. 
- Generar mesh procedural (80×40 vertices, 50×25 metros)
- Implementar GetHeightAt() con 2 armónicos (valores de Calma del GDD)
- Shader URP básico: color sólido #00C4CC, no transparencia todavía
- UpdateMeshVertices() cada frame
- Test: abrir Scene y ver el mar moverse
```

#### Días 4-5 — StoneSimulator v1
```
Objetivo: La piedra rebota en el agua.
- Implementar StoneSimulatorImpl según spec de Fase 8
- Launch() con valores hardcodeados (ángulo 45°, fuerza 0.8, spin 0)
- Tick() a 120Hz con Euler integration
- ProcessImpact() usando coeficientes de GuijarroData
- Trigger OnImpact / OnSunk eventos
- Test: Play → la piedra sale, rebota 3-5 veces, se hunde
```

**Criterio de salida del Sprint 1:** La piedra rebota de forma que no se ve aleatoria. Si falla (la piedra atraviesa el agua, rebota en ángulo raro, se detiene instantáneamente), no se avanza al Sprint 2.

---

### Sprint 2 — El Jugador Controla (Semana 2)

**Objetivo del sprint:** El jugador hace un swipe y la piedra responde con el ángulo, la fuerza y el spin del gesto.

#### Días 1-2 — InputController
```
Implementar swipe detection:
- TouchPhase.Began → guardar posición inicial + timestamp
- TouchPhase.Moved → calcular vector actual, preview del arco
- TouchPhase.Ended → calcular FlickInput final

Cálculo del FlickInput:
  AngleDegrees = Atan2(delta.y, delta.x) en grados
  Force = Clamp01(swipeSpeed / MAX_SWIPE_SPEED)  // MAX_SWIPE_SPEED = 2000 px/s
  Spin = Clamp(swipeCurvature / MAX_CURVATURE, -1, 1)

Cálculo del spin (curvatura del gesto):
  - Muestrear 5 puntos del swipe durante TouchPhase.Moved
  - Comparar la desviación del punto medio respecto a la línea recta inicio→fin
  - Positivo = right spin, negativo = left spin
```

#### Días 3-4 — Arco de proyección
```
Objetivo: 3 puntos de trayectoria visibles MIENTRAS el jugador arrastra.

Implementar GetProjectedArc():
  - Clonar StoneState inicial con la FlickInput actual
  - Correr el StoneSimulator "en seco" a ×20 velocidad (sin emitir eventos)
  - Capturar las posiciones de los primeros 3 impactos
  - Devolver esos 3 puntos como Vector3[]

Renderizar como 3 círculos blancos semi-transparentes en el agua
Actualizar cada frame durante el drag

Test: arrastrar el dedo — los 3 puntos se mueven en tiempo real y coinciden 
      aproximadamente con donde la piedra aterrizará
```

#### Día 5 — Integración + Primera Jugabilidad
```
- Conectar InputController → StoneSimulator via evento OnFlickDetected
- Conectar OnFlickDrag → GetProjectedArc → actualizar visual
- Desactivar input mientras la piedra está en vuelo (IsEnabled = false)
- Reactivar al hundirse (OnSunk)
- Test: 20 lanzamientos consecutivos sin bugs
```

**Criterio de salida del Sprint 2:** Hacer 10 lanzamientos seguidos sin que el juego crashee o se congele. El arco de proyección da una indicación honesta de la trayectoria (no perfecta, pero en la dirección correcta).

---

### Sprint 3 — Se Siente y Se Escucha (Semana 3)

**Objetivo del sprint:** El juego tiene audio musical y efectos visuales. Cada salto hace algo que se ve y se escucha.

#### Días 1-2 — AudioSystem
```
Implementar escala pentatónica en Do mayor:
  Nota 1 (primer salto): C4 — 261.6 Hz
  Nota 2: D4 — 293.7 Hz
  Nota 3: E4 — 329.6 Hz
  Nota 4: G4 — 392.0 Hz
  Nota 5: A4 — 440.0 Hz
  Nota 6+: C5 — 523.3 Hz (octava superior, loop)
  
Generar cada nota como AudioClip sintético en Awake():
  - Onda senoidal de 100ms + ADSR (Attack 5ms, Decay 20ms, Sustain 70ms, Release 5ms)
  - Este enfoque es 0 assets, sonido "limpio" y minimalista
  
PlaySkipNote(skipNumber): toca la nota asignada con volume = 1 - (skipNumber * 0.03f)
  (las notas posteriores son ligeramente más suaves — sensación de naturalism)

PlayImpactSound(force): clip de "splash" — ruido blanco de 60ms filtrado a 200Hz
  volume = Clamp01(force / 10f)
  
Test de latencia: medir desde OnImpact hasta audio audible
  Si > 80ms percibidos: documentar el problema para investigar FMOD
```

#### Días 3-4 — VFXSystem
```
Anillos de agua (persistentes por sesión):
  - Prefab: Circle mesh de radio 0.5m, shader emissivo teal #00C4CC
  - Al spawnear: escalar de 0 a 3m en 2s (DOTween), fade alpha 1→0 en 60s
  - Color según nota: C=#00C4CC, D=#4ECDC4, E=#7BC8A4, G=#A8D8A8, A=#C8E6C9
  - Límite: 200 anillos activos simultáneos (pool de objetos)

Splash de partículas:
  - 6-12 partículas (según force) que salen en abanico hacia arriba
  - Color blanco/teal, lifetime 0.8s, gravedad 0.5
  - Usar VFX Graph: 1 graph, parameter force controla count y velocity

Score pop-up (minimal para VS):
  - TextMeshPro en world space: "+{score}"
  - Escala de 0→1 en 0.1s, sube 0.5m, fade en 0.8s
  - Solo aparece si score > 0
```

#### Día 5 — Integración + Test de Feeling
```
- Wiring: StoneSimulator.OnImpact → AudioSystem.PlaySkipNote + VFXSystem.SpawnRing
- Wiring: StoneSimulator.OnImpact → VFXSystem.SpawnSplash
- Test de feeling: 30 lanzamientos, evaluar subjetivamente:
  ¿Las notas suenan como una melodía emergente? (sí/no)
  ¿Los anillos hacen que el mar se vea "vivo"? (sí/no)
  ¿El splash da sensación de impacto físico? (sí/no)
Si cualquier respuesta es "no": ajustar antes de seguir.
```

**Criterio de salida del Sprint 3:** El juego ya es disfrutable solo por el audio-visual, sin contar el gameplay. Si se siente como un instrumento musical interactivo, el sprint fue un éxito.

---

### Sprint 4 — El Loop Completo (Semana 4)

**Objetivo del sprint:** El juego tiene un loop completo: lanzar → ver resultados → volver a lanzar. HUD, score, PB, cámara.

#### Días 1-2 — ScoringSystem + HUD
```
ScoringSystem:
  - RegisterImpact(): incrementar skip count, actualizar multiplicador
  - FinalizeLaunch(): calcular score según fórmula de Fase 8
  - OnMultiplierChanged: evento que actualiza HUD

HUD mínimo (solo TextMeshPro, sin diseño artístico todavía):
  - Score grande arriba al centro
  - Multiplicador ×N arriba a la derecha
  - Distancia (metros) arriba a la izquierda
  - Línea de PB: LineRenderer horizontal en la posición del récord de sesión
```

#### Día 3 — Cámara
```
Objetivo: La cámara sigue a la piedra de forma que siempre sea visible sin perder 
          el contexto del mar.

CameraController (MonoBehaviour):
  - En vuelo: la cámara se desplaza horizontalmente con la piedra (smooth follow X)
    damping = 0.15s
  - Altura fija: la cámara NO sube cuando la piedra sube (evita mareo)
  - Al hundirse: suave retorno al origen X en 1.5s (DOTween ease out)
  - Campo de visión: 60° vertical, sin cambios durante el juego
  
Test: lanzar con fuerza máxima — la piedra nunca sale de pantalla
```

#### Día 4 — Post-Lanzamiento Card
```
UI simplificada (sin arte definitivo):
  - Panel semitransparente que aparece al hundirse (DOTween scale 0→1 en 0.2s)
  - Muestra: Distancia / Saltos / Score
  - Botón "OTRA VEZ" + toque en cualquier parte → reiniciar
  - Si nuevo récord de sesión: texto "¡NUEVO RÉCORD!" en gold

Reinicio:
  - StoneSimulator.Reset()
  - ScoringSystem.ResetForNewLaunch()
  - VFXSystem: NO limpiar anillos (persisten en sesión por diseño)
  - Cámara vuelve al origen
  - HUD vuelve a 0
```

#### Día 5 — Build en dispositivo real + Test de Feeling Final
```
CRÍTICO: Este test se hace en dispositivo físico, no en el editor.
  - Build para Android (o iOS según disponibilidad)
  - Instalar en el teléfono objetivo (Galaxy A52 / iPhone XR para el piso de performance)
  
Test de performance:
  - Medir FPS con el Profiler de Unity (target: 30fps mínimo en dispositivo piso)
  - Si < 30fps: identificar el cuello de botella. Candidatos:
    a) RecalculateNormals() del mesh de agua — mitigación: recalcular en hilo separado o reducir frecuencia a cada 2 frames
    b) Pool de VFX activos > 200 — mitigación: reducir límite
    c) GetProjectedArc corriendo simulación "en seco" — mitigación: reducir a 15Hz en lugar de cada frame

Test de latencia de audio:
  - Medir con cronómetro lento si el audio tiene lag perceptible
  - Si > 80ms: activar plan FMOD (ver sección de riesgos)
  
Test de feeling final (las 3 preguntas del Vertical Slice):
  1. ¿Entiendes intuitivamente que fuerza y ángulo importan? (sí/no)
  2. ¿Después de 5 lanzamientos quieres hacer otro? (sí/no)
  3. ¿Cuando el juego dice "+450 pts" sientes que te lo ganaste? (sí/no)
  
Si hay 3 "sí": el Vertical Slice está completo. Se avanza a Fase 10.
Si hay algún "no": se itera antes de avanzar.
```

---

## Riesgos Identificados y Mitigaciones

### Riesgo 1 — La física se siente aleatoria (CRÍTICO, probabilidad: alta)
**Síntoma:** El jugador hace el mismo gesto y la piedra va a lugares distintos.
**Causa real:** Las olas de Calma son lo suficientemente suaves para que casi no afecten. Pero si el timing del lanzamiento cambia (el jugador tarda más en soltar), la posición de la ola es distinta → punto de impacto diferente.
**Esto no es un bug — es física real.** Pero puede sentirse injusto.
**Mitigación:** Si en el test del Sprint 2 se siente injusto, añadir una "ventana de sincronización" de 0.5 segundos al inicio del vuelo donde la ola se evalúa en t=0 (congelar el tiempo de la ola para el primer impacto). Desactivar en Marejada y Tormenta.

### Riesgo 2 — Latencia de audio en Android (CRÍTICO, probabilidad: media)
**Síntoma:** El audio del salto llega 100-200ms después del impacto visual.
**Causa:** Android Audio HAL tiene latencia variable. Unity AudioSource usa AudioTrack → puede acumularse.
**Plan A:** Usar AudioSource.PlayScheduled() con anticipación de 1 frame.
**Plan B (si A no es suficiente):** Integrar FMOD Studio (plugin de Unity). FMOD usa AAUDIO en Android API 26+ que tiene latencia < 20ms.
**Decisión:** Medir en Sprint 3 Día 2. Si latencia > 60ms en Galaxy A52, activar Plan B.

### Riesgo 3 — GetProjectedArc afecta performance (MEDIA, probabilidad: media)
**Síntoma:** El juego baja de FPS mientras el jugador está arrastrando el dedo.
**Causa:** Correr el simulador en modo fantasma cada frame durante el drag es costoso.
**Mitigación:** 
- Throttle: correr GetProjectedArc cada 3 frames en lugar de cada frame
- Si sigue siendo caro: correr en un Job de C# Jobs System (offload a hilo separado)

### Riesgo 4 — El spin no es percibible (MEDIA, probabilidad: media)
**Síntoma:** El jugador no siente que el spin cambia algo.
**Causa:** SpinSensitivity del Guijarro es 0.30 (el más bajo de las 4 piedras) — elegido porque el Guijarro es la piedra "predecible". Pero si el efecto es demasiado sutil, el spin se percibe como inexistente.
**Mitigación:** Si en el test de Sprint 2 el spin no se percibe, subir SpinSensitivity del Guijarro a 0.50 y ajustar los demás proporcionalmente. La escala relativa importa más que los valores absolutos.

### Riesgo 5 — RecalculateNormals() es costoso en mobile
**Causa:** Llamar RecalculateNormals() en una mesh de 80×40 vértices cada frame puede ser caro.
**Mitigación:** Calcular las normales manualmente usando la derivada (GetSlopeAt ya la tenemos) en lugar de recalcularlas. Normal = normalize(-slope, 1, 0). Esto evita el RecalculateNormals() completamente.

---

## Métricas de Éxito

### Performance (cuantitativo)
| Métrica | Target | Mínimo aceptable |
|---------|--------|-----------------|
| FPS (Pixel 7 / iPhone 14) | 60fps estable | 55fps |
| FPS (Galaxy A52 / iPhone XR) | 45fps | 30fps |
| Draw calls | < 50 | < 80 |
| Memoria RAM | < 150MB | < 200MB |
| Latencia de audio | < 40ms | < 80ms |
| Tamaño del build | < 80MB | < 120MB |

### Feeling (cualitativo — test con 3 personas externas)
| Pregunta | Respuesta esperada |
|---------|-------------------|
| "¿Qué controlas en el gesto?" | Menciona al menos 2 de: dirección, fuerza, spin |
| "¿Cuántos lanzamientos quieres hacer?" | "Más de 10" |
| "¿Cuándo la piedra rebota muchas veces, ¿sientes que fue tu mérito?" | "Sí" |
| "¿Recuerdas algún sonido del juego?" | Describe algo de la melodía |

---

## Estructura de Scenes en Unity

```
Boot.unity
  └─ GameBootstrapper (DontDestroyOnLoad)
     └─ Carga aditiva →

Game.unity
  ├─ Ocean (OceanSystemImpl)
  ├─ Stone (StoneSimulatorImpl — el prefab se instancia aquí)
  ├─ Camera (CameraController)
  ├─ VFX (VFXSystemImpl — parent de todos los pools)
  └─ [Lights] — 1 directional light suave, temperatura 6500K, intensidad 0.8

UI.unity
  ├─ HUD Canvas (Screen Space — Camera)
  │   ├─ Score Label
  │   ├─ Distance Label
  │   ├─ Multiplier Badge
  │   └─ ProjectedArc (3 circle renderers)
  └─ PostLaunch Canvas (Screen Space — Camera, initially hidden)
      ├─ ResultCard
      └─ RetryButton
```

---

## Configuración Inicial de ScriptableObjects

### GuijarroData
```
StoneName: "Guijarro"
ReboundCoefficient: 0.72
ElasticityCoefficient: 0.65
SpinSensitivity: 0.30
Radius: 0.08
Mass: 0.15
UnlockDistanceMeters: 0
```

### CalmaData — Armónicos
```
ClimateName: "Calma"
UnlockDistanceMeters: 0
ClimateMultiplier: 1.0
DifficultyRating: 1
HarmonicCount: 2

Harmonics[0]:
  Amplitude: 0.05      // 5cm — prácticamente flat
  WaveLength: 12.0     // olas largas y suaves
  AngularFrequency: 0.4
  InitialPhase: 0      // se randomiza en SetClimate()

Harmonics[1]:
  Amplitude: 0.03      // 3cm
  WaveLength: 7.0
  AngularFrequency: 0.7
  InitialPhase: 0
```

---

## Autocrítica — Fase 9

### Fortalezas del plan

**1. El orden de los sprints es el correcto**
Sprint 1 valida que la física funciona antes de invertir en audio/VFX. Si la física falla, el juego no existe. No tiene sentido construir el HUD antes de que la piedra rebote bien.

**2. Los criterios de salida son concretos y binarios**
"La piedra rebota de forma que no se ve aleatoria" es subjetivo pero decidible. "20 lanzamientos sin crashes" es objetivo. Cada sprint tiene un contrato claro.

**3. El test de feeling final pregunta lo correcto**
Las 3 preguntas del Sprint 4 Día 5 no preguntan "¿te gustó el juego?" — preguntan si el jugador entendió el control, si quiere seguir, y si siente agencia. Esas son las 3 cosas que hacen que Helix Jump sea adictivo.

### Debilidades del plan

**1. No hay estimación de horas por tarea**
El plan asume 1 semana por sprint pero no desglosa cuánto tiempo toma cada tarea. Si el StoneSimulator tarda más de 2 días, el Sprint 1 se rompe. Se necesita un buffer.

**2. El test con personas externas no está planeado logísticamente**
"3 personas externas" en el test del Sprint 4 — ¿quiénes? ¿Cómo se les pasa el build? Para Android: APK por WhatsApp. Para iOS: TestFlight (requiere cuenta de desarrollador activa). Si no hay cuenta de Apple, solo se puede testear en Android.

**3. La cámara es un sistema no trivial**
"Seguir a la piedra de forma que siempre sea visible" parece simple pero tiene edge cases: ¿qué pasa si la piedra sale de pantalla hacia arriba en un rebote muy alto? ¿La cámara la sigue? Si sí, puede marear. Si no, el jugador pierde de vista la piedra. Requiere iteración.

### Decisión de avance

Cuando los 4 sprints estén completos y los criterios cualitativos del test de feeling sean 3/3 "sí", SKIM avanza a **Fase 10 — Desarrollo Completo.**

La Fase 10 es donde se construye el resto: múltiples piedras, múltiples climas, UI completa según el prototipo de Fase 5, monetización, desafíos diarios. Todo lo que no está en el VS.
