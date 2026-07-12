# SKIM — Game Design Document
## Parte 1: Visión, Gameplay, Loops, Controles, Física, Progresión, Economía

**Versión:** 1.0  
**Fecha:** 2026-06-30  
**Estado:** Aprobado post-Fase 3

---

## 1. VISIÓN

### 1.1 Concepto central

SKIM es un juego de habilidad táctil donde lanzas piedras planas sobre el mar para hacerlas saltar. Un flick con el dedo lanza la piedra. La física real del rebote en agua — ángulo, fuerza, spin — determina cuántos saltos consigue y hasta dónde llega. El mar nunca es idéntico: olas procedurales cambian las condiciones en cada partida. El objetivo: mayor distancia, más saltos, bonus zones alcanzadas.

**Plataforma:** Android / iOS  
**Género:** Arcade de habilidad táctil  
**Sesión objetivo:** 30 segundos a 5 minutos  
**Público primario:** 14-35 años, casual a mid-core  
**Público secundario:** Cualquier persona que haya intentado saltar una piedra en agua

### 1.2 Pilares de diseño

**Pilar 1 — "Un gesto, tres secretos"**
El flick parece simple. Esconde tres variables (ángulo, fuerza, spin). El jugador las descubre gradualmente sin que el juego las explique. La maestría se siente ganada, no enseñada.

**Pilar 2 — "El mar nunca miente"**
Las olas procedurales son la fuente de variedad y la razón por la que no existe "la tirada perfecta" replicable. Cada partida es nueva. El jugador aprende a leer el agua, no a memorizar patrones.

**Pilar 3 — "La piedra hace música"**
Cada salto produce una nota. Un combo es una melodía. El audio no es ambientación — es feedback. El jugador siente el ritmo de sus propias habilidades.

**Pilar 4 — "Reinicio sin fricción"**
La piedra se hunde. Tap. Nueva piedra lista en menos de 0.5 segundos. No hay pantalla de game over, no hay animación de transición, no hay pregunta de confirmación. El único obstáculo para intentarlo de nuevo es el deseo de hacerlo.

**Pilar 5 — "El océano recuerda"**
Los anillos de impacto de todos los lanzamientos de la sesión permanecen visibles en el agua (con fade). Al final de la sesión, el océano muestra el mapa completo de tu actividad. El progreso es visible.

### 1.3 Referencia de calidad

Tan adictivo, entretenido y disfrutable como Helix Jump fue en su momento.

**Traducción en métricas:**
- Sesión promedio Helix Jump: 4-7 minutos
- SKIM objetivo: 3-8 minutos por sesión
- Retención D1 Helix Jump: ~45%
- SKIM objetivo D1: ≥40%
- Retención D7 Helix Jump: ~18%
- SKIM objetivo D7: ≥15%

---

## 2. GAMEPLAY CORE

### 2.1 El lanzamiento (La acción fundamental)

El jugador presiona y desliza el dedo en cualquier parte de la pantalla para lanzar la piedra.

**Tres variables codificadas en el gesto:**

| Variable | Control | Efecto |
|----------|---------|--------|
| **Ángulo de lanzamiento** | Dirección del swipe respecto a la horizontal | Ángulo bajo (horizontal) = más saltos; ángulo alto (vertical) = menos saltos, más fuerza inicial |
| **Fuerza** | Velocidad del swipe | Más rápido = más distancia; muy rápido = ángulo alto inevitable (física real) |
| **Spin** | Curvatura del swipe | Recto = sin spin; curvado hacia arriba = topspin; curvado hacia abajo = backspin |

**Efecto del spin:**
- Topspin: la piedra tiende a "hundirse" en cada impacto, menos saltos pero más distancia por salto
- Backspin: la piedra "rebota hacia arriba" en cada impacto, más saltos pero menos distancia por salto
- Sin spin: comportamiento neutro y predecible

**Asistencia en primeros lanzamientos:**
Los primeros 3 lanzamientos de la primera sesión del juego aplican un modificador oculto que garantiza al menos 2 saltos, independientemente del gesto. Invisible para el jugador. Asegura que el momento de satisfacción ocurra antes de que la dificultad real aparezca.

### 2.2 Física del rebote (Modelo simplificado fiel)

