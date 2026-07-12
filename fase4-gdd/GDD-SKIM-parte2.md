# SKIM — Game Design Document
## Parte 2: HUD, Audio, Arte, Accesibilidad, Guardado, LiveOps, Arquitectura, QA, Roadmap

**Versión:** 1.0  
**Fecha:** 2026-06-30

---

## 12. HUD E INTERFAZ

### 12.1 Pantallas del juego

| Pantalla | Descripción |
|----------|-------------|
| Splash | Logo + loading |
| Main Menu | Inicio de sesión, stats de hoy, acceso a modos |
| Gameplay | El océano + piedra + HUD mínimo |
| Selector de clima | Grid de 5 climas, estado de desbloqueo |
| Selector de piedra | 4 piedras base + cosméticos |
| Colección | Todas las piedras y entornos, cuáles tienes |
| Desafíos | Desafío diario + historial |
| Leaderboard | Top 100 de la semana + posición propia |
| Configuración | Audio, accesibilidad, idioma, cuenta |
| Stats | Récords personales por clima y piedra |

### 12.2 HUD de gameplay

El HUD es intencionalmente mínimo. El océano y la piedra son el protagonista.

**Elementos en pantalla durante gameplay:**

```
┌─────────────────────────────────────────┐
│  [PB: 847]           [SCORE: 1,240]      │
│                                          │
│                                          │
│         ·  ·  ·  (arco proyección)       │
│                                          │
│   ≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈     │
│   ≈≈≈≈≈ OCÉANO ≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈     │
│                                          │
│  [○] [○○] [○○○]    [✕ 3]  [⊙ Viento]  │
│  (piedra)  (clima)   (ads)   (pausa)    │
└─────────────────────────────────────────┘
```

**Elementos detallados:**

- **Score actual** (centro-arriba): grande, sans-serif, blanco. Se anima al sumar.
- **Personal Best** (izquierda arriba): pequeño, semitransparente. Siempre visible para comparación.
- **Arco de proyección** (3 puntos semitransparentes): solo visible mientras el dedo está en pantalla.
- **Skip counter** (aparece durante el vuelo): número de saltos que lleva la piedra, se anima por cada skip. Desaparece al hundirse.
- **Multiplicador flotante** (aparece al tocar zona): "+0.3x" flota y sube hacia el score cuando toca zona bonus.
- **Selector de piedra** (abajo izquierda): thumbnail de la piedra activa. Tap para cambiar.
- **Indicador de clima** (abajo centro): ícono pequeño del clima activo.
- **Anuncios recompensados** (abajo derecha): ícono discreto de "×2". Solo visible cuando está disponible.
- **Pausa** (arriba derecha): ícono mínimo de pausa.
- **Línea de récord** (en el océano): línea visual tenue que marca hasta dónde llegó tu mejor piedra de la sesión.

### 12.3 Post-lanzamiento (2 segundos)

Al hundirse la piedra, aparece un overlay mínimo sobre el océano:

```
        ╔══════════════════╗
        ║  47.3m  |  5 skips  ║
        ║     +1,240 pts    ║
        ║   ⬆ PB! distancia ║
        ╚══════════════════╝
```

Desaparece en 2 segundos o al primer tap. No hay botón de "continuar" — el tap reinicia directamente.

### 12.4 Main Menu

- Fondo: el océano en animación suave (idle, clima seleccionado)
- Centro: logo SKIM
- Botón grande: "LANZAR" (único CTA principal)
- Abajo: iconos de Colección / Desafíos / Leaderboard / Configuración
- Pequeño: stats del día (metros hoy, tiradas hoy, desafío: completado/pendiente)

### 12.5 Flujo de navegación

```
Splash → Main Menu
                ↓ LANZAR
                Gameplay (loop indefinido)
                ↓ pausa
                Menú de pausa → Configuración / Menú principal
                
Main Menu → Selector de clima → Gameplay
         → Selector de piedra (en colección o gameplay)
         → Desafíos
         → Leaderboard
         → Configuración
         → Stats
```

---

## 13. AUDIO

### 13.1 Filosofía de audio

