# FASE 11 — QA Y LANZAMIENTO
## Proyecto: SKIM

**Fecha:** 2026-06-30
**Entrada:** Build completo de Fase 10 — todas las features implementadas, builds de release para Android e iOS.
**Salida:** SKIM publicado en Google Play Store y App Store.

---

## Estructura de la fase

La Fase 11 tiene tres etapas secuenciales. No se pasa a la siguiente sin completar la anterior.

```
Etapa A — QA Interno (2 semanas)
  └─ El equipo testea el juego sistemáticamente con casos de prueba documentados

Etapa B — Beta Abierta (2 semanas)
  └─ Usuarios externos testean en dispositivos reales — Google Play Internal Testing + TestFlight

Etapa C — Lanzamiento (1 semana)
  └─ Store listings, submit, live monitoring el día del lanzamiento
```

---

## Etapa A — QA Interno

### A.1 — Dispositivos de prueba obligatorios

Todo bug crítico debe reproducirse en al menos uno de los dispositivos piso antes de bloquearse:

| Dispositivo | OS | GPU | Rol |
|-------------|-----|-----|-----|
| Pixel 7 | Android 13 | Mali-G710 | Dispositivo objetivo Android |
| Galaxy A52 | Android 11 | Adreno 618 | **Dispositivo piso** — el más lento que soportamos |
| iPhone 14 | iOS 16 | A15 Bionic | Dispositivo objetivo iOS |
| iPhone XR | iOS 15 | A12 Bionic | **Dispositivo piso** iOS |

Si no se dispone de todos los dispositivos físicos, usar BrowserStack Device Testing (plan de pago) para Galaxy A52 y iPhone XR.

---

### A.2 — Casos de prueba — Física (P0)

Los bugs P0 bloquean el lanzamiento. No se puede lanzar con ningún P0 abierto.

```
F-01  La piedra no atraviesa el agua en ningún clima
      Pasos: Lanzar con fuerza mínima (Force ~0.1) en Tormenta
      Esperado: la piedra se hunde inmediatamente sin atravesar el mesh
      Riesgo: tunneling si el timestep de 120Hz no es suficiente con olas grandes

F-02  La física es determinista bajo el mismo input
      Pasos: Lanzar con ángulo=45°, fuerza=0.8, spin=0 exactamente dos veces
             en el mismo frame (test de código, no manual)
      Esperado: exactamente la misma trayectoria

F-03  La ecuación del océano es idéntica en física y shader
      Pasos: Pausar el juego en mid-flight, comparar posición Y de la piedra
             con la posición Y del vértice del mesh en la misma X
      Esperado: diferencia < 0.001m
      Nota: este test es el más crítico de todo el proyecto

F-04  El spin del Cuarzo no produce trayectorias infinitas
      Pasos: Lanzar Cuarzo con spin=1.0 (máximo) en Calma con fuerza=1.0
      Esperado: la piedra se hunde en < 30 segundos

F-05  El multiplicador no supera el cap de 15.0x
      Pasos: Conseguir 20+ saltos consecutivos (puede requerir God Mode en testing)
      Esperado: multiplicador se queda en 15.0x, no sube más

F-06  La piedra no rebota una vez hundida
      Pasos: Dejar que la piedra se hunda — esperar 5 segundos
      Esperado: phase = Sunk, no hay más eventos OnImpact
```

---

### A.3 — Casos de prueba — Progresión y Save (P0)

```
S-01  Los datos no se pierden al cerrar la app
      Pasos: Jugar 10 lanzamientos → cerrar la app (no home, cerrar) → reabrir
      Esperado: TotalAccumulatedDistance y AllTimeRecord son los mismos

S-02  El desafío diario se resetea correctamente
      Pasos: Cambiar la fecha del dispositivo al día siguiente
      Esperado: nuevo desafío diferente, progress = 0

S-03  El desafío diario completado no se puede completar dos veces el mismo día
      Pasos: Completar el desafío → seguir jugando
      Esperado: el reward se otorga exactamente una vez

S-04  Las piedras bloqueadas no son equipables
      Pasos: Intentar equipar Basalto sin haber acumulado 4,000m (desde save limpio)
      Esperado: la fila de Basalto no responde al tap, muestra candado

S-05  El balance de Conchas no puede ser negativo
      Pasos: Tener 10 Conchas → intentar comprar skin de 150 Conchas
      Esperado: SpendConchas devuelve false, balance sigue en 10

S-06  El récord histórico solo sube, nunca baja
      Pasos: Conseguir récord de 500m → jugar y quedarse en 100m
      Esperado: AllTimeRecord sigue siendo 500m

S-07  "Eliminar datos" borra todo
      Pasos: Ir a Configuración → Eliminar datos → confirmar → reabrir
      Esperado: todo en cero, como primera vez, tutorial aparece de nuevo
```