El motor de física de SKIM es una simulación custom en 2D, no Unity Physics. Razón: Unity Physics no permite el nivel de control de parámetros necesario para que los rebotes se sientan justos y perceptibles.

**Modelo matemático por salto:**

```
// Ángulo óptimo de rebote: 10-20° sobre la horizontal
// Por debajo de 5°: piedra entra al agua en el primer impacto
// Por encima de 45°: piedra cae en picado, 0-1 saltos

velocidad_horizontal[n+1] = velocidad_horizontal[n] × coeficiente_rebote × (1 - desgaste_agua)
velocidad_vertical[n+1]   = velocidad_vertical[n] × coeficiente_elasticidad - efecto_ola[n]
angulo_impacto[n]         = atan2(velocidad_vertical[n], velocidad_horizontal[n])
saltos_posibles           = f(angulo_inicial, velocidad_inicial, spin, condicion_mar)
```

**Parámetros por tipo de piedra:**

| Piedra | Coeficiente rebote | Coeficiente elasticidad | Spin sensitivity |
|--------|-------------------|------------------------|-----------------|
| Guijarro | 0.72 | 0.65 | 0.3 |
| Esquisto | 0.85 | 0.55 | 0.8 |
| Basalto | 0.60 | 0.80 | 0.2 |
| Cuarzo | 0.78 | 0.70 | 1.2 |

**Efecto de las olas en el rebote:**
En cada punto de impacto, la superficie del agua tiene una pendiente local determinada por la suma de olas. Si la ola sube en el punto de impacto: la piedra rebota más alto (bonus inesperado). Si la ola baja: la piedra rebota menos (penalización). Esta variación hace que el mar se sienta vivo y real sin ser injusto — el jugador aprende a leer las olas y anticipar.

### 2.3 El océano procedural

El agua de SKIM es la suma de N funciones senoidales a diferentes frecuencias y amplitudes, calculadas en shader (GPU) y en simulación de física (CPU, misma ecuación).

**Garantía de consistencia:** El mismo conjunto de parámetros de ola se usa en el shader visual Y en el cálculo físico de rebote. Lo que el jugador ve es exactamente lo que la física usa. No hay gap entre representación visual y realidad física.

**Generación por clima:**

| Clima | Ondas | Amplitud máx | Frecuencia | Predictibilidad |
|-------|-------|-------------|-----------|----------------|
| Calma | 2 | 0.05m | Baja | Muy alta |
| Brisa | 3 | 0.15m | Media | Alta |
| Viento | 4 | 0.35m | Media-alta | Media |
| Marejada | 5 | 0.70m | Alta | Baja |
| Tormenta | 7 | 1.20m | Muy alta | Muy baja |

**Seed del mar:**
Cada sesión de juego usa un seed diferente para las olas. Esto garantiza variedad. El seed se puede guardar para reproducir un lanzamiento exacto (necesario para el sistema de ghost replay en v1.1).

### 2.4 Arco de proyección pre-lanzamiento

Mientras el dedo está en pantalla y en movimiento, se muestran 3 puntos de proyección de la trayectoria estimada de la piedra. Los puntos son semitransparentes y se actualizan en tiempo real mientras el jugador ajusta el gesto.

**Propósito:** Hacer los 3 parámetros del gesto perceptibles. El jugador puede ver cómo su ángulo y velocidad afectan la trayectoria estimada ANTES de soltar.

**Limitación intencional:** Los puntos solo muestran la trayectoria inicial, no los rebotes. El jugador ve hacia dónde va la piedra pero no cuántos saltos dará. La incertidumbre de los saltos (afectada por las olas) se mantiene como elemento de sorpresa.

**Desaparece:** Los puntos desaparecen en el momento en que el dedo se levanta y la piedra se lanza.

### 2.5 Las zonas de bonus

Círculos flotantes que aparecen en la superficie del agua. Se mueven lentamente con la corriente. Se balancean con las olas.

**Propiedades:**

| Zona | Diámetro | Bonus | Frecuencia de aparición |
|------|---------|-------|------------------------|
| Grande (verde) | 3.0m | +200 puntos | Siempre (1-2 por lanzamiento) |
| Mediana (azul) | 1.5m | +500 puntos + 0.3x multiplicador | Frecuente |
| Pequeña (dorada) | 0.6m | +1500 puntos + 0.8x multiplicador | Rara |
| Especial (arco iris) | 1.0m | ×2 multiplicador global | Muy rara (evento) |