El audio en SKIM no es ambientación — es feedback. El jugador literalmente "escucha" su habilidad. Un combo perfecto suena como una melodía espontánea. Un lanzamiento pobre suena como... el silencio después de un hundimiento.

### 13.2 Soundtrack de ambiente

No existe un soundtrack de música tradicional. El ambiente sonoro del océano IS la música de fondo.

**Por clima:**

| Clima | Ambiente | BPM feeling |
|-------|----------|-------------|
| Calma | Olas suaves, gaviotas lejanas, brisa ligera | Calmo, 50-60 BPM |
| Brisa | Olas medias, viento audible, crujido de la embarcación | Moderado, 70 BPM |
| Viento | Viento fuerte, olas constantes, spray | Activo, 85 BPM |
| Marejada | Oleaje potente, viento rugiente, truenos lejanos | Tenso, 95 BPM |
| Tormenta | Tormenta completa, lluvia, truenos cercanos | Dramático, 110 BPM |

El crossfade entre climas es gradual (3 segundos). No hay cortes abruptos.

### 13.3 Efectos de sonido por evento

**Lanzamiento:**
- Sonido de piedra deslizándose + whoosh de vuelo (pitch varía con velocidad y spin)

**Skip (cada rebote):**
- Nota musical de la escala pentatónica de Do mayor: C, D, E, G, A
- El salto 1 siempre es C (Do)
- El salto 2 es D (Re)
- Salto 3: E (Mi)
- Salto 4: G (Sol)
- Salto 5: A (La)
- Salto 6+: secuencia aleatoria de las notas disponibles (siempre consonante con la tónica)
- Efecto de agua simultáneo: "splap" + pequeña resonancia
- Pitch del "splap" varía ligeramente por velocidad del impacto

**Combo 5+ skips:**
- En el hundimiento final: acorde mayor (C-E-G) como "resolución"
- Sensación de "melodía completada"

**Zona bonus:**
- Grande: chime único (sin pitch musical)
- Mediana: chime más brillante + shimmer
- Pequeña: arpeggio ascendente (3 notas)
- Especial: arpeggio completo + campanada resonante

**Hundimiento final:**
- "Plop" de agua grave + pequeñas burbujas (2-3 burbujitas en fade)
- Si era lanzamiento de ≥6 skips: el "plop" tiene una pequeña reverberación satisfactoria

**Personal Best:**
- Fanfarria breve (2 segundos, 4 notas ascendentes)
- Diferente para best de distancia vs. best de saltos

**Desbloqueo:**
- Sonido de "tesoro encontrado" (3 notas ascendentes + shimmer)

### 13.4 Mezcla

- Ambiente océano: siempre presente, dinámica según clima (más fuerza en Tormenta)
- Efectos de gameplay: en frente del ambiente, nunca tapados
- Notas musicales de skip: levemente reverberadas para que se siendan en el espacio del océano
- Balance general: el océano no debe "competir" con los efectos — está en segundo plano

### 13.5 Configuración de audio

- Volumen master
- Volumen efectos (separado)
- Volumen ambiente (separado)
- Modo silencioso: desactiva todo excepto la opción de vibración háptica

---

## 14. ARTE Y DIRECCIÓN VISUAL

### 14.1 Estilo artístico

**Low-poly stylized.** No realista, no pixel art.

La razón: el low-poly estilizado permite representar agua convincente con shaders eficientes, y se ve bien en todos los rangos de hardware Android/iOS (desde gama baja a gama alta). El realismo fotográfico requeriría física de agua que no es viable en móvil. El pixel art no captura la liquidez del agua.

**Referencia visual:** La estética "limpia y natural" de Monument Valley + la paleta oceánica de Journey.

### 14.2 Paleta de color por clima

| Clima | Agua | Cielo | Espuma | Luz |
|-------|------|-------|--------|-----|
| Calma | #1B6CA8 → #2BB5A0 | #FF9966 → #FF5E62 (atardecer) | #E8F4F8 | Dorada cálida |
| Brisa | #1A5276 → #2E86C1 | #85C1E9 → #AED6F1 | #EAF2FF | Luz diurna neutra |
| Viento | #1A4A6E → #1F618D | #7F8C8D → #95A5A6 | #D6EAF8 | Luz fría |
| Marejada | #154360 → #1A5276 | #566573 → #717D7E | #D0D3D4 | Luz gris plana |
| Tormenta | #1C2833 → #2C3E50 | #212F3C → #1B2631 | #BFC9CA | Relámpagos |

