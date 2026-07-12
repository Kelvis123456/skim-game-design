# FASE 10 — DESARROLLO COMPLETO
## Proyecto: SKIM

**Fecha:** 2026-06-30
**Entrada:** Vertical Slice validado — la piedra rebota, se siente justa, el jugador dice "otra vez."
**Salida:** SKIM completo, listo para entrar a QA.

---

## Qué se construye en esta fase

El Vertical Slice tenía 1 piedra, 1 clima y sin persistencia. El juego completo tiene 4 piedras, 5 climas, UI completa, progresión, economía, desafíos diarios, tutorial, y monetización.

Esta fase se organiza en **5 milestones** secuenciales. El orden protege el núcleo: no se añade contenido hasta que la base de datos esté sólida, no se añade monetización hasta que el juego valga la pena jugar sin pagar.

---

## Milestone 1 — Base Sólida (2 semanas)

**Objetivo:** La infraestructura que el resto del juego necesita. Todo lo que sigue depende de este milestone estar completo y estable.

### 1.1 — ProgressionSystem completo

```csharp
// El sistema que el VS no tenía
ProgressionSystemImpl:
  - Load() desde JSON en persistentDataPath
  - Save() en OnApplicationPause + OnApplicationQuit
  - TotalAccumulatedDistance: se incrementa con cada lanzamiento, NUNCA se resetea
  - AllTimeRecord: mejor lanzamiento individual histórico
  - IsStoneUnlocked(stone): stone.UnlockDistanceMeters <= TotalAccumulatedDistance
  - IsClimateUnlocked(climate): mismo patrón
  - AvailableStones / AvailableClimates: IReadOnlyList ordenados por unlock distance
```

**Valores de desbloqueo (del GDD):**
| Ítem | Distancia acumulada |
|------|---------------------|
| Guijarro | 0m — disponible desde el inicio |
| Esquisto | 1,000m |
| Brisa | 2,000m |
| Basalto | 4,000m |
| Viento | 5,000m |
| Cuarzo | 8,000m |
| Marejada | 12,000m |
| Tormenta | 25,000m |

### 1.2 — Selección de piedra activa

- El jugador puede elegir qué piedra lanzar antes de cada sesión
- La piedra elegida persiste entre sesiones (guardada en SaveData)
- Las piedras bloqueadas se muestran con candado y distancia necesaria
- Sin pantalla separada todavía — un selector minimal en el área pre-lanzamiento

### 1.3 — EconomySystem base

```csharp
EconomySystemImpl:
  - ConchaBalance: guardado en JSON
  - EarnConchas(amount, reason): solo una fuente por ahora (completar desafíos diarios)
  - SpendConchas(amount, itemId): retorna false si balance insuficiente
  - Sin cosmética todavía — solo el balance y las transacciones
```

### 1.4 — Test de regresión del VS

Antes de avanzar al M2, correr todos los criterios del Vertical Slice de nuevo:
- La física sigue sintiéndose justa
- El audio sigue siendo < 80ms
- 30fps mínimo en el dispositivo piso
- 20 lanzamientos sin crashes

**Si algo regresiona aquí, se arregla antes de M2. Sin excepciones.**

---

## Milestone 2 — Las 4 Piedras (1.5 semanas)

**Objetivo:** El juego tiene toda su variedad de gameplay. El Esquisto, el Basalto y el Cuarzo se sienten completamente distintos al Guijarro.

### 2.1 — ScriptableObjects de las 3 piedras restantes

**Esquisto** — La piedra del spin:
```
ReboundCoefficient: 0.85   // rebota muy bien
ElasticityCoefficient: 0.55 // se aplana en el impacto
SpinSensitivity: 0.80      // el spin la curva dramáticamente
Radius: 0.06               // más delgada
Mass: 0.12
```

**Basalto** — La piedra pesada:
```
ReboundCoefficient: 0.60   // rebota menos, necesita más fuerza
ElasticityCoefficient: 0.80 // dura, se aplana poco
SpinSensitivity: 0.20      // casi no responde al spin
Radius: 0.10               // más grande
Mass: 0.40
```

**Cuarzo** — La piedra impredecible:
```
ReboundCoefficient: 0.78
ElasticityCoefficient: 0.70
SpinSensitivity: 1.20      // un mínimo spin la curva mucho
Radius: 0.07
Mass: 0.18
```