**Activación:** La piedra debe aterrizar (hacer skip) dentro de la zona durante su vuelo. No cuenta el aterrizaje final (hundimiento).

**Retroalimentación al alcanzar zona:** Burst de partículas de agua en el color de la zona + chime musical único + el multiplicador aparece flotando (+0.3x) y sube al HUD.

### 2.6 Anillos persistentes en sesión

Cada punto de impacto de la piedra en el agua genera un anillo circular de ondas. Los anillos de la sesión completa permanecen visibles en el agua con un fade progresivo de 60 segundos.

**Propósito:** Al final de una sesión, el jugador puede ver el "mapa" completo de todos sus lanzamientos — qué tan lejos llegaron, en qué zonas impactaron. El progreso es visual, no solo numérico.

**Implementación:** Los anillos son un shader de expansión radial en la capa de agua. Sin física — solo visual. Máximo 50 anillos simultáneos antes de que los más viejos se eliminen.

---

## 3. LOOPS DE JUEGO

### 3.1 Micro-loop (Un lanzamiento — 5 a 15 segundos)

```
INICIO DEL LANZAMIENTO
  ↓
Jugador toca pantalla → aparece arco de proyección
  ↓
Jugador desliza → los 3 puntos de proyección se actualizan en tiempo real
  ↓
Jugador levanta el dedo → piedra se lanza
  ↓
Piedra vuela → [primer impacto]
  ↓
  → ¿Ángulo correcto? → SKIP + nota musical + anillo de agua
  → ¿Ángulo incorrecto? → HUNDIMIENTO + "plop" + fin del lanzamiento
  ↓
[Si skip] → acumula multiplicador → repite impacto hasta hundirse
  ↓
[Hundimiento final] → puntuación aparece 2 segundos
  ↓
TAP ANYWHERE → nueva piedra instantánea
  ↓
REINICIO (< 0.5 segundos total)
```

### 3.2 Loop de sesión (3-8 minutos)

El jugador hace múltiples lanzamientos consecutivos. No hay número fijo — la sesión dura mientras el jugador quiera.

**Estructura interna de la sesión:**
- Los primeros 3-5 lanzamientos son de "calentamiento" — el jugador recalibra su flick para las condiciones de ese día
- Los lanzamientos 5-15 son la zona de "flow" — el jugador está en ritmo, intenta batir su mejor tirada
- Los lanzamientos 15+ tienden a ser experimentación — probar stones diferentes, targets específicos

**Señal de fin de sesión (emergente, no forzada):**
El jugador ha batido su record personal → satisfacción natural → cierra el juego.
O: el jugador completó el desafío diario → cierra el juego.
O: la sesión se extiende indefinidamente → no hay límite artificial.

### 3.3 Loop de meta-progresión (Días a semanas)

```
DISTANCIA ACUMULADA TOTAL
  ↓
Desbloquea piedras nuevas con propiedades diferentes
Desbloquea climas nuevos con mayor desafío
  ↓
Desafíos diarios → Conchas (moneda)
  ↓
Conchas → Piedras cosméticas / Entornos cosméticos
  ↓
Personal bests por clima y por piedra → motivación de comparación
  ↓
Leaderboard semanal (top de score acumulado en 7 días)
```

### 3.4 Loop emocional

El loop emocional de SKIM sigue un patrón específico que crea el efecto "one more try":

```
Lanzamiento pobre → "¿Por qué?" → El jugador analiza el gesto
  ↓
Lanzamiento bueno → "¡Yo lo hice!" → Motivación sube
  ↓
Casi bate el record → "Una más, seguro esta vez"
  ↓
Bate el record → Satisfacción pico → ...pero ¿puede mejorar?
  ↓
LOOP INFINITO DE MOTIVACIÓN
```

---

## 4. CONTROLES

### 4.1 Gestos de gameplay

| Gesto | Zona | Acción | Plataforma |
|-------|------|--------|-----------|
| Swipe (cualquier dirección) | Pantalla completa | Lanzar piedra | Android + iOS |
| Tap | Pantalla completa (post-hundimiento) | Reinicio instantáneo | Android + iOS |
| Tap | HUD — selector de piedra | Cambiar piedra activa | Android + iOS |
| Tap larga | Pantalla completa | Ver estadísticas rápidas de la sesión | Android + iOS |