### 14.3 El océano (shader)

El agua de SKIM es generada por shader, no por asset estático. Usa la misma función de suma de sinusoides que la física del rebote (consistencia visual-física garantizada).

**Capas del shader de agua:**
1. Geometría base: mesh plano de baja resolución que se deforma con las ecuaciones de ola
2. Normal map animado: genera el detalle de ondas superficiales pequeñas
3. Reflections: reflexión simplificada del cielo
4. Foam: espuma en las crestas de olas altas (solo en Marejada y Tormenta)
5. Depth color: el agua es más oscura "más lejos" del jugador

### 14.4 La piedra

La piedra es el elemento más importante del juego — el jugador la ve constantemente. Cada tipo tiene una forma característica reconocible en thumbnail.

**Animaciones de la piedra:**
- En vuelo: rotación en el eje Y (tumble natural), ligero tilt según el spin
- En impacto: deformación breve (squash) por 2-3 frames
- En hundimiento: la piedra entra al agua, genera splash de partículas, desaparece

**Nivel de detalle:**
- La piedra en pantalla es pequeña pero su forma es legible a cualquier distancia
- El nivel de detalle de la textura permite distinguir el tipo de piedra de inmediato

### 14.5 Anillos de agua (VFX)

El sistema de anillos es el elemento visual más característico de SKIM.

**Por skip:**
- Anillo expandiéndose desde el punto de impacto (radio 0 → 2m en 1 segundo)
- Color: blanco con baja opacidad
- 3-5 anillos concéntricos por impacto (los externos más transparentes)
- Los anillos persisten en pantalla con fade gradual de 60 segundos
- El agua detrás de la piedra muestra todos los anillos de la sesión = trayectoria visible

**Por zona bonus:**
- Burst de partículas en el color de la zona (verde/azul/dorado/arco iris)
- Escala: 2-3x el tamaño del anillo normal
- Duración: 0.5 segundos, rápido y satisfactorio

**Por hundimiento final:**
- Splash de agua: partículas hacia arriba y outward
- Un último anillo grande (el del hundimiento)
- Pequeñas burbujas que emergen y desaparecen

### 14.6 Partículas

- Sistema: Unity VFX Graph (GPU particles)
- Presupuesto por frame: máximo 500 partículas simultáneas en gama baja, 2000 en gama alta
- LOD: en dispositivos detectados como "low-end", los anillos usan menos polígonos y las partículas de splash son menos

### 14.7 UI visual

- Tipografía: Poppins (moderna, redondeada, legible en pantallas pequeñas)
- Peso regular para cuerpo, semibold para números de score
- Iconografía: minimalista, sin relleno (outline style)
- El score usa tipografía tabular (números no-proporcionales para que no "salte" al cambiar de valor)
- Animaciones de UI: spring physics (iOS-like), 200ms para transiciones

---

## 15. ACCESIBILIDAD

### 15.1 Visual

- **Daltonismo:** Las zonas de bonus distinguibles por forma Y color. Verde = círculo sólido. Azul = círculo con borde punteado. Dorado = estrella. Especial = círculo con gradiente.
- **Contraste:** El HUD tiene fondo semitransparente oscuro detrás de texto blanco. WCAG AA mínimo.
- **Tamaño de texto:** 3 opciones (normal / grande / muy grande). Afecta solo HUD y menús, no gameplay.
- **Modo alto contraste:** Activa colores de UI de máximo contraste (blanco sobre negro para HUD).

### 15.2 Motor

- **Asistencia de física permanente:** Toggle en configuración. Activa permanentemente la asistencia de ≥2 skips por lanzamiento (no solo en los primeros 3). Para jugadores con dificultad motriz.
- **Reducción de efectos:** Toggle que desactiva los efectos de partículas complejos. Para jugadores con sensibilidad visual o dispositivos lentos.
- **Modo sin brillos:** Desactiva destellos y flashes de luz. Para fotosensibilidad.

