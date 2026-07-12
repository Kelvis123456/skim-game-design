# FASE 6 — DIRECCIÓN DE ARTE
## Proyecto: SKIM

**Fecha:** 2026-06-30
**Responsable:** Estudio

---

## 1. Identidad Visual — Los 4 Pilares

SKIM tiene una estética que nace de la tensión entre **simplicidad y profundidad**. El juego se ve simple porque el océano real es simple. Pero la física debajo es compleja, igual que el océano real.

### Pilar 1 — GEOMÉTRICO
No hay texturas fotorealistas. Todo es geometría. El agua es un plano triangulado. Las piedras son primitivas con materiales sólidos. El jugador nunca confunde "decoración" con "información".

### Pilar 2 — VIVO
A pesar de ser geométrico, el mundo respira. Las olas se mueven. Los anillos de agua se expanden. La cámara hace micro-ajustes. Hay vida sin realismo.

### Pilar 3 — NOCTURNO
El juego vive en el azul oscuro nocturno. No es negro — es #0A1628. La luz viene desde el agua (teal reflejo) y desde arriba (cielo degradado). La oscuridad es ambiente, no amenaza.

### Pilar 4 — MUSICAL
Los colores y las partículas responden al audio. Un combo de 5 saltos tiene una respuesta visual diferente a un combo de 2. El arte no es decoración: es retroalimentación.

---

## 2. Sistema de Color

### Paleta Base (tokens constantes en todos los climas)

| Token | Hex | Nombre | Uso |
|-------|-----|--------|-----|
| `bg` | `#0A1628` | Abismo | Fondo de pantalla, cielo profundo |
| `card` | `#0F2035` | Profundidad | Tarjetas, superficies de UI |
| `border` | `#1E3A5F` | Horizonte | Separadores, bordes inactivos |
| `teal` | `#00C4CC` | Bioluminiscencia | Acción, progreso, anillos de impacto activos |
| `gold` | `#F4D03F` | Ámbar | Recompensas, Conchas, nuevos récords |
| `muted` | `#8DB4D4` | Niebla | Texto secundario, elementos inactivos |
| `white` | `#FFFFFF` | Espuma | Texto primario, piedra en vuelo |

### Paleta por Clima

Cada clima modifica el **color del agua**, **el cielo**, y la **intensidad emocional**. La UI (tarjetas, texto) nunca cambia — solo el mundo 3D.

#### CALMA — Azul puro, sereno
```
Agua superficial:    #1A4A6A  →  #0A2A45  (degradado de frente a profundidad)
Agua reflejada:      #00C4CC (teal bioluminiscente, baja intensidad)
Cielo alto:          #071020
Cielo horizonte:     #0D2A45
Brillo solar:        ninguno (es de noche, solo luna)
Luna:                círculo blanco #E8F4FF, 15% opacidad, sin bloom
Amplitud de olas:    0.05m — geometría casi plana
Velocidad de ola:    0.8 unidades/s
```

#### BRISA — Cyan, levemente energético
```
Agua superficial:    #1A5A6A  →  #0A2A45
Agua reflejada:      #10D4E4 (más cyan que teal)
Cielo horizonte:     #1A3A55 (muy levemente más cálido)
Amplitud de olas:    0.20m — geometría suavemente ondulada
Velocidad de ola:    1.2 unidades/s
Añadir:              micro-espuma en cresta de olas (#FFFFFF, 30% opacity)
```

#### VIENTO — Azul-gris, tensión creciente
```
Agua superficial:    #1A4060  →  #0A1A30
Agua reflejada:      #6AB4C4 (teal más apagado, grisáceo)
Cielo horizonte:     #1E2A3A (gris azulado entra)
Nubes:               formas geométricas #0A1828, opacity 60%, se añaden por primera vez
Amplitud de olas:    0.50m — geometría claramente ondulada
Velocidad de ola:    1.8 unidades/s
Espuma:              más pronunciada (#FFFFFF, 50% opacity en crestas)
```