**Zona muerta:** Los primeros 15px del swipe no se registran para evitar lanzamientos accidentales por toques casuales.

**Velocidad mínima de swipe para lanzar:** 150px/s. Por debajo, el gesto se ignora y el arco de proyección desaparece.

**Handedness:** La UI puede mirrorearse para zurdo/diestro en configuración.

### 4.2 Navegación de menús

| Gesto | Acción |
|-------|--------|
| Tap | Seleccionar / Confirmar |
| Swipe horizontal | Cambiar tab/sección |
| Swipe vertical | Scroll en listas |
| Back (Android) | Volver / Pausa |

### 4.3 Pausa

Tap en el ícono de pausa (esquina superior derecha). Overlay semitransparente. Opciones: Reanudar / Configuración / Menú principal. No hay "abandon run" porque no hay runs — cada lanzamiento es completo en sí mismo.

---

## 5. PIEDRAS

### 5.1 Guijarro (Starter)
**Desbloqueada desde:** Inicio  
**Visual:** Piedra redonda gris, ligeramente aplastada  
**Física:** Comportamiento neutro y predecible  
**Propósito:** Aprendizaje. La piedra con la que el jugador aprende las tres variables del gesto.  
**Personaje:** "La piedra de todos. Confiable. Nunca te sorprende."

### 5.2 Esquisto (Flat skipper)
**Desbloqueada desde:** 500m acumulados  
**Visual:** Piedra plana gris-azulada, muy delgada, bordes irregulares  
**Física:** Coeficiente de rebote 18% superior. Muy sensible al spin. Requiere ángulo preciso.  
**Propósito:** Recompensa la precisión. El jugador que domina el flick obtiene muchos más saltos.  
**Personaje:** "Para quien sabe lo que hace. El experto la ama. El novato la odia."

### 5.3 Basalto (Heavy skipper)
**Desbloqueada desde:** 2000m acumulados  
**Visual:** Piedra oscura casi negra, algo más gruesa, brillante  
**Física:** Menos saltos, pero cada salto cubre más distancia. Casi insensible al spin.  
**Propósito:** Para quien prioriza distancia sobre cantidad de saltos. Perfil de scoring diferente.  
**Personaje:** "Un tanque. No baila, pero llega lejos."

### 5.4 Cuarzo (Wild card)
**Desbloqueada desde:** 5000m acumulados  
**Visual:** Piedra translúcida blanco-rosada, con reflejos de arco iris  
**Física:** Alta sensibilidad al spin. Comportamiento impredecible en climas altos. Alta varianza en todas direcciones.  
**Propósito:** Riesgo/recompensa máximo. En Calma es casi tan bueno como Esquisto. En Tormenta, puede hacer 12 saltos o 1.  
**Personaje:** "Caótica. En manos expertas, legendaria."

### 5.5 Piedras cosméticas (Skins)
Versiones visuales alternativas de las 4 piedras base. Misma física exacta. Solo cambia el aspecto. Ejemplos:
- Guijarro Lunar (blanco con motas grises)
- Esquisto Obsidiana (negro brillante con reflejos verdes)
- Basalto Coral (naranja-rosado)
- Cuarzo Aurora (azul-verde iridiscente)

Obtenibles con Conchas o en bundles cosméticos.

---

## 6. CLIMAS

### 6.1 Calma
**Desbloqueado:** Desde el inicio  
**Descripción:** Mar en calma total. La luz dorada del atardecer. Superficie casi espejada.  
**Física:** 2 ondas de amplitud mínima. Los rebotes son casi perfectamente predecibles.  
**Color de ambiente:** Azul claro, naranja dorado en horizonte  
**Propósito de diseño:** Aprendizaje puro. El jugador domina el flick sin interferencia del mar.  
**Dificultad:** ★☆☆☆☆