### 2.2 — Modelos 3D y shaders

| Piedra | Forma | Color base | Shader especial |
|--------|-------|-----------|-----------------|
| Guijarro | Elipsoide suave | #B8C5D0 | Standard URP |
| Esquisto | Hexágono plano | #4A6A5A | Standard URP |
| Basalto | Prisma oscuro | #1E2830 | Standard URP |
| Cuarzo | Cristal angular | #C0D8F0 | Semi-transparente (ver riesgo técnico M2) |

**Riesgo técnico del Cuarzo:** El shader semi-transparente requiere sorteo de transparencia. En mobile puede costar 2-3 draw calls adicionales. Implementar con un alpha cutout primero (más barato) y evaluar si el look es aceptable. Solo escalar a transparencia real si el dispositivo piso mantiene > 30fps.

### 2.3 — Pantalla de Selector de Piedras (Fase 5 Pantalla 04)

Implementar la pantalla diseñada en el prototipo UX/UI (ID Pencil: `yn5tP`):
- Fila por piedra con representación 3D (render texture), nombre, descripción
- Stats visuales: Saltos / Spin (barras de color)
- Estado: equipada (teal) / desbloqueada / bloqueada (con distancia)
- Accesible desde tab bar del Menú Principal → "Colección"

### 2.4 — Validación de diferenciación

Test con 5 personas: ¿pueden describir en qué se diferencia el Esquisto del Guijarro después de usar ambas?

Si la respuesta es "no sé, se sienten igual" → ajustar SpinSensitivity o ReboundCoefficient hasta que la diferencia sea obvia.

---

## Milestone 3 — Los 5 Climas (2 semanas)

**Objetivo:** El mar cambia. Cada clima se siente completamente diferente, más difícil, y visualmente distinto.

### 3.1 — ScriptableObjects de los 4 climas restantes

**Brisa** — Primeros pasos en dificultad:
```
HarmonicCount: 3
Harmonics[0]: Amplitude=0.20, WaveLength=10.0, AngularFrequency=0.6
Harmonics[1]: Amplitude=0.12, WaveLength=6.0, AngularFrequency=1.0
Harmonics[2]: Amplitude=0.05, WaveLength=3.5, AngularFrequency=1.8
ClimateMultiplier: 1.3
WaterSurfaceColor: #1A3A5C
SkyHorizonColor: #8DB4D4
```

**Viento** — El mar empieza a interferir:
```
HarmonicCount: 4
Harmonics[0]: Amplitude=0.45, WaveLength=8.0, AngularFrequency=0.8
Harmonics[1]: Amplitude=0.28, WaveLength=4.5, AngularFrequency=1.3
Harmonics[2]: Amplitude=0.15, WaveLength=2.5, AngularFrequency=2.1
Harmonics[3]: Amplitude=0.08, WaveLength=1.5, AngularFrequency=3.2
ClimateMultiplier: 1.7
WaterSurfaceColor: #0D2840
SkyHorizonColor: #4A6A8A
```

**Marejada** — Necesitas dominar el timing:
```
HarmonicCount: 5
Harmonics[0]: Amplitude=0.90, WaveLength=14.0, AngularFrequency=0.5
Harmonics[1]: Amplitude=0.55, WaveLength=7.0, AngularFrequency=0.9
Harmonics[2]: Amplitude=0.30, WaveLength=3.8, AngularFrequency=1.6
Harmonics[3]: Amplitude=0.18, WaveLength=2.0, AngularFrequency=2.8
Harmonics[4]: Amplitude=0.10, WaveLength=1.2, AngularFrequency=4.5
ClimateMultiplier: 2.0
WaterSurfaceColor: #081828
SkyHorizonColor: #1E3050
```

**Tormenta** — El modo experto:
```
HarmonicCount: 6
Harmonics[0]: Amplitude=1.80, WaveLength=18.0, AngularFrequency=0.4
Harmonics[1]: Amplitude=1.10, WaveLength=9.0, AngularFrequency=0.7
Harmonics[2]: Amplitude=0.65, WaveLength=5.0, AngularFrequency=1.2
Harmonics[3]: Amplitude=0.38, WaveLength=2.8, AngularFrequency=2.0
Harmonics[4]: Amplitude=0.22, WaveLength=1.6, AngularFrequency=3.1
Harmonics[5]: Amplitude=0.12, WaveLength=0.9, AngularFrequency=5.0
ClimateMultiplier: 2.5
WaterSurfaceColor: #050E18
SkyHorizonColor: #0A1420
```