#### MAREJADA — Azul oscuro, tormenta inminente
```
Agua superficial:    #0A2A40  →  #050F1A
Agua reflejada:      #3A8A9A (teal muy oscuro, casi apagado)
Cielo horizonte:     #0A141E (casi negro)
Nubes:               más densas, algunos hexágonos grandes #050A10, 80% opacity
Relámpagos lejanos:  flash #7070FF, 10% del tiempo, detrás de nubes
Amplitud de olas:    0.80m — geometría muy pronunciada
Velocidad de ola:    2.5 unidades/s
Espuma:              abundante (#FFFFFF, 70% opacity)
```

#### TORMENTA — Casi monocromático, épico
```
Agua superficial:    #050A14  →  #020508
Agua reflejada:      #1A4A5A (teal casi extinto, solo trazas)
Cielo horizonte:     #04070A (negro con traza azul)
Nubes:               cubren 90% del cielo, formas geométricas grandes
Relámpagos:          flash #00AAFF, frecuente (30% del tiempo), bloom fuerte
Post-relámpago:      el agua refleja el azul del rayo por 0.3 segundos
Amplitud de olas:    1.20m — geometría extrema
Velocidad de ola:    3.5 unidades/s
Lluvia:              partículas delgadas #AACCEE, caen en diagonal
```

---

## 3. El Océano — Geometría y Shader

### Malla base
- **Tipo:** Plano triangulado en Unity (Mesh generado por código)
- **Resolución:** 80 × 40 vértices (suficiente para low-poly visible pero performante en mobile)
- **Actualización:** Los vértices se desplazan en Y cada frame usando la ecuación de olas (misma que la física, como se decidió en GDD)
- **Back-face culling:** Activado — el jugador solo ve la superficie

### Ecuación de agua (debe ser IDÉNTICA en shader y en PhysicsEngine)
```
y(x, t) = Σ(i=1 to N) Aᵢ × sin(kᵢ × x + ωᵢ × t + φᵢ)

Donde:
  Aᵢ = amplitud de la ola i
  kᵢ = número de onda (2π / longitud)
  ωᵢ = frecuencia angular
  φᵢ = fase inicial (aleatoria por semilla de sesión)
  N  = número de armónicos (2 en Calma, 7 en Tormenta)
```

Esta ecuación corre en el **CPU** para física y en el **vertex shader** para visual. Si divergen, la piedra "atraviesa" el agua visualmente — un bug que destruye la credibilidad.