### 6.2 Brisa
**Desbloqueado:** 5000m acumulados totales  
**Descripción:** Viento suave. El agua tiene pequeñas olas regulares y predecibles.  
**Física:** 3 ondas, amplitud hasta 0.15m. Las olas tienen un patrón regular que el jugador puede aprender.  
**Color de ambiente:** Azul medio, tonos verdes, nubes blancas  
**Propósito de diseño:** Primera introducción de varianza. El jugador aprende que el mar afecta los rebotes.  
**Dificultad:** ★★☆☆☆

### 6.3 Viento
**Desbloqueado:** 12000m acumulados totales  
**Descripción:** Viento notable. Olas regulares pero con interferencia. El mar "respira".  
**Física:** 4 ondas, amplitud hasta 0.35m. Aparecen combinaciones de olas que crean puntos altos y bajos impredecibles.  
**Color de ambiente:** Azul grisáceo, cielo con nubes más densas, ligero spray en el horizonte  
**Propósito de diseño:** El nivel principal donde la mayoría de jugadores pasan más tiempo. Desafiante pero justo.  
**Dificultad:** ★★★☆☆

### 6.4 Marejada
**Desbloqueado:** 25000m acumulados totales  
**Descripción:** Mar embravecido. Olas grandes. Los bonus zones se balancean dramáticamente.  
**Física:** 5 ondas, amplitud hasta 0.70m. Los efectos de ola pueden amplificarse o cancelarse — creando momentos de calma relativa y picos de caos.  
**Color de ambiente:** Azul-gris oscuro, cielo cubierto, espuma blanca en crestas  
**Propósito de diseño:** Territorio de expertos. El jugador que llegue aquí siente que ha dominado algo real.  
**Dificultad:** ★★★★☆

### 6.5 Tormenta
**Desbloqueado:** 50000m acumulados totales  
**Descripción:** Tormenta total. Lluvia, relámpagos en el fondo, oleaje caótico.  
**Física:** 7 ondas con amplitudes hasta 1.20m. El mar es genuinamente caótico — incluso expertos tienen alta varianza.  
**Color de ambiente:** Gris oscuro, relámpagos ocasionales en el fondo, lluvia visible  
**Propósito de diseño:** El Tormenta de SKIM es el equivalente al "all-clear" de Tetris. Hacer 5+ saltos aquí es una historia que el jugador quiere contar.  
**Dificultad:** ★★★★★

---

## 7. SCORING

### 7.1 Fórmula base

```
score_lanzamiento = (distancia_metros × 10) × multiplicador_acumulado + suma_bonus_zones
```

### 7.2 Multiplicador acumulado

El multiplicador empieza en 1.0x y crece con cada salto exitoso:

| Salto # | Multiplicador |
|---------|--------------|
| 1 | 1.0x |
| 2 | 1.3x |
| 3 | 1.7x |
| 4 | 2.2x |
| 5 | 2.8x |
| 6 | 3.5x |
| 7+ | +0.9x por salto adicional |

El multiplicador se reinicia en 1.0x con cada nuevo lanzamiento.

### 7.3 Bonus zones

| Zona | Bonus plano | Bonus multiplicador |
|------|------------|-------------------|
| Grande (verde) | +200 | — |
| Mediana (azul) | +500 | +0.3x |
| Pequeña (dorada) | +1500 | +0.8x |
| Especial (arco iris) | +0 | ×2 (multiplica el multiplicador actual) |

### 7.4 Bonus de clima

Multiplicador adicional aplicado al score total del lanzamiento según el clima activo:

| Clima | Bonus clima |
|-------|------------|
| Calma | ×1.0 |
| Brisa | ×1.2 |
| Viento | ×1.5 |
| Marejada | ×2.0 |
| Tormenta | ×3.0 |

### 7.5 Records personales

El juego guarda por separado para cada clima y cada piedra:
- Distancia máxima en un solo lanzamiento
- Mayor número de saltos en un solo lanzamiento
- Mayor score en un solo lanzamiento
- Mayor score acumulado en una sesión

### 7.6 Leaderboard

Leaderboard semanal de score acumulado total (suma de todos los lanzamientos de la semana). Reset cada lunes a las 00:00 UTC. Top 100 visible. El jugador siempre ve su posición aunque no esté en top 100.

---

## 8. PROGRESIÓN

### 8.1 Distancia acumulada total

La métrica principal de progresión es la distancia total (en metros) acumulada a través de todos los lanzamientos de todas las sesiones. Esta cifra nunca regresa atrás — es el "nivel" del jugador.