---

### A.4 — Casos de prueba — UI y Flujos (P1)

Los bugs P1 deben resolverse antes del lanzamiento pero no bloquean la beta.

```
U-01  El tab bar navega correctamente entre las 5 secciones
      Pasos: Tocar cada tab 3 veces en orden y en orden inverso
      Esperado: sin freezes, sin pantallas en negro entre navegaciones

U-02  El botón "OTRA VEZ" responde en < 200ms
      Pasos: Medir con cronómetro entre tap y aparición del arco de proyección
      Esperado: < 200ms (si tarda más, el jugador siente lag en el restart)

U-03  El overlay del tutorial aparece solo en la primera sesión
      Pasos: Primera vez abrir el juego → aparece el tutorial
             Cerrar y reabrir → NO aparece
      Esperado: exactamente 1 aparición en toda la vida del save

U-04  El arco de proyección se actualiza durante el drag
      Pasos: Arrastrar muy lentamente de izquierda a derecha
      Esperado: los 3 puntos del arco se mueven suavemente

U-05  La transición entre climas no produce glitches visuales
      Pasos: Cambiar de Calma a Tormenta durante el resultado post-lanzamiento
      Esperado: 2s de transición suave, sin pop ni flash de colores

U-06  El slider de audio en Configuración afecta el volumen en tiempo real
      Pasos: Abrir Config → ajustar slider de Música a 0 → volver → lanzar
      Esperado: las notas musicales no se escuchan (volumen = 0)

U-07  El idioma se aplica en todas las pantallas
      Pasos: Cambiar idioma a English → revisar todas las 7 pantallas
      Esperado: no hay strings en español mezcladas con inglés

U-08  Las notificaciones se solicitan correctamente
      Pasos: Activar toggle de Notificaciones en Config (iOS)
      Esperado: aparece el diálogo nativo de iOS pidiendo permiso
```

---

### A.5 — Casos de prueba — Performance (P0)

```
P-01  30fps mínimo en dispositivo piso bajo carga máxima
      Pasos: Lanzar Cuarzo en Tormenta con 50+ anillos activos en pantalla
      Esperado: ≥ 30fps en Galaxy A52 e iPhone XR (medir con Unity Profiler)

P-02  Sin memory leaks en sesión de 30 minutos
      Pasos: Jugar 30 minutos continuos sin cerrar la app
             Monitorear RAM con Profiler
      Esperado: RAM estable (no crecimiento continuo — indica leak)
      Candidato principal: pool de VFX si no se reciclan correctamente

P-03  El audio no tiene latencia perceptible en Android
      Pasos: Lanzar en Galaxy A52, escuchar el momento exacto de impacto
      Esperado: audio del impacto llega en < 80ms
      Si falla: verificar que FMOD está activo (Plan B de Fase 9)

P-04  El build de Android es < 150MB (tamaño en Google Play)
      Pasos: Generar AAB release → revisar tamaño en Play Console
      Esperado: < 150MB (si supera, auditar audio y texturas)

P-05  Sin crashes en 60 lanzamientos consecutivos
      Pasos: Lanzar 60 veces seguidas sin tocar menús
      Esperado: cero crashes, cero exceptions no manejadas en logcat
```

---

### A.6 — Casos de prueba — Monetización (P0)

```
M-01  Los IAP completan en entorno sandbox
      Pasos: Configurar cuenta de prueba en Google Play / Apple Sandbox
             Comprar cada uno de los 4 bundles
      Esperado: ConchaBalance se incrementa correctamente en cada compra

M-02  Los IAP fallidos no dan Conchas
      Pasos: Cancelar la compra en el diálogo de pago
      Esperado: ConchaBalance no cambia, OnPurchaseFailed se emite

M-03  Los IAP no son explotables (doble compra)
      Pasos: Comprar el mismo bundle dos veces en < 5 segundos
      Esperado: solo se procesa una transacción, no hay duplicado de Conchas

M-04  La cosmética equipada persiste entre sesiones
      Pasos: Equipar "Guijarro Dorado" → cerrar app → reabrir
      Esperado: el Guijarro sigue siendo dorado al lanzar

M-05  Las skins no afectan la física
      Pasos: Registrar la distancia del Guijarro estándar en 20 lanzamientos
             Equipar "Guijarro Dorado" → registrar 20 lanzamientos más
      Esperado: la distribución estadística de distancias es equivalente
```