### 3.2 — Transición visual entre climas

La transición de 2 segundos (definida en IOceanSystem.SetClimate) necesita:
- Lerp suave de los colores de agua y cielo (Material.Lerp en Update)
- Cambio gradual de la altura de las olas (lerp de amplitudes de armónicos)
- El cambio de clima ocurre al seleccionar en el Selector de Climas — no hay cambio automático mid-sesión (decisión de diseño: el jugador elige su nivel de dificultad intencionalmente)

### 3.3 — Shaders de agua por clima

El shader de océano del VS solo tenía color sólido. En M3 se añade:
- **Normal map** procedural (calculado desde la derivada del mesh — ya la tenemos)
- **Specular highlight** según color de horizonte del clima
- **Foam** en las crestas de las olas (solo para Marejada y Tormenta) — máscara calculada cuando `vertex.y > 0.7 * Amplitude`
- **Partículas de lluvia** para Tormenta — VFX Graph simple, 500 partículas, lifetime 0.3s

### 3.4 — Pantalla de Selector de Climas (Fase 5 Pantalla 05)

Implementar la pantalla diseñada en el prototipo UX/UI (ID Pencil: `xlf3Y`):
- Lista vertical de 5 climas
- Estado: activo (borde teal) / desbloqueado / bloqueado (con distancia)
- Preview visual del clima al tocar una fila (mini ocean render)
- Badge "N/5 niveles desbloqueados"

---

## Milestone 4 — El Meta-Juego (2.5 semanas)

**Objetivo:** El jugador tiene un motivo para volver mañana. Las sesiones tienen estructura. La progresión es visible.

### 4.1 — Tutorial implícito (Física asistida)

La física asistida del VS siempre estaba activa. En M4 se activa solo en las primeras 3 sesiones:

```csharp
// En ProgressionSystem:
int TotalSessionCount { get; }  // se incrementa cada vez que se inicia una sesión

// En GameBootstrapper.WireEvents():
bool assisted = progressionSystem.TotalSessionCount < 3;
```

Además, en la primera sesión se muestra un overlay de tutorial de 1 pantalla:
- "ARRASTRA para apuntar y lanzar" con animación de flick
- "SPIN: curva el gesto para girar la piedra" con animación de curva
- Toca para cerrar — aparece solo 1 vez, nunca más

### 4.2 — Sistema de Desafíos Diarios

```csharp
[CreateAssetMenu(menuName = "SKIM/Daily Challenge")]
public class DailyChallengeData : ScriptableObject
{
    public ChallengeType Type;
    public float Target;         // 100m, 5 saltos, etc.
    public int ConchaReward;
    public string Description;   // "Llega a 100m en un solo lanzamiento"
    
    public enum ChallengeType
    {
        Distance,           // Lanzamiento individual: llega a X metros
        SkipCount,          // Consigue X saltos en un lanzamiento
        ConsecutiveSkips,   // N saltos sin que la velocidad baje de Y
        TotalDistance,      // Suma X metros en la sesión
        ClimateChallenge,   // Lanza en clima X y llega a Y metros
    }
}

// DailyChallengeSystem:
// - Selecciona el challenge del día usando (fecha actual como seed) → determinista
//   mismo día = mismo reto para todos los jugadores
// - Progreso se guarda en SaveData.LastDailyChallenge
// - Al completar: EarnConchas(reward, "daily_challenge")
// - Reset a las 00:00 hora local (comparar fecha en SaveData.LastSaveTimestamp)
```

**Pool de 30 desafíos distintos** — suficiente para un mes sin repetición:
- 10 challenges de distancia (50m, 100m, 150m, ..., 500m)
- 8 challenges de skip count (3, 5, 7, 10 saltos)
- 6 challenges de clima (uno por clima disponible)
- 6 challenges acumulados (totaliza X metros en sesión)

### 4.3 — Pantalla de Desafío Diario (Fase 5 Pantalla 06)

Implementar la pantalla diseñada en el prototipo UX/UI (ID Pencil: `FBXG9`):
- Tarjeta del desafío con tipo, descripción, barra de progreso y reward
- Timer de reset (countdown hasta 00:00)
- 3 objetivos bonus secundarios con estado completado/pendiente
- Bonus: si el jugador completó el challenge, mostrar badge "COMPLETADO ✓"