**¿Por qué distancia y no score?**
La distancia es intuitiva y directamente observable. El jugador puede ver físicamente cuánto llegó la piedra. El score puede inflarse con bonus zones. La distancia es honesta.

### 8.2 Tabla de desbloqueos

| Hito (m acum.) | Desbloqueo |
|---------------|-----------|
| 0 | Guijarro, Clima Calma |
| 500 | Piedra Esquisto |
| 2,000 | Piedra Basalto |
| 5,000 | Clima Brisa, Piedra Cuarzo |
| 12,000 | Clima Viento |
| 25,000 | Clima Marejada |
| 50,000 | Clima Tormenta |
| 100,000 | Título "Maestro del Skimmer" (cosmético) |

### 8.3 Sistema de Conchas

Las Conchas son la moneda del juego. No tienen efecto en la física ni en el gameplay.

**Fuentes de Conchas:**
| Fuente | Conchas | Frecuencia |
|--------|---------|-----------|
| Desafío diario completado | 50-150 | Diario |
| Personal best (distancia) | 30 | Cuando ocurre |
| Personal best (saltos) | 30 | Cuando ocurre |
| Hito de distancia acumulada | 200-500 | En cada hito |
| Primer lanzamiento del día | 10 | Diario |

**Costos cosméticos:**
| Ítem | Conchas |
|------|---------|
| Skin de piedra (unidad) | 150-300 |
| Skin de entorno (unidad) | 500-800 |
| Bundle estacional (3-4 ítems) | — (IAP) |

### 8.4 Desafíos diarios

Un nuevo desafío cada día a las 00:00 UTC local. El jugador puede tener 1 activo a la vez.

**Tipos de desafío:**

| Tipo | Ejemplo | Dificultad |
|------|---------|-----------|
| Distancia | "Llega a 60m en un lanzamiento" | Variable |
| Saltos | "Haz 5 saltos en un solo lanzamiento" | Variable |
| Zona bonus | "Alcanza 3 zonas azules en una sesión" | Variable |
| Clima | "Haz 3 saltos en Marejada" | Requiere desbloqueo |
| Piedra | "Lanza el Cuarzo 5 veces" | Simple |
| Streak | "Completa 3 desafíos seguidos" | Meta |

La dificultad del desafío diario escala con la distancia acumulada del jugador — un principiante no recibe desafíos de Marejada.

---

## 9. TUTORIAL

### 9.1 Filosofía

No existe un tutorial tradicional. El jugador aprende jugando. La física asistida en los primeros 3 lanzamientos garantiza el momento de satisfacción antes de que aparezca la dificultad. El juego comunica con mínimo texto.

### 9.2 Secuencia de primera experiencia

**Lanzamiento 1 (física 100% asistida):**
- Pantalla: el océano en Calma. Una piedra en la mano (visible). Un texto mínimo: "desliza para lanzar" con una flecha.
- El jugador hace cualquier swipe.
- La piedra sale, hace 3 saltos garantizados.
- Al hundirse: anillos visibles, puntuación aparece (grande, satisfactoria).
- No hay texto adicional. El juego habla por sí solo.

**Lanzamiento 2 (física 80% asistida):**
- Sin texto. El jugador repite por motivación propia.
- Aparece el arco de proyección (si no lo notó en el primero).
- La piedra hace 2-4 saltos.
- Si alcanzó una zona bonus: el burst de partículas y el sonido explican solos lo que es una zona bonus.

**Lanzamiento 3 (física 50% asistida):**
- Sin texto. El jugador experimenta con el gesto.
- Se desactiva la asistencia. El juego ya no explica nada.

**Lanzamiento 4+ (física 0% asistida):**
- El jugador puede fallar. El primer fallo (piedra que se hunde en el primer impacto) es seguido de un tap instantáneo y nuevo intento. El fail no tiene drama — es parte del aprendizaje.

### 9.3 Señales de Esquisto (primera piedra nueva)
Cuando el jugador desbloquea el Esquisto a 500m acumulados:
- Una notificación breve aparece en el HUD: "Nueva piedra desbloqueada"
- El jugador puede equiparla desde el selector.
- Sin tutorial de cómo usarla. El jugador descubre que se comporta diferente al lanzarla.
- La primera vez que hace más saltos con Esquisto que con Guijarro, lo entiende solo.