### 15.3 Interfaz

- **Modo zurdo:** Espeja todos los elementos de HUD.
- **Área de swipe:** La zona activa de swipe ocupa pantalla completa — no hay zonas muertas inaccesibles.
- **Texto descriptivo:** Todos los ícones tienen etiqueta de texto alternativo para lectores de pantalla.

### 15.4 Audio

- **Hápticos:** Vibración al cada skip, al hundirse, al alcanzar zona bonus. Configurable (on/off, intensidad).
- **Subtítulos de efectos:** Para jugadores con audición reducida, aparece texto "[SKIP x3]" "[BONUS ZONE]" cuando ocurren eventos importantes.

---

## 16. SISTEMA DE GUARDADO

### 16.1 Datos guardados

**Local (PlayerPrefs + JSON):**
- Distancia acumulada total
- Records personales (por clima, por piedra): distancia, saltos, score
- Piedras desbloqueadas
- Climas desbloqueados
- Cosméticos en posesión
- Conchas actuales
- Desafío diario: estado (completado/pendiente) + fecha
- Preferencias de usuario: volumen, accesibilidad, idioma, handedness
- Última piedra y clima seleccionados

**Nube (GameCenter / Google Play Games):**
- Distancia acumulada total
- Records personales
- Cosméticos (para prevenir pérdida en reinstalación)

### 16.2 Sincronización

- Sincronización automática al cerrar la app
- Al abrir la app: compara timestamp local vs. nube. El más reciente gana.
- Conflicto de datos: siempre prevalece la distancia mayor (evita perder progreso)

### 16.3 Backup

- Export de datos local disponible en configuración
- La reinstalación recupera progreso si el jugador está logueado con su cuenta de GameCenter/Google

---

## 17. LIVEOPS

### 17.1 Calendario de contenido

| Frecuencia | Tipo | Descripción |
|-----------|------|-------------|
| Diario | Desafío | Un nuevo objetivo específico |
| Semanal | Leaderboard | Reset + cosmético para top 10 |
| Mensual | Evento estacional | Cosmético temático + challenge especial |
| Trimestral | Update de contenido | Nueva piedra o nuevo entorno |

### 17.2 Eventos especiales

**Storm Week** (2x al año):
- El clima Tormenta se desbloquea temporalmente para todos los jugadores (incluyendo los que no lo han desbloqueado aún)
- Leaderboard especial de 7 días solo en Tormenta
- Cosmético exclusivo para el top 3
- Propósito: muestra a todos los jugadores el máximo desafío del juego, crea aspiración

**Evento Estival (verano):**
- Entorno cosmético "Mediterráneo" — agua turquesa, costa rocosa, luz mediterránea
- 4 skins de piedra temáticas (mármol, cerámica, coral, vidrio de mar)
- Bundle IAP + versión ganable con Conchas

**Evento Nocturno (invierno):**
- Entorno "Bioluminiscente" — agua oscura con destellos de luz en cada impacto
- Las notas musicales del skip tienen reverberación adicional
- Las zonas bonus brillan más intensamente
- Atmósfera completamente diferente del juego base

### 17.3 Notificaciones push

Máximo 1 notificación por día. Nunca más de 1.

| Trigger | Mensaje | Prioridad |
|---------|---------|----------|
| 24h sin jugar | "El océano te espera. Desafío nuevo disponible." | Alta |
| Desafío diario disponible (08:00 local) | "Nuevo desafío: [nombre del desafío]" | Media |
| Storm Week inicia | "La tormenta llegó. ¿Cuántos saltos puedes hacer?" | Alta |
| Personal best superado por amigo | "[Amigo] llegó a 67m. ¿Puedes superarlo?" | Media |

El jugador puede desactivar categorías de notificaciones individualmente.

---

## 18. ARQUITECTURA TÉCNICA

### 18.1 Engine y stack

- **Engine:** Unity 2022 LTS + URP 14.x
- **Target Android:** API 26+ (Android 8.0), ARM64
- **Target iOS:** iOS 14+, ARM64
- **Lenguaje:** C#
- **Compilación:** IL2CPP