### Coloreado del agua
- Usar **vertex color** en lugar de texturas
- Color de vértice = interpolación entre color superficial y color profundo basado en la altura Y del vértice
- Vértice en cresta de ola → más claro (reflejado)
- Vértice en valle de ola → más oscuro (profundo)
- El shader aplica un rim light sutil (#00C4CC, 15% intensity) en las crestas para simular bioluminiscencia

### Espuma
- No es textura — son polígonos delgados (quad meshes) posicionados en las crestas
- Solo visibles en Brisa y más arriba
- Color: #FFFFFF, gradiente de opacidad horizontal (0% → 70% → 0%)
- Se generan y destruyen con el sistema de partículas, no son geometría persistente

---

## 4. Las 4 Piedras — Diseño Visual

Cada piedra es un mesh 3D simple. Nada de texturas fotorealistas. Material = Unlit (sin sombras proyectadas — no corresponde a la estética).

### GUIJARRO — La clásica
```
Forma:          Esfera achatada, eje Y comprimido al 60%
Polígonos:      ~180 tris (suficiente para silueta suave)
Color base:     #B8C5D0 (gris frío, como granito de río)
Color highlight:#D8E5F0 (reflejo en la parte superior)
Color shadow:   #6A7A88 (sombra en la parte inferior)
Tamaño:         0.18m radio
En vuelo:       ligera rotación sobre el eje horizontal (efecto gyroscópico)
En impacto:     deforma 20% en Y por 0.1s (squish) antes de rebotar
```

### ESQUISTO — La técnica
```
Forma:          Poliedro irregular aplanado — hexágono deformado con grosor 30% del radio
Polígonos:      ~120 tris (las caras planas requieren menos polígonos)
Color base:     #4A6A5A (verde-gris oscuro, como pizarra natural)
Líneas de clivaje: rayas sutiles #3A5A4A en las caras laterales (marcas de estratificación)
Color highlight:#6A8A7A
Tamaño:         0.22m de lado mayor, 0.06m grosor
En vuelo:       rotación más rápida (el disco gira — es lo que le da los saltos extras)
En impacto:     angle de entrada más crítico — el squish es más pronunciado en el eje correcto
```

### BASALTO — La fuerza bruta
```
Forma:          Esfera más perfecta que el guijarro, levemente hexagonal (como basalto real)
Polígonos:      ~200 tris
Color base:     #1E2830 (negro azulado, muy oscuro)
Color highlight:#2E3840 (casi negro — solo visible con luz directa)
Brillo:         levemente más glossy que los otros — el basalto real es más denso
Tamaño:         0.20m radio (parece más pesado por el color oscuro)
En vuelo:       rotación mínima (la piedra pesada gira lenta)
En impacto:     penetra más el agua (splash MÁS GRANDE, anillo de ola MÁS PRONUNCIADO)
```

### CUARZO — La impredecible
```
Forma:          Cristal romboidal irregular — 6 caras pero con ángulos aleatorios por semilla
Polígonos:      ~160 tris
Color base:     #C0D8F0 (blanco-azul translúcido — simula cristal)
Color highlight:#E8F4FF (casi blanco en las aristas)
Transparencia:  Shader semi-transparent (25% — se ve el agua detrás ligeramente)
Brillo:         Alto glossy — las aristas brillan con el "bioluminiscente"
Tamaño:         0.16m (más pequeña — la varianza viene de la forma, no del tamaño)
En vuelo:       rotación CAÓTICA — los ángulos cambian cada salto (realista para cristal)
En impacto:     probabilidad de ángulo de entrada aleatoria — puede saltar 1x o 8x
```

---

## 5. VFX — Efectos Visuales

### Anillo de Agua (por impacto)
```
Tipo:           Mesh procedural expandible (no partícula — necesita seguir el plano del agua)
Forma:          Elipse que sigue la curvatura local del océano
Vida útil:      60 segundos (decisión de Fase 3 — son persistentes en sesión)
Expansión:      0.1m → 2.5m de radio en los primeros 2s, luego estático
Grosor de línea:2px → 0.5px (se adelgaza mientras se expande)
Color:          #00C4CC → #00C4CC00 (teal → transparente según tiempo de vida)
Cantidad:       1 anillo por impacto, sin límite en sesión (todos persisten)
En pantalla al final de sesión: el océano muestra TODOS los anillos de la sesión
```

### Splash de Impacto
```
Tipo:           Sistema de partículas Unity
Cantidad:       6-12 gotitas por impacto (escala con fuerza de impacto)
Forma:          Esfera pequeña (0.02m radius), elongada en dirección de movimiento
Color:          #AADDEE (agua) con #FFFFFF (espuma) para las más altas
Velocidad:      0.5-3.0 m/s hacia arriba + spread lateral
Gravedad:       9.8 m/s² (física real — se sienten pesadas y reales)
Vida:           0.4-0.8 segundos
Audio trigger:  el splash visual y el audio deben ocurrir en el mismo frame
```

### Glow de Combo (en la piedra durante el vuelo)
```
Activación:     A partir del 3er salto consecutivo
Nivel 1 (×1-2): Sin glow
Nivel 2 (×3-4): Point light sutil, #00C4CC, radio 0.3m, intensidad 0.3
Nivel 3 (×5-6): Intensidad 0.6, radio 0.5m, partículas de "trail" de agua detrás
Nivel 4 (×7+):  Intensidad 1.0, radio 0.8m, trail más denso, anillos de agua brillan más
MAX (×5+):      Las notas musicales de los anillos hacen un chord — el agua "brilla" brevemente
```

### Score Pop-up (por cada salto)
```
Tipo:           Texto flotante en world-space (no en UI)
Contenido:      Puntuación del salto (ej: "+120")
Fuente:         Funnel Sans Bold
Tamaño:         Proporcional a la puntuación (rango: 24px-48px)
Color:          #F4D03F (oro) para el multiplicador activo, #FFFFFF para normal
Animación:      Aparece en el punto de impacto, flota 0.8m hacia arriba en 1.2s, ease-out
                Fade-out en los últimos 0.4s
Posición:       En el punto de impacto, sobre la superficie del agua
```

### Efecto de Nuevo Récord
```
Trigger:        Cuando la piedra supera la línea de máxima distancia de la sesión
Visual:         La línea dorada (#F4D03F) pulsa 3 veces, luego se actualiza a nueva posición
Partículas:     8 estrellas doradas emergen desde la nueva posición de la línea
Audio:          Chord mayor en la escala pentatónica
Duración:       1.5 segundos total
```

### Efecto de Hundimiento Final
```
Trigger:        Cuando la piedra pierde velocidad y se hunde
Visual:         La piedra desciende Y, hace un último anillo más pequeño
                Las partículas de agua caen verticalmente en el punto de hundimiento
Cámara:         Pull-out suave (zoom out 15% en 0.8s) para mostrar el recorrido total
Anillos:        Todos los anillos de la sesión permanecen visibles durante el pull-out
```

### Efectos Climáticos Atmosféricos
```
CALMA:    Parpadeo muy suave de la luna en el horizonte (2% scale variation cada 4s)
BRISA:    Micro-partículas de brisa (#AADDEE, opacity 10%) moviéndose en diagonal
VIENTO:   Las partículas de brisa son más densas y rápidas, nubes geométricas se mueven
MAREJADA: Spray ocasional de la cresta de las olas más altas — partículas de espuma
TORMENTA: Lluvia (partículas delgadas #AACCEE), relámpagos (flash de pantalla #7070FF, 10% opacity, 0.1s)
```

---

## 6. Sistema de Animación

### Timings Base (en segundos)
```
Micro-interacción UI:    0.15s ease-out (tap, toggle, select)
Transición de pantalla:  0.25s ease-in-out (slide desde abajo)
Score pop-up:            1.2s ease-out
Pull-out de cámara:      0.8s ease-out
Expansión de anillo:     2.0s ease-out
Combo glow aparece:      0.3s ease-in
Splash de impacto:       0.4-0.8s (variado, más realista)
Nuevo récord:            1.5s (espectacular pero no bloqueante)
```

### Cámara
```
Posición fija:    La cámara NO sigue la piedra en vuelo
Micro-tracking:   Cuando la piedra está en su punto más alto, la cámara hace un
                  adjust sutil de 5% en Y para mostrar más horizonte (ease-in-out)
Pull-out:         Solo al hundirse — el único movimiento de cámara notable
FOV:              60° fijo — no varía con la velocidad de la piedra
```

### La Piedra en Vuelo
```
Rotación eje X:   Velocidad proporcional a la velocidad horizontal de la piedra
Rotación eje Z:   Proporcional al spin aplicado en el gesto
Squish on impact: 20% compresión en Y durante 0.1s en cada rebote
Stretch pre-impact:5% elongación en la dirección de movimiento durante el descenso
```

---

## 7. UI — Lenguaje Visual Formalizado

### Tarjetas (Cards)
```
Background:     #0F2035
Border:         #1E3A5F, 1px
Corner radius:  16px (elementos de lista), 20px (cards principales), 24px (modales)
Shadow:         0px 8px 24px #00000033 (solo en cards que flotan sobre el océano)
Glassmorphism:  NO — se reserva para overlays de pausa/resultado
```

### Botones
```
CTA Principal:  Background #00C4CC, height 70px, corner-radius 35px
                Glow: box-shadow 0 8px 24px #00C4CC44
                Texto: Funnel Sans 18px Bold, #FFFFFF
                Icon: lucide, 20px, #FFFFFF

Secundario:     Background transparente, border #1E3A5F 1px
                Texto: Inter 14px Regular, #8DB4D4

Destructivo:    Texto #E74C3C (solo para "Eliminar datos" — no hay botón destructivo con bg)
```

### Estados de elementos de lista
```
ACTIVO/EQUIPADO:  fill #0F2035, border #00C4CC 1.5px, badge teal
DISPONIBLE:       fill #0F2035, border #1E3A5F 1px, checkmark teal
BLOQUEADO:        fill #091422, border #1E3A5F 1px, opacity 60%, icon lock #8DB4D4
```

### Iconografía
```
Librería:   Lucide Icons (línea, no filled)
Tamaños:    16px (inline texto), 18px (filas de lista), 22px (navegación), 24px (HUD)
Color:      #FFFFFF (primario), #8DB4D4 (secundario/inactivo), #00C4CC (activo/acción)
```

---

## 8. Sincronía Audio-Visual

El sistema de audio-visual es una de las características más importantes de SKIM. El diseño visual DEBE colaborar con el sistema de audio definido en el GDD.

### Mapa Nota → Color
```
Nota C (Do):   Anillo teal base          #00C4CC
Nota D (Re):   Anillo teal más brillante #10D4DC
Nota E (Mi):   Anillo cyan claro         #20E4EC
Nota G (Sol):  Anillo azul-teal          #0094CC
Nota A (La):   Anillo teal dorado        #00C4AA
```
*Diferencias sutiles — el jugador las percibe acumuladas, no individualmente.*

### Chord Resolution (combo ≥5)
```
Cuando se cierra la melodía espontánea (5 notas en C mayor pentatónico):
- TODOS los anillos visibles de la sesión hacen un pulse simultáneo (#00C4CC → #FFFFFF → #00C4CC, 0.3s)
- La piedra hace un trail de luz blanca durante 0.5s
- Score pop muestra "+CHORD" en dorado
```

### Silencio entre lanzamientos
```
Entre lanzamientos: el océano hace micro-ripples ambientes (#00C4CC, 2% opacity, aleatorios)
Estos no corresponden a ninguna nota — son ruido de fondo visual
Si el jugador tiene headphones, este silencio visual + el sonido del océano crea el momento de calma
```

---

## 9. Lo que NO es SKIM — Anti-Estética

Esta lista existe para mantener la coherencia cuando se generen nuevos assets.

| ❌ Evitar | ✅ En cambio |
|-----------|-------------|
| Texturas fotorealistas de agua | Geometría triangulada con vertex color |
| Luz de día brillante (sol amarillo) | Noche, luna suave, bioluminiscencia teal |
| Colores vibrantes/neón saturados (excepto teal) | Paleta oscura con acentos controlados |
| Gradients de más de 2 colores en UI | Fondos sólidos o degradados simples |
| Efectos de bloom excesivo | Bloom solo en relámpagos (Tormenta) y récords |
| Sombras proyectadas en objetos 3D | Materiales Unlit, información por vertex color |
| Partículas caóticas/múltiples sistemas simultáneos | Partículas específicas, controladas, con propósito |
| UI que cubre el océano | HUD mínimo en los bordes, océano siempre visible |
| Animaciones > 2s en UI | Todo debajo de 0.3s en UI, excepto efectos dramáticos de juego |
| Texto en más de 2 fuentes | Solo Funnel Sans + Inter |
| Gradients en el agua durante el gameplay | Solo el degradado de profundidad (no decorativo) |

---

## 10. Assets a Producir — Lista Completa

### 3D (Unity)
- [ ] Mesh: Guijarro (LOD0 + LOD1)
- [ ] Mesh: Esquisto (LOD0 + LOD1)
- [ ] Mesh: Basalto (LOD0 + LOD1)
- [ ] Mesh: Cuarzo (semi-transparent shader)
- [ ] Mesh: Plano de océano procedural (generado en runtime)
- [ ] Mesh: Quad de espuma
- [ ] Mesh: Nube geométrica (3-4 variantes)

### Shaders (Unity ShaderGraph / URP)
- [ ] WaterSurface.shader (vertex displacement + vertex color + rim light)
- [ ] Stone_Opaque.shader (Unlit + vertex highlight/shadow)
- [ ] Stone_Transparent.shader (Cuarzo — semi-transparent unlit)
- [ ] RainDrop.shader (partícula delgada con alpha)

### Partículas (Unity VFX Graph)
- [ ] WaterSplash.vfx (impacto de piedra)
- [ ] WaterRing.vfx (anillo expandible persistente)
- [ ] ComboTrail.vfx (trail de la piedra en combo alto)
- [ ] RecordStar.vfx (estrellas doradas de nuevo récord)
- [ ] Rain.vfx (lluvia de Tormenta)
- [ ] WindParticle.vfx (brisa de Viento/Marejada)

### UI (Importar desde Pencil / implementar)
- [ ] Implementar todas las 7 pantallas del prototipo Fase 5
- [ ] Animaciones de transición entre pantallas
- [ ] Score pop-up (world-space canvas)
- [ ] Indicador de multiplicador (canvas overlay)

---

## 11. Autocrítica — Fase 6

### Fortalezas

**1. La paleta por clima es dramática pero coherente**
Los 5 climas tienen identidad visual propia pero usan el mismo sistema de color. Un jugador que llega a Marejada reconoce que es el mismo juego — solo más oscuro y más tenso.

**2. La ecuación de agua unificada evita el bug más probable**
Especificar explícitamente que el shader y la física usan la MISMA ecuación matemática elimina el bug donde la piedra "atraviesa" visualmente el agua. Este es el bug más frecuente en este tipo de juegos.

**3. El sistema de anillos persistentes es visualmente único**
Ningún juego del género muestra el "mapa" de la sesión completa en el océano. Al final de una sesión de 20 lanzamientos, el océano está lleno de anillos concéntricos. Esto no está especificado en ningún otro juego que yo conozca. Es SKIM.

### Debilidades encontradas

**1. El Cuarzo semi-transparente puede ser costoso en mobile**
Los shaders de transparencia en mobile son notoriamente caros (overdraw). Si el Cuarzo genera drops de FPS, la solución es simular la transparencia con una textura de environment map falsa (fake reflection). Documentado como riesgo técnico.

**2. Las nubes geométricas no están especificadas suficientemente**
Se menciona que son "formas geométricas" pero no se define exactamente cuántas, de qué tamaño, cómo se mueven. Esto es deuda de arte que el artista 3D deberá resolver en Fase 9.

**3. La sincronía nota→color es difícil de percibir**
La diferencia entre #00C4CC y #10D4DC es de 16 puntos en el canal G — perceptible pero sutil. Si en las pruebas de usuario nadie lo nota, se puede abandonar sin perder nada. Si lo notan, se convierte en una profundidad oculta que algunos jugadores descubrirán y amarán.

### Decisión de avance

La dirección de arte está completamente especificada para que un desarrollador Unity pueda comenzar la implementación sin necesitar más preguntas sobre "cómo se ve". Los assets específicos están listados. Los shaders están conceptualizados.

**SKIM avanza a Fase 7 — Selección Tecnológica.**