---

### A.7 — Criterio de salida de QA Interno

```
□ 0 bugs P0 abiertos
□ < 5 bugs P1 abiertos (y todos con workaround conocido)
□ Performance: todos los casos P-01 a P-05 pasan
□ Monetización: todos los casos M-01 a M-05 pasan en sandbox
□ Save: ningún caso de corrupción de datos encontrado en 10+ reinstalaciones
```

---

## Etapa B — Beta Abierta

### B.1 — Distribución

**Android — Google Play Internal Testing:**
- Máximo 100 testers en Internal Testing
- Distribución por email (lista en Google Play Console)
- Feedback por formulario de Google Forms (link en descripción del build)

**iOS — TestFlight:**
- Hasta 10,000 testers externos
- Distribución por link público de TestFlight
- Feedback por TestFlight built-in + formulario de Google Forms

### B.2 — Qué pedir a los beta testers

El formulario tiene exactamente 6 preguntas. Menos preguntas = más respuestas.

```
1. ¿En qué dispositivo jugaste? (campo libre)

2. ¿El juego corrió sin problemas en tu teléfono?
   □ Sí, sin problemas
   □ Sí, pero con algunos drops de FPS
   □ No, el juego se congeló o cerró solo

3. ¿Entendiste cómo funciona el gesto de lanzamiento sin que nadie te lo explicara?
   □ Sí, fue intuitivo
   □ Lo entendí después de 2-3 intentos
   □ No lo entendí del todo

4. ¿Cuántas sesiones de juego tuviste?
   □ 1 sola
   □ 2-5 sesiones
   □ Más de 5 — seguí volviendo

5. ¿Alguna vez sentiste que el juego te trató injustamente? (campo libre)

6. ¿Qué cambiarías o añadirías? (campo libre — opcional)
```

### B.3 — Bugs críticos de la beta

Un bug encontrado por ≥ 3 testers en dispositivos distintos entra automáticamente como P0 aunque no haya estado en los test cases de QA Interno.

Un dispositivo que crashea consistentemente se añade a la lista de "dispositivos problemáticos" y se evalúa si bloqueamos el juego en ese modelo específico en Play Console.

### B.4 — Criterio de salida de Beta

```
□ ≥ 50 respuestas al formulario
□ ≥ 80% reportan "sin problemas" de performance
□ ≥ 70% reportan ≥ 2 sesiones (retención mínima aceptable para lanzamiento)
□ ≥ 75% entendieron el gesto sin explicación
□ 0 bugs P0 nuevos sin resolver
□ Los bugs más frecuentes de preguntas 5 y 6 están documentados como roadmap v1.1
```

---

## Etapa C — Lanzamiento

### C.1 — Store Listings

**Google Play Store:**

```
Título: SKIM — Stone Skipping
Subtítulo (30 chars): Flick. Skip. Score.

Descripción corta (80 chars):
Lanza una piedra sobre el océano. Un flick. Infinitas posibilidades.

Descripción larga:
SKIM es el arte de hacer saltar una piedra sobre el agua — llevado al límite.

Un solo gesto: arrastra para apuntar, desliza con fuerza, tuerce para añadir spin.
Cada lanzamiento es diferente. El océano nunca es el mismo.

🌊 5 CLIMAS — desde el mar en calma hasta la tormenta total
🪨 4 PIEDRAS — cada una con física única: predecible, curvilínea, pesada, impredecible
🎵 MÚSICA EMERGENTE — cada rebote es una nota de una escala pentatónica
⭕ ANILLOS PERSISTENTES — cada punto de impacto deja su marca en el agua
📏 RÉCORD PERSONAL — supera tu mejor lanzamiento, sesión tras sesión

Sin vidas. Sin energía. Sin esperas.
Solo tú, una piedra, y el océano.

Categoría: Arcade
Clasificación de contenido: Everyone
```