### 18.2 Módulos del juego

```
SKIM/
├── Core/
│   ├── GameManager          — Bootstrapper, estado global
│   ├── ServiceLocator       — DI container (no Singletons)
│   ├── SceneLoader          — Carga aditiva de escenas
│   └── AppLifecycle         — Pausa/resume, backgrounding
│
├── Physics/
│   ├── StoneSimulator       — Motor de física custom del rebote
│   ├── OceanSimulator       — Generador de ondas (misma ecuación que shader)
│   └── TrajectoryPredictor  — Calcula 3 puntos de arco pre-lanzamiento
│
├── Input/
│   ├── SwipeDetector        — Captura flick: dirección, velocidad, curvatura
│   └── GestureAnalyzer      — Traduce gesto a parámetros (ángulo, fuerza, spin)
│
├── Gameplay/
│   ├── LaunchController     — Orquesta el ciclo completo de un lanzamiento
│   ├── SkipTracker          — Cuenta saltos, acumula multiplicador
│   ├── BonusZoneManager     — Genera zonas, detecta hits
│   ├── SessionManager       — Maneja la sesión completa (múltiples lanzamientos)
│   └── ScoreCalculator      — Fórmula completa de puntuación
│
├── Ocean/
│   ├── OceanRenderer        — Puente entre OceanSimulator y el shader de agua
│   ├── RingSystem           — Genera y gestiona anillos persistentes en pantalla
│   └── ClimateManager       — Estado del clima activo, transiciones
│
├── Progression/
│   ├── SaveSystem           — Lectura/escritura de datos locales y nube
│   ├── UnlockSystem         — Evalúa condiciones de desbloqueo
│   ├── ChallengeManager     — Desafíos diarios: generación, tracking, recompensa
│   └── CurrencyManager      — Gestión de Conchas
│
├── Audio/
│   ├── AudioManager         — Gestión de todos los clips
│   ├── SkipMusicSystem      — Asigna notas musicales a cada skip
│   └── OceanAmbience        — Ambience procedural según clima
│
├── UI/
│   ├── HUDController        — Score, PB, skip counter, arco
│   ├── MainMenuController   — Main menu, stats del día
│   ├── CollectionController — Piedras y entornos disponibles
│   ├── ChallengeUIController — Vista de desafío diario
│   ├── LeaderboardController — Top 100 + posición propia
│   └── SettingsController   — Audio, accesibilidad, configuración
│
└── VFX/
    ├── StoneFX              — Partículas de vuelo y hundimiento
    └── BonusZoneFX          — Burst de partículas por zona
```

### 18.3 Patrón de comunicación

**Eventos ScriptableObject** para comunicación entre módulos desacoplados.

```csharp
// Ejemplo: StoneSimulator dispara evento → SkipTracker lo escucha → ScoreCalculator reacciona
GameEvent<SkipData> onSkipOccurred;
GameEvent<SinkData> onStoneSank;
GameEvent<BonusZoneData> onBonusZoneHit;
GameEvent<LaunchData> onLaunchInitiated;
```

Ningún módulo tiene referencia directa a otro. Todo a través de eventos. Esto permite testing unitario independiente de cada módulo.

### 18.4 Física custom — StoneSimulator

El StoneSimulator opera a 120Hz (independiente del framerate visual) usando Euler integration.

```csharp
// Cada tick de física:
void Simulate(float dt) {
    // 1. Obtener pendiente de ola en posición actual
    float waveSlope = oceanSim.GetSlopeAt(position.x, Time.time);
    
    // 2. Aplicar gravedad
    velocity.y -= gravity * dt;
    
    // 3. Detectar impacto con superficie de agua
    float waterHeight = oceanSim.GetHeightAt(position.x, Time.time);
    if (position.y <= waterHeight) {
        float impactAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        
        if (impactAngle < -MIN_SKIP_ANGLE || impactAngle > -MAX_SKIP_ANGLE) {
            // Hundimiento
            OnSink();
        } else {
            // Skip
            ApplyRebound(waveSlope);
            OnSkip();
        }
    }
    
    // 4. Avanzar posición
    position += velocity * dt;
}
```