### 4.4 — Menú Principal completo (Fase 5 Pantalla 01)

Implementar la pantalla `qi1hx` del prototipo — la pantalla hub central:
- Estado del clima activo con preview visual
- Estadísticas de la sesión anterior (distancia del día / récord personal)
- Card del desafío diario con progress
- Botón CTA "LANZAR"
- Tab bar: Inicio / Colección / Desafíos / Ranking / Config

### 4.5 — Pantalla de Post-Lanzamiento completa (Fase 5 Pantalla 03)

Actualizar el card minimal del VS con el diseño completo (ID Pencil: `gyWHi`):
- Badge "¡NUEVO RÉCORD!" animado (DOTween scale bounce)
- Distancia / Saltos / Score
- Dot carousel con los últimos 5 lanzamientos de la sesión
- Botón "OTRA VEZ" + tap en cualquier parte
- Notificación de conchas ganadas si completó challenge

### 4.6 — Splash Screen y Menú Principal inicial (Fase 5 Pantalla 00)

Implementar la pantalla `m00IN`:
- Logo SKIM animado (DOTween fade + scale)
- Tagline
- Animación de la piedra con anillos de impacto
- Auto-avance a Menú Principal en 3s (o tap para saltar)

---

## Milestone 5 — Monetización y Plataforma (2 semanas)

**Objetivo:** El juego puede venderse. IAP funcional, builds limpios para ambas plataformas.

### 5.1 — Sistema de Cosmética

La cosmética de SKIM es **skins visuales de piedras** — no afecta la física.

```
Skins disponibles (cosmética pura):
  - Guijarro Dorado: #D4A843, borde gold — 150 Conchas
  - Guijarro Noche: #1E1E2E, shimmer azul — 200 Conchas
  - Esquisto Jade: #2D6A4F, verde intenso — 250 Conchas (requiere tener Esquisto)
  - Basalto Rojo: #8B0000, textura volcánica — 300 Conchas (requiere tener Basalto)
  - Cuarzo Rosa: #F7CAC9, rosado claro — 350 Conchas (requiere tener Cuarzo)
```

```csharp
// EconomySystem actualizado:
IsCosmeticUnlocked(cosmeticId): bool
GetEquippedStoneSkin(stoneId): string   // devuelve cosmeticId o null
EquipCosmeticSkin(stoneId, cosmeticId): void

// StoneRenderer aplica el skin al inicializar la piedra:
var skinId = economy.GetEquippedStoneSkin(activeStone.name);
if (skinId != null) ApplySkin(skinId);
```

### 5.2 — Pantalla de Colección (nueva — no estaba en el prototipo)

Esta es la pantalla del tab "Colección" del Menú Principal:
- Sección superior: las 4 piedras con su estado de desbloqueo
- Sección inferior: skins disponibles para la piedra seleccionada
- Precio en Conchas / "Equipada" / "Desbloqueada"
- Saldo de Conchas visible en el header

### 5.3 — Paquetes de Conchas (IAP)

```
Bundles disponibles:
  - Puñado: 100 Conchas — $0.99 USD
  - Bolsa: 300 Conchas — $2.49 USD
  - Cofre: 750 Conchas — $4.99 USD
  - Tesoro: 2,000 Conchas — $9.99 USD
```

**Implementación:**
```csharp
// Android: Google Play Billing Library 6.x (Unity IAP plugin)
// iOS: Apple StoreKit 2 (Unity IAP plugin)
// Unity IAP abstrae ambas plataformas con la misma API

EconomySystemImpl.PurchaseConchaBundle(bundleId):
  - Llama a IAPManager.BuyProductID(bundleId)
  - OnPurchaseCompleted: EarnConchas(bundle.amount, "iap_purchase")
  - OnPurchaseFailed: fire event con motivo
  
// Receipts: validar server-side en v2. En v1 (lanzamiento): validar client-side con Unity IAP receipt validation
// No guardar datos de pago localmente — nunca
```

### 5.4 — Ranking Local

El ranking online (leaderboard global) es deuda de v1.1 — requiere backend. En el lanzamiento v1.0, el ranking es **local** (mejores 10 de la sesión actual + récord histórico personal).