**Screenshots requeridos (7 total, 1080×1920):**
```
1. Gameplay — piedra en vuelo sobre Marejada, 4 anillos visibles, score ×3.5
2. Gameplay — momento de impacto con splash y nota musical visible
3. Post-lanzamiento — card con "¡NUEVO RÉCORD!" en Tormenta
4. Selector de climas — los 5 climas, 2 desbloqueados
5. Selector de piedras — las 4 piedras, Cuarzo visible pero bloqueado
6. Desafío diario — challenge activo con barra de progreso
7. Gameplay — Calma, primer lanzamiento, arco de proyección visible
```

**App Store (iOS) — campo adicional:**
```
Keywords (100 chars):
stone,skip,ocean,arcade,flick,relax,physics,music,zen,casual,water
```

### C.2 — Política de privacidad

Necesaria para:
- Google Play (obligatoria si el juego pide permisos o usa IAP)
- App Store (obligatoria siempre)

Contenido mínimo:
```
SKIM no recopila datos personales.
Usamos GameAnalytics para estadísticas de gameplay anónimas (sesiones, distancias, tasa de retención).
No compartimos datos con terceros.
Los IAP son procesados por Google Play / Apple App Store según sus términos.
Contacto: [email del estudio]
```

Publicar en una URL permanente (puede ser una página de GitHub Pages o similar) antes de hacer el submit.

### C.3 — Checklist de submit

**Android:**
```
□ AAB firmado con keystore de producción
□ Keystore guardado en lugar seguro + backup en almacenamiento offline
□ Bundle version code incrementado (1 → release)
□ ProGuard / R8 activo (IL2CPP ya lo maneja en Unity)
□ Screenshots subidos (7 de 1080×1920)
□ Descripción en inglés Y español
□ Política de privacidad publicada y URL añadida en Play Console
□ Clasificación de contenido completada (cuestionario de IARC)
□ Precio: Free (con IAP)
□ Países de distribución: todos
□ Build subido a Internal Testing → promovido a Production
```

**iOS:**
```
□ Archive generado en Xcode con Distribution Certificate
□ Subido a App Store Connect vía Xcode Organizer
□ Screenshots en formato 6.7" (iPhone 15 Pro Max) — obligatorio
□ Screenshots adicionales en 5.5" (iPhone 8 Plus) — obligatorio
□ Descripción en inglés (idioma principal)
□ Keywords completos (100 chars)
□ Política de privacidad URL añadida
□ "Sign In with Apple" — no requerido si no hay login
□ IDFA: NO se usa (no hay publicidad en SKIM) → marcar "Does not use IDFA"
□ Age Rating: 4+ (sin contenido problemático)
□ Precio: Free (con In-App Purchases)
□ In-App Purchases creados en App Store Connect con mismos IDs que el código
□ Submit for Review
```

### C.4 — Tiempos de revisión esperados

```
Google Play:   3-7 días laborables (primera subida — las actualizaciones son más rápidas)
App Store:     1-3 días (varía — puede ser hasta 1 semana en periodos de alta demanda)
```

Planificar el submit para un martes o miércoles para que la revisión no caiga en fin de semana.

---

## C.5 — Día del Lanzamiento — Live Monitoring

Las primeras 24 horas son críticas. Bugs que no aparecieron en beta pueden aparecer a escala.

### Dashboard de monitoreo

```
GameAnalytics (free tier):
  - Sessions per day
  - Session length
  - DAU (Daily Active Users)
  - Progression events (desbloqueos de piedras y climas)
  - Business events (IAP completados)

Google Play Console:
  - ANR rate (Application Not Responding): target < 0.47%
  - Crash rate: target < 1.09%
  - Rating promedio: objetivo ≥ 4.2 en primera semana

App Store Connect:
  - Crashes (Xcode Organizer → Crashes)
  - Ratings
```

### Umbrales de acción inmediata

Si en las primeras 6 horas:
- Crash rate > 5% → detener la distribución en Play Console (botón "Halt rollout")
- 3+ reviews de 1 estrella mencionando el mismo bug → hotfix prioritario
- ANR rate > 2% → probable deadlock en el hilo principal — hotfix

### Hotfix pipeline

```
Bug crítico encontrado en producción → plazo de hotfix:
  - Crash que afecta > 10% de sesiones: 24 horas
  - Bug de gameplay que rompe el loop central: 48 horas
  - Bug de IAP (no se otorgan Conchas): 12 horas (prioridad máxima — implica dinero del usuario)
  
Proceso:
  1. Fix en rama hotfix/v1.0.1
  2. Test en dispositivo piso
  3. Build de release
  4. Google Play: subir y activar rollout al 20% → 100% en 2h si no hay más crashes
  5. App Store: submit expedited review (solo para bugs críticos — Apple lo puede aprobar en horas)
```