### 9.4 Señales de clima (primer clima nuevo)
Cuando el jugador desbloquea Brisa a 5000m:
- La pantalla del selector de clima muestra "BRISA — Desbloqueada"
- El jugador puede elegirla.
- Sin explicación de cómo afecta la física. El primer lanzamiento en Brisa lo enseña.

---

## 10. BALANCE

### 10.1 Curva de dificultad

La dificultad de SKIM tiene dos ejes independientes:

**Eje 1: Habilidad del jugador con el flick**
Curva de aprendizaje de 1-3 sesiones para entender el gesto básico. Curva de maestría de semanas para dominar el spin y el ángulo óptimo por clima.

**Eje 2: Dificultad del clima**
Lineal y bloqueada por distancia acumulada. El jugador nunca enfrenta un clima que sus métricas indican que no puede manejar aún.

**El balance crítico:**
El nivel Calma nunca debe volverse aburrido. Esto se logra porque la maestría del flick (eje 1) sigue progresando incluso en el clima más fácil — siempre hay más distancia que alcanzar, mejor spin que aprender.

### 10.2 Ventana de satisfacción por nivel de habilidad

| Nivel de habilidad | Saltos típicos en Calma | Máx distancia en Calma | Clima recomendado |
|-------------------|------------------------|----------------------|-------------------|
| Principiante | 1-3 | 15-30m | Calma |
| Intermedio | 3-6 | 30-55m | Brisa |
| Avanzado | 5-8 | 50-80m | Viento |
| Experto | 7-12 | 70-120m | Marejada |
| Maestro | 10-15+ | 100m+ | Tormenta |

### 10.3 Números de referencia para playtesting

**Umbrales de juicio:**
- Si >70% de lanzamientos principiantes terminan en 0 saltos: la física asistida es insuficiente → ampliar a 5 lanzamientos
- Si el salto promedio en Calma del jugador no crece entre sesión 1 y sesión 5: el sistema de aprendizaje no está funcionando → revisar señales de feedback
- Si la sesión promedio supera 15 minutos: agregar señal de "logro de sesión" para darle al jugador un momento de cierre natural
- Si el D1 cae de 35%: revisar el primer lanzamiento — la primera impresión es el cuello de botella

---

## 11. MONETIZACIÓN

### 11.1 Principios

- Nunca Pay to Win. La física es idéntica para todos los jugadores.
- Nunca bloquear progreso detrás de pago.
- Los ads son opcionales y recompensados.
- La monetización no debe ser visible antes del minuto 5 de primera sesión.

### 11.2 Fuentes de ingresos

**Anuncios recompensados (voluntarios):**
- "Ver un anuncio → tu próximo lanzamiento tiene ×2 al score"
- Disponible máximo 3 veces por sesión
- El jugador lo activa voluntariamente — nunca aparece sin acción del jugador
- No interrumpe el flujo de juego

**Remove Ads (IAP única):**
- Elimina los anuncios recompensados de la interfaz (aunque el jugador puede verlos voluntariamente si quiere el bonus)
- Precio: $2.99 USD
- Valor percibido: limpieza de UI + apoyo al developer

**Cosméticos individuales:**
- Skins de piedras: 150-300 Conchas (ganables jugando en 3-5 días)
- Skins de entorno: 500-800 Conchas (ganables en 1-2 semanas)
- Los mejores cosméticos son ganables sin pago

**Bundles estacionales (IAP):**
- Paquetes de 3-4 cosméticos temáticos (verano, invierno, japonés, etc.)
- Precio: $1.99-4.99 USD
- Exclusivos temporales — no disponibles con Conchas

### 11.3 Proyección de ingresos (estimación conservadora)

Asumiendo 100,000 descargas en primeros 3 meses:
- Conversión IAP media industria hyper-casual: 1-3%
- Remove Ads a $2.99: 1% → ~$2,990
- Bundles estacionales: 0.5% promedio → ~$1,000-2,500 por evento
- Revenue ads (eCPM $5-15 en Latam/ES): depende de sesiones activas
- Total conservador 3 meses: $8,000-25,000

*(Estas cifras son para calibración interna — no objetivos de negocio)*