```
Pantalla de Ranking (tab 4 del Menú Principal):
  - Lista de los 10 mejores lanzamientos históricos del dispositivo
  - Cada entrada: distancia / skip count / fecha / clima usado
  - El récord personal se destaca en gold
  - Banner: "Ranking global — próximamente" (honesto con el jugador)
```

### 5.5 — Pantalla de Configuración completa (Fase 5 Pantalla 07)

Implementar la pantalla `c8UGR3` del prototipo:
- Sliders de Música y Efectos de Sonido (con persistencia en SaveData)
- Toggles de Vibración / Alto contraste / Texto grande
- Selector de Idioma (Español / English — i18n básico)
- Toggle de Notificaciones (solicitar permiso si se activa — iOS y Android)
- Enlace "Eliminar datos" (muestra alerta de confirmación antes de borrar)

### 5.6 — i18n básico (Español + English)

```
Solo las 2 strings principales que cambian:
  - Todos los textos de UI (botones, labels, mensajes)
  - Nombres de piedras y climas
  - Descripción de desafíos
  
NO localizar:
  - El logo SKIM
  - Los números de score y distancia
  - Los efectos de audio
  
Implementación: ScriptableObject de LocalizationTable con Dictionary<string, string>
  para cada idioma. El idioma se detecta desde Application.systemLanguage.
  Si no es español, default = English.
```

### 5.7 — Builds de release

**Android:**
```
Player Settings:
  Company Name: [NombreEstudio]
  Product Name: SKIM
  Bundle Identifier: com.[estudio].skim
  Version: 1.0.0
  Bundle Version Code: 1
  
Build:
  - Signed APK / AAB con keystore dedicado (NO el keystore de debug)
  - Keystore guardado en lugar seguro fuera del repo
  - Build type: Release (IL2CPP, ARM64, LZ4HC)
  - Target: AAB para Google Play
```

**iOS:**
```
Player Settings:
  Bundle Identifier: com.[estudio].skim
  Version: 1.0
  Build: 1
  
Build:
  - Archive en Xcode con Distribution Certificate
  - Upload a App Store Connect
  - TestFlight para testing externo antes de submit
```

---

## Checklist de Arte — Todo lo que produce en esta fase

El arte se produjo en especificación en Fase 6. En Fase 10 se implementa. Checklist completo:

### Modelos 3D
```
□ Guijarro — elipsoide suave, ~150 tris
□ Esquisto — hexágono plano, ~200 tris
□ Basalto — prisma con corte, ~180 tris
□ Cuarzo — cristal angular, ~240 tris
□ Cada modelo tiene UV map limpio para las skins
```

### Shaders URP
```
□ Ocean Shader — colores por clima, specular, foam en crestas
□ Stone Shader — Standard URP con input de color (para skins)
□ Crystal Shader — alpha cutout para Cuarzo (evaluar full transparencia)
□ Water Ring Shader — emissivo, fade por alpha, color por nota musical
□ Combo Trail Shader — gradient por nivel de combo
```

### VFX Graph
```
□ Splash — 6-12 partículas, controlado por force parameter
□ Rain — solo Tormenta, 500 partículas ligeras
□ Record Stars — efecto de nuevo récord histórico (dorado)
□ Chord Pulse — pulso en todos los anillos activos simultáneamente
```

### UI Assets
```
□ Logo SKIM — vector → texture 512x512
□ Iconos de piedra × 4 (para selector y HUD)
□ Iconos de clima × 5
□ Iconos de tab bar × 5 (Inicio, Colección, Desafíos, Ranking, Config)
□ Iconos de stats (distancia, saltos, score)
□ Badge de Conchas (icono de concha)
□ Botones: estilo teal con borde redondeado
```

### Audio
```
□ 5 notas pentatónicas (generadas sintéticamente en AudioSystem — no son assets)
□ Chord resolution (acorde de Do mayor, reverb leve)
□ Splash SFX (ruido blanco filtrado — generado sintéticamente)
□ Ambiente Calma — olas suaves en loop 30s
□ Ambiente Brisa — olas con viento ligero en loop 30s
□ Ambiente Viento — viento más intenso en loop 30s
□ Ambiente Marejada — olas grandes en loop 30s
□ Ambiente Tormenta — tormenta completa en loop 30s
□ Jingle de nuevo récord (3-4 notas, 0.5s)
□ Sonido de desbloqueo de piedra/clima
```