---

## Roadmap Post-Lanzamiento

Lo que no entró en v1.0 y está documentado como deuda:

### v1.1 — Primera actualización (4-6 semanas post-lanzamiento)
```
□ Ranking global online (requiere backend — Supabase o Firebase Leaderboard)
□ Compartir resultado como imagen (score card para redes sociales)
□ Anuncios opcionales (AdMob rewarded video) para ganar Conchas sin pagar
□ Corrección de bugs reportados en reviews de lanzamiento
```

### v1.2 — Segunda actualización (3 meses post-lanzamiento)
```
□ Modo "Desafío Semanal" — un reto especial de 7 días con recompensa exclusiva
□ Nueva piedra (Obsidiana) — desbloqueada con distancia acumulada masiva
□ Evento estacional (si el juego tiene traction suficiente)
```

### v2.0 — Si SKIM tiene éxito comercial (6+ meses)
```
□ Multijugador asíncrono: lanzar en el mismo mar que otro jugador (score en tiempo real)
□ Editor de climas: el jugador configura sus propias olas
□ Modo infinito: el mar cambia de clima automáticamente cada 500m
```

---

## Autocrítica — Fase 11

### Fortalezas del plan

**1. Los casos de prueba están ordenados por prioridad**
Los P0 son los que rompen el juego o comprometen dinero del usuario (física, save, IAP). Los P1 son importantes pero no bloquean el lanzamiento. Este orden evita el error clásico de perder tiempo arreglando bugs de UI mientras hay un memory leak sin resolver.

**2. El formulario de beta es breve a propósito**
6 preguntas. Un formulario de 20 preguntas recibe < 10% de respuestas. Un formulario de 6 recibe > 60%. Menos datos de 50 personas son más valiosos que cero datos de nadie.

**3. El live monitoring tiene umbrales concretos**
"Crash rate > 5% → detener rollout" es accionable. "Si hay muchos crashes" no lo es. Los umbrales del día de lanzamiento son los que decide si hay que parar o seguir.

### Debilidades del plan

**1. No hay plan de ASO (App Store Optimization)**
Las keywords elegidas son razonables pero no están respaldadas por investigación de volumen de búsqueda. "Stone skip" puede tener muy poco volumen. Herramientas como AppFollow o Sensor Tower podrían validar esto antes del submit. Es trabajo de 2 horas que puede multiplicar la visibilidad orgánica.

**2. El backend para v1.1 no está especificado**
"Supabase o Firebase" es vago. Si la decisión se toma apresuradamente después del lanzamiento, puede resultar en una arquitectura que no escale. Lo ideal sería decidir durante la Fase 10 ya que el EconomySystem podría preparar endpoints futuros.

**3. No hay presupuesto de UA (User Acquisition)**
El plan asume crecimiento orgánico. Para un juego sin marketing, el ranking de búsqueda en tiendas es casi todo. Si el juego no aparece en las primeras páginas de "arcade" o "physics game", las descargas serán mínimas. Se necesita al mínimo 1 campaña de UAC (Universal App Campaigns) en Google Ads con presupuesto pequeño para las primeras 2 semanas.

---

## Estado final del proyecto al lanzamiento

Con la Fase 11 completa, SKIM está publicado en Google Play Store y App Store.

Las 11 fases del estudio han producido:

| Fase | Entregable |
|------|-----------|
| 1 — Investigación | Análisis de mercado — referentes, brechas, oportunidades |
| 2 — Ideas | 35 conceptos, 3 finalistas, concepto SKIM fusionado |
| 3 — Validación | GDD de validación — mecánica central, 7 decisiones de diseño |
| 4 — GDD | Game Design Document completo — sistemas, economía, progresión |
| 5 — UX/UI | Prototipo de 7 pantallas en Pencil, sistema de diseño completo |
| 6 — Arte | Dirección de arte — 4 pilares, 5 paletas de clima, specs de VFX |
| 7 — Tecnología | Unity 2022.3 LTS, selección de stack, performance budgets |
| 8 — Arquitectura | Interfaces C#, modelos de datos, boot sequence, save system |
| 9 — Vertical Slice | Primer código ejecutable — 4 sprints, criterios de feeling |
| 10 — Desarrollo | Juego completo — 4 piedras, 5 climas, economía, monetización |
| 11 — QA y Lanzamiento | **SKIM en producción** |

**Una piedra. Un flick. El océano entero.**
