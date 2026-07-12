# SKIM — Setup en Unity 6

## Lo que ya está hecho
Todos los scripts C# están escritos en `Assets/_Project/Scripts/`.

## Lo que necesitas hacer en Unity (30-40 min)

### Paso 1 — Crear el proyecto
1. Abrir Unity Hub
2. New Project → **Universal 3D** (URP) → Unity 6
3. Nombre: `SKIM`
4. Guardar en: `C:\Users\Usuario\OneDrive\Escritorio\NuevoJuego\SKIM\`
5. Unity creará la carpeta `Assets/` — NO la borra porque ya tiene `_Project/Scripts/` dentro

### Paso 2 — Instalar paquetes
Window → Package Manager:
- **TextMeshPro** → Install (si no está)
- **Input System** → Install (si no está, reiniciar Unity cuando pida)
- **DOTween Pro** → Asset Store → comprar (~$15) o DOTween gratis en Package Manager

### Paso 3 — Crear ScriptableObjects (Assets/_Project/Data/)
Click derecho en Project → Create:
- `SKIM/Stone Data` × 4:
  - **Guijarro**: Rebound=0.72, Elasticity=0.65, Spin=0.30, Radius=0.08, Mass=0.15
  - **Esquisto**: Rebound=0.85, Elasticity=0.55, Spin=0.80, Radius=0.06, Mass=0.12, Unlock=1000
  - **Basalto**: Rebound=0.60, Elasticity=0.80, Spin=0.20, Radius=0.10, Mass=0.40, Unlock=4000
  - **Cuarzo**: Rebound=0.78, Elasticity=0.70, Spin=1.20, Radius=0.07, Mass=0.18, Unlock=8000

- `SKIM/Climate Data` × 5:
  - **Calma**: Multiplier=1.0, Difficulty=1, Harmonics[0]=(A=0.05,WL=12,Freq=0.4) [1]=(A=0.03,WL=7,Freq=0.7)
  - **Brisa**: Multiplier=1.3, Difficulty=2, Unlock=2000, Harmonics[0-2] según fase9 doc
  - **Viento**: Multiplier=1.7, Difficulty=3, Unlock=5000
  - **Marejada**: Multiplier=2.0, Difficulty=4, Unlock=12000
  - **Tormenta**: Multiplier=2.5, Difficulty=5, Unlock=25000

### Paso 4 — Crear Boot.unity
File → New Scene → Empty → guardar como `Assets/_Project/Scenes/Boot.unity`

En la escena Boot:
1. Crear GameObject vacío → nombrar `GameBootstrapper`
2. Agregar componentes:
   - `GameBootstrapper` (script)
   - `StoneSimulatorImpl`
   - `OceanSystemImpl` (necesita MeshFilter + MeshRenderer — se añaden automáticamente)
   - `InputControllerImpl`
   - `AudioSystemImpl` (crear 3 child GameObjects con AudioSource: Music, SFX, Ambience)
   - `ScoringSystemImpl`
   - `ProgressionSystemImpl`
   - `VFXSystemImpl`
   - `EconomySystemImpl`
3. En el Inspector de `GameBootstrapper`, arrastrar cada componente a su slot
4. En `ProgressionSystemImpl`, asignar los arrays de All Stones y All Climates

### Paso 5 — Crear Game.unity
File → New Scene → Empty → guardar como `Assets/_Project/Scenes/Game.unity`

En la escena Game:
1. **Directional Light** — intensidad 0.8, rotación (50, -30, 0)
2. **Ocean** GameObject:
   - Añadir `OceanSystemImpl` (ya lo hace en Boot, pero necesita un renderer aquí)
   - Crear material URP/Lit con color #00C4CC
   - Asignar Default Climate = Calma
3. **Stone** — crear prefab de la piedra (una esfera simple mientras no hay arte)
4. **Main Camera** con `CameraController`
5. Añadir la escena `Game` en Build Settings

### Paso 6 — Build Settings
File → Build Settings:
- Agregar escenas: Boot (index 0), Game (index 1)
- Platform: Android o iOS según tu dispositivo de test

---

## Para testear rápido en el Editor
1. Abrir Boot.unity
2. Press Play
3. Hacer click y arrastrar en la Game View para simular un flick
   (InputControllerImpl tiene soporte para mouse en Editor)

La piedra debería salir, rebotar en el agua, y producir sonido + anillos.
