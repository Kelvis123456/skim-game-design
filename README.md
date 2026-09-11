# SKIM

**Una piedra. Un flick. El océano entero.**

SKIM es un concepto original de videojuego móvil: lanzas piedras planas de un flick sobre un océano procedural y las haces saltar usando física real de rebote. Un solo gesto controla tres variables a la vez — ángulo, fuerza y spin — y el mar nunca es igual dos veces. Pensado con la fórmula arcade de "una acción, un resultado satisfactorio, repetir", pero con una metáfora completamente nueva.

## Qué hay en este repositorio

Este proyecto pasó por un proceso completo de preproducción de videojuego (11 fases: investigación de mercado, ideación, validación de concepto, GDD, UX/UI, dirección de arte, selección de tecnología, arquitectura técnica, vertical slice, plan de desarrollo y QA/lanzamiento) y ya tiene una **implementación inicial en Unity** en marcha — no se quedó solo en el papel.

- `fase1-investigacion/` a `fase11-qa-lanzamiento/` — documentación completa del proceso de diseño (investigación de mercado, 35 conceptos evaluados, Game Design Document, dirección de arte, arquitectura técnica, plan de vertical slice).
- `SKIM/` — proyecto Unity real con el prototipo en desarrollo: sistema de física de rebote de piedras (simulación custom a 120Hz, no motor de físicas estándar de Unity), cámara, economía y datos del juego. ~35 scripts C# en `SKIM/Assets/_Project/`.

## Stack técnico

- **Motor**: Unity 2022.3 LTS + Universal Render Pipeline (URP)
- **Física del gameplay**: simulación custom de rebote (Euler integration a 120Hz), no el motor de físicas nativo de Unity — necesario para que el rebote sea perceptible y justo en cada lanzamiento
- **Target de performance**: 60fps en gama media-alta (Pixel 7 / iPhone 14), 30fps mínimo en gama baja
- **Build**: IL2CPP + ARM64

## Diseño del gameplay

- Física asistida en los primeros lanzamientos para garantizar al menos 2 rebotes mientras el jugador aprende
- Arco de proyección pre-lanzamiento con puntos de trayectoria estimada
- Audio musical emergente: cada salto genera una nota, los combos forman melodías espontáneas
- 5 niveles climáticos que cambian la dificultad del océano: Calma → Brisa → Viento → Marejada → Tormenta
- Piedras con propiedades físicas distintas como progresión de juego

## Estado

Documentación de diseño 100% completa (fases 1-11). Implementación en Unity iniciada — prototipo del sistema de física, cámara y economía del juego en desarrollo activo.
