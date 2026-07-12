# FASE 5 — PROTOTIPO UX/UI
## Proyecto: SKIM

**Fecha:** 2026-06-30
**Archivo de diseño:** `fase5-uxui/SKIM-UI.ep`
**Herramienta:** Pencil Project (MCP)

---

## Objetivo de la fase

Diseñar el prototipo visual completo de SKIM: todas las pantallas necesarias para comunicar la experiencia al equipo de desarrollo y validar la dirección visual antes de entrar en producción.

**Criterio de éxito:** Un diseñador o desarrollador que vea el prototipo debe poder entender la mecánica central, el flujo de usuario y la identidad visual sin necesidad de una explicación adicional.

---

## Pantallas diseñadas (7 total)

### 00 — Splash / Carga
**ID Pencil:** `m00IN`

Pantalla de entrada. Muestra el logotipo SKIM en Funnel Sans sobre fondo degradado azul oscuro, el tagline "Una piedra. Un flick. El océano entero." y una representación visual de la piedra sobre el agua con anillos de impacto. Dots de paginación en la parte inferior.

**Decisión clave:** La piedra está presente desde el primer frame. El jugador ya está mirando el objeto que va a controlar antes de tocar nada.

---

### 01 — Menú Principal
**ID Pencil:** `qi1hx`

Pantalla de inicio de sesión activa. Muestra:
- Estado del clima actual ("CALMA") con representación visual del mar
- Estadísticas de la sesión anterior (distancia del día, tiradas totales, récord personal)
- Desafío diario activo con progress indicator
- Botón CTA "LANZAR" en teal con glow effect
- Tab bar con 5 secciones: Inicio, Colección, Desafíos, Ranking, Config

**Decisión clave:** La primera acción disponible es siempre lanzar. No hay onboarding que bloquee. El estado del juego (clima, stats, challenge) es visible antes de entrar.

---

### 02 — Gameplay HUD
**ID Pencil:** `HJ1xs`

Pantalla activa durante el juego. Elementos:
- Score central en la parte superior (grande, siempre visible)
- PB indicator a la izquierda del score
- Botón pausa
- Área del arco de proyección (dots de trayectoria estimada)
- Oceano procedural con anillos de impacto persistentes y línea de PB
- Multiplicador activo (×3) en badge teal
- Bottom HUD: tipo de piedra | clima activo | multiplicador

**Principio de diseño:** Información mínima, máxima legibilidad. El océano ocupa 360px de los 844px totales. Todo lo demás es HUD ligero.

---

### 03 — Post-Lanzamiento
**ID Pencil:** `gyWHi`

Aparece al hundirse la piedra. Muestra los resultados en una tarjeta flotante:
- Badge "¡NUEVO RÉCORD!" (cuando aplica)
- Distancia, saltos, score
- Dot carousel para lanzamientos anteriores en sesión
- Botón "OTRA VEZ" (CTA principal) + botón home secundario
- Texto "Toca en cualquier parte para lanzar de nuevo"

**Decisión clave:** El tiempo de restart es 0 si el jugador simplemente toca la pantalla. El card se muestra pero no bloquea. Replica el instinto "una más" de Helix Jump.

---

### 04 — Selector de Piedras
**ID Pencil:** `yn5tP`

Pantalla de progresión. Lista las 4 piedras con sus propiedades:
- **Guijarro** (equipada) — La clásica · Predecible
- **Esquisto** (desbloqueada) — Más saltos · Sensible al spin
- **Basalto** (bloqueada a 2,000m)
- **Cuarzo** (bloqueada a 5,000m)

Cada fila muestra la representación visual de la piedra, nombre, descripción y stats de Saltos/Spin en color. Las piedras bloqueadas muestran el icono de candado y la distancia acumulada necesaria.

---

### 05 — Selector de Climas
**ID Pencil:** `xlf3Y`

5 niveles climáticos mostrados como lista vertical:
- **Calma** (equipado, borde teal activo) — 2 olas · 0.05m amplitud
- **Brisa** (desbloqueada) — 3 olas · 0.20m amplitud
- **Viento** (bloqueado) — Desbloquea a 5,000m
- **Marejada** (bloqueado) — Desbloquea a 12,000m
- **Tormenta** (bloqueado) — Desbloquea a 25,000m

Badge "2/5 niveles" en el header muestra el progreso de desbloques.

---

### 06 — Desafío Diario
**ID Pencil:** `FBXG9`

Pantalla dedicada al desafío activo. Incluye:
- Tarjeta de desafío principal: tipo (DISTANCIA), descripción, barra de progreso, texto "47m / 100m" con porcentaje, recompensa en Conchas
- Timer de reset del desafío
- 3 objetivos bonus con estado (completado / pendiente)