### 18.5 Rendimiento objetivo

| Métrica | Gama baja | Gama media | Gama alta |
|---------|-----------|-----------|----------|
| FPS | 30 estables | 60 estables | 60/120 |
| Tiempo de carga | < 3s | < 2s | < 1.5s |
| RAM uso | < 180MB | < 250MB | < 350MB |
| Temperatura tras 10min | < +5°C | < +4°C | < +3°C |
| Batería por hora | < 12% | < 8% | < 6% |

**Estrategias de optimización:**
- El OceanSimulator corre en Job System (multihilo) — no bloquea el main thread
- Los anillos de agua usan instanced rendering (un draw call para todos los anillos)
- Los partículas usan VFX Graph (GPU) — el CPU no participa en el cálculo
- El StoneSimulator usa pooling — las piedras se reciclan, no se instancian
- Adaptive quality: el sistema detecta la temperatura del dispositivo y reduce efectos automáticamente si supera umbral

### 18.6 Herramientas de terceros

| Herramienta | Uso | Costo |
|-------------|-----|-------|
| Unity 2022 LTS | Engine | Free (Personal) |
| GameAnalytics | Analytics | Free |
| Firebase Crashlytics | Crash reporting | Free |
| Unity Ads 4.x | Rewarded ads | Free (revenue share) |
| Unity IAP 4.x | In-app purchases | Free |
| Apple GameCenter | Leaderboards iOS | Free |
| Google Play Games | Leaderboards Android | Free |

**Costo total de stack:** $0 hasta revenue significativo.

---

## 19. QA

### 19.1 Checklist de cada build

**Funcional:**
- [ ] El arco de proyección aparece con el primer toque y desaparece al soltar
- [ ] Los primeros 3 lanzamientos siempre producen ≥2 saltos (física asistida activa)
- [ ] La física asistida se desactiva correctamente después del 3er lanzamiento
- [ ] Los anillos de agua persisten y hacen fade en 60 segundos
- [ ] El tap post-hundimiento genera nueva piedra en <0.5 segundos
- [ ] La nota musical correcta se reproduce por cada salto (do-re-mi-sol-la)
- [ ] El score se calcula correctamente con la fórmula de multiplicador
- [ ] Las zonas bonus se detectan solo en skips, no en el hundimiento final
- [ ] El record personal se actualiza y persiste entre sesiones
- [ ] El desafío diario se refresca a las 00:00 local

**Rendimiento:**
- [ ] 30fps estables en dispositivo de gama baja target (Motorola G32 o equivalente)
- [ ] Sin frame drops durante combo de 8+ saltos con partículas completas
- [ ] Tiempo de carga desde splash hasta gameplay < 3s en gama baja
- [ ] Sin memory leak tras 20 lanzamientos consecutivos

**UX:**
- [ ] El primer lanzamiento produce al menos 2 saltos sin instrucción previa
- [ ] El jugador nuevo entiende que tap = nuevo intento (testear con persona no familiarizada)
- [ ] Las zonas bonus son visualmente distinguibles a 1m de distancia de pantalla
- [ ] El HUD no tapa ningún elemento de gameplay crítico

**Accesibilidad:**
- [ ] Modo daltonismo: todas las zonas distinguibles por forma
- [ ] Modo alto contraste: HUD legible
- [ ] Modo zurdo: UI completamente funcional espejada

### 19.2 Criterios Go/No-Go para Fase 10

| Criterio | Umbral mínimo |
|----------|--------------|
| Física se siente justa | 0 casos de "el juego me hizo perder sin razón clara" en 5+ testers |
| Primer lanzamiento satisfactorio | 100% de testers consigue ≥2 saltos en primeras 3 tiradas |
| El reinicio es instantáneo | Tiempo medido < 500ms en gama baja |
| El audio de notas funciona | El jugador puede "escuchar" su combo sin que suene a ruido |
| Fun score | Promedio ≥4.0/5.0 en test de 10 minutos con 5+ testers |

### 19.3 Bugs conocidos de diseño (a resolver antes de Fase 10)