---

## Gestión de Deuda Técnica

Deuda identificada en fases anteriores que se paga en este milestone:

| Deuda | Origen | Cuándo pagar |
|-------|--------|-------------|
| InputController swipe — cálculo completo | Fase 8 — no especificado | M1, día 1 |
| GetProjectedArc performance | Fase 9 | M1 — después de diagnóstico con profiler |
| Cuarzo shader transparencia vs cutout | M2 especificación | M2, día 3 |
| DailyChallengeSystem sin arquitectura | Fase 8 | M4 |
| Ranking online | M5 scope cut | v1.1 post-lanzamiento |
| Tutorial pantalla completa | Fase 5 autocrítica | M4 |
| Colección de Conchas | Fase 5 autocrítica | M5 |

---

## Métricas de Éxito — Fase 10

### Performance (no regresionar desde el VS)
| Métrica | Target |
|---------|--------|
| FPS dispositivo piso | ≥ 30fps en Galaxy A52 / iPhone XR |
| FPS dispositivo objetivo | 60fps estable en Pixel 7 / iPhone 14 |
| Draw calls (5 climas) | < 80 |
| RAM | < 200MB |
| Tamaño del build (Android AAB) | < 150MB |

### Completitud de features
```
□ Las 4 piedras se sienten distintas
□ Los 5 climas se ven distintos y son más difíciles progresivamente
□ El sistema de save/load funciona — los datos no se pierden al cerrar
□ Los desafíos diarios se resetean correctamente a las 00:00
□ Las Conchas se ganan al completar challenges
□ La cosmética se puede equipar y persiste
□ Los IAP funcionan en entorno de sandbox
□ Las 7 pantallas del prototipo UX/UI están implementadas
□ El tutorial aparece solo en las primeras 3 sesiones
□ El i18n cambia el idioma correctamente según el sistema
```

### Validación subjetiva
La pregunta central de SKIM antes de entrar a QA:

**"¿Hay algo que no tenga sentido, que se sienta roto, o que haría que el jugador dejara de jugar?"**

Si la respuesta tiene más de 3 ítems, no se avanza a QA.

---

## Autocrítica — Fase 10

### Fortalezas del plan

**1. Los milestones están ordenados por dependencia real**
No se puede hacer la cosmética (M5) sin el balance de Conchas (M1). No se puede hacer el desafío diario de clima (M4) sin tener los climas (M3). El orden protege de tener trabajo bloqueado.

**2. La deuda técnica está explícitamente listada**
En lugar de "dejarla para después", cada ítem de deuda tiene una fecha de pago. El GetProjectedArc se diagnostica en M1 — no cuando ya hay 5 climas y 4 piedras y es imposible aislar el problema.

**3. El scope de monetización es honesto**
No se prometió ranking global para v1.0. El banner "próximamente" es mejor que un ranking vacío. Los IAP son sencillos — 4 bundles, sin subscripciones, sin anuncios. Esto reduce el riesgo de rechazo en revisión de las tiendas.

### Debilidades del plan

**1. Las estimaciones de tiempo son optimistas**
5 milestones × promedio 2 semanas = 10 semanas. Para un desarrollador solo, algunos milestones (M3, M5) probablemente toman el doble. M4 especialmente — las pantallas de UI son lentas de implementar bien.

**2. El arte no está en el plan de forma concreta**
El checklist de arte está listado pero no hay un milestone dedicado solo a arte. El riesgo es que los modelos 3D y los shaders se empiecen cuando "quede tiempo" y bloqueen features que los necesitan. El Cuarzo semi-transparente puede tomar días de tweaking de shader.

**3. No hay plan de A/B testing**
La fórmula de score (multiplicadores, velocidad de desbloqueo) se definió en el GDD pero no se ha validado con usuarios reales. Puede que 25,000m para desbloquear Tormenta sea demasiado o demasiado poco. Idealmente habría 2-3 versiones de la curva de progresión para testear. Esto se delega a QA (Fase 11).

### Decisión de avance

El juego completo entra a **Fase 11 — QA y Lanzamiento** cuando:
1. Los 5 milestones están completos
2. Las métricas de performance se cumplen en dispositivo real
3. La validación subjetiva tiene < 3 ítems de "algo roto"
4. Los builds de Android y iOS compilan sin errores en modo Release