**Decisión de economía:** Las Conchas son la única recompensa monetaria. No hay timers de energía ni pay-walls. El desafío renueva el propósito de la sesión diaria sin añadir mecánicas nuevas.

---

### 07 — Configuración
**ID Pencil:** `c8UGR3`

3 secciones con controles nativos:
- **AUDIO:** Slider de Música (70%), slider de Efectos de sonido (100%), toggle Vibración (ON)
- **ACCESIBILIDAD:** Toggles de Alto contraste y Texto grande (ambos OFF)
- **JUEGO:** Selector de Idioma (Español), toggle Notificaciones, enlace Eliminar datos

---

## Sistema de diseño aplicado

| Token | Valor | Uso |
|-------|-------|-----|
| Background | `#0A1628` | Todas las pantallas |
| Card | `#0F2035` | Tarjetas, filas |
| Teal | `#00C4CC` | Acciones, activo, progreso |
| Gold | `#F4D03F` | Recompensas, Conchas |
| Muted | `#8DB4D4` | Texto secundario, labels |
| Border | `#1E3A5F` | Separadores, bordes |
| Heading | Funnel Sans 700 | Títulos, nombres |
| Body | Inter | Descripciones, labels, datos |

---

## Flujos de usuario prototipados

```
Splash (3s auto) → Menú Principal
Menú Principal → [LANZAR] → Gameplay HUD
Gameplay HUD → [Piedra se hunde] → Post-Lanzamiento
Post-Lanzamiento → [tap/OTRA VEZ] → Gameplay HUD
Post-Lanzamiento → [home icon] → Menú Principal
Menú Principal → [Tab: Colección] → Selector de Piedras
Menú Principal → [Nivel de clima] → Selector de Climas
Menú Principal → [Tab: Desafíos] → Desafío Diario
Menú Principal → [Tab: Config] → Configuración
```

---

## Autocrítica — Fase 5

### Fortalezas

**1. Identidad visual coherente**
Las 7 pantallas usan el mismo sistema de tokens sin excepciones. Un desarrollador puede implementar el design system con 6 colores y 2 fuentes.

**2. HUD mínimo pero completo**
El Gameplay HUD muestra exactamente la información necesaria: score, PB, multiplicador, tipo de piedra y clima activo. Ningún elemento es decorativo. Todos comunican estado de juego.

**3. El flujo de restart es el más corto posible**
Post-Lanzamiento → tap en cualquier parte → nuevo lanzamiento. Sin confirmaciones, sin animaciones de carga. Replica exactamente el loop adictivo de Helix Jump.

**4. La progresión es visible sin jugar**
El Selector de Piedras y el Selector de Climas muestran qué hay por delante. El jugador ve "Tormenta bloqueado a 25,000m" en la primera sesión. Eso crea un objetivo de largo plazo sin que el juego lo mencione explícitamente.

### Debilidades encontradas

**1. Falta la pantalla de tutorial**
No se diseñó la pantalla de asistencia para los primeros 3 lanzamientos (decisión de diseño #1 de Fase 3). Se asumió que el tutorial es 100% contextual (la física asistida es invisible), pero debería haber al menos un estado visual del HUD que indique "modo tutorial activo".

**2. El Ranking no tiene pantalla**
El tab "Ranking" aparece en el tab bar del Menú Principal pero no se diseñó su pantalla. Es deuda de prototipo.

**3. La Colección de Conchas no está diseñada**
El sistema de cosmética (skinning con Conchas) se menciona en el GDD pero no tiene pantalla en el prototipo.

**4. El arco de proyección es un placeholder**
En el Gameplay HUD, el arco de proyección se representa con dos puntos estáticos. La animación dinámica del arco (que responde al dedo en movimiento) no puede representarse en un prototipo estático — pero debería documentarse como requisito de interacción para Fase 9.

### Riesgo identificado

El prototipo asume que el jugador entiende el gesto de flick intuitivamente. La pantalla de Post-Lanzamiento muestra resultados pero no enseña a mejorar. Si el jugador no descubre que el gesto tiene 3 variables (ángulo, fuerza, spin), puede abandonar creyendo que el juego es aleatorio.

**Mitigación propuesta para Fase 9:** Añadir micro-feedback en el HUD post-lanzamiento que muestre qué variable fue decisiva ("SPIN ALTO — intenta reducirlo").

---

## Decisión de avance

El prototipo cubre el 85% del flujo principal. Las pantallas faltantes (Tutorial, Ranking, Colección) son secundarias y pueden prototipares en iteraciones futuras. La experiencia central — lanzar, ver resultados, volver a lanzar — está completamente diseñada.

**SKIM avanza a Fase 6 — Dirección de Arte.**