- El multiplicador máximo teórico (15+ saltos en Tormenta con zona especial) puede generar scores absurdos → Definir cap de multiplicador (propuesta: 15.0x máximo)
- La línea de récord en el océano puede confundirse con una ola si no tiene suficiente contraste → Evaluar color/grosor en pruebas de usabilidad
- El "plop" de hundimiento en Tormenta puede perderse en el ruido de ambiente → Priorizar el plop en el mix cuando el clima es alto

---

## 20. ROADMAP

### v1.0 — Launch (MVP)
- Guijarro, Esquisto, Basalto, Cuarzo
- 5 climas
- Sistema de scoring completo
- Desafíos diarios
- Leaderboard semanal
- Colección de cosméticos básicos
- Monetización: Remove Ads + 2 bundles

### v1.1 — Ghost Update
- Sistema de ghost: el replay de tu mejor lanzamiento de la semana visible mientras juegas
- Leaderboard de ghosts: puedes ver el lanzamiento de los top 3 del leaderboard

### v1.2 — Social
- Compartir replay del mejor lanzamiento (video de 15 segundos)
- Comparar stats con amigos (GameCenter/Google Play)
- Desafíos de amigos: "te reto a superar mis 67m"

### v1.3 — Nueva piedra
- Obsidiana: piedra volcánica extremadamente plana, muy difícil de controlar, récords imposibles cuando se domina
- Nuevo tipo de zona bonus: zona "corriente" que mueve la piedra horizontalmente en el skip

### v2.0 — Nuevo entorno
- "Cañón de Río": entorno completamente diferente. Corriente del río afecta la dirección de los saltos. Paredes del cañón crean zonas de sombra y luz. Física diferente (el río fluye, el océano oscila).

---

## 21. AUTOCRÍTICA — FASE 4

### Fortalezas del GDD
- La física custom (StoneSimulator) está bien especificada — un developer puede implementarla directamente
- La tabla de desbloqueos es simple y predecible — no hay curvas de progresión misteriosas
- El sistema de audio emergente (notas musicales) está completamente definido sin dejar ambigüedad
- La monetización es ética por diseño — el pay-to-win es físicamente imposible

### Errores encontrados durante redacción
- No se definió el comportamiento del multiplicador cuando el jugador pierde la piedra por viento (en Tormenta). ¿El multiplicador se resetea? → **Decisión:** Sí, siempre a 1.0x al inicio de cada lanzamiento.
- No se especificó cuántas zonas bonus pueden aparecer simultáneamente en pantalla. → **Decisión:** Máximo 3 simultáneas. Se generan al inicio del lanzamiento, desaparecen si la piedra pasa sin golpearlas (20 segundos de vida).
- El tutorial no tiene un caso de borde: ¿qué pasa si el jugador no hace tap después del hundimiento? ¿La piedra vuelve sola? → **Decisión:** Después de 3 segundos sin tap, la nueva piedra aparece automáticamente con una animación sutil de "lista".

### Riesgos post-GDD
- **Riesgo 1 (crítico):** La física de skip se sentirá "aleatoria" si los parámetros de coeficiente de rebote no son calibrados con pruebas reales. El GDD los define, pero los valores correctos solo se encuentran en el Vertical Slice.
- **Riesgo 2 (alto):** El sistema de audio emergente (notas musicales) puede sonar caótico si los skips son muy rápidos. Necesita testing de audio específico.
- **Riesgo 3 (medio):** La escalada de dificultad climática puede ser demasiado abrupta entre Viento y Marejada. El gap de amplitud de olas (0.35m → 0.70m) es significativo.

### Deuda de diseño identificada
- No se diseñó el sistema de "achievements" (logros). Será necesario para App Store y Google Play. Se propone para Fase 10 como sistema separado del sistema de desafíos diarios.
- No se definió el comportamiento online vs. offline. Si el jugador no tiene internet, ¿el leaderboard se desactiva? → **Decisión:** Sí. El gameplay es 100% offline. El leaderboard requiere conectividad y muestra mensaje de "sin conexión" si no hay.

### Decisión final
**GDD aprobado para Fase 5 — Prototipo UX/UI.**
