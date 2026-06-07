# Guía de Laboratorio — Pac-Man en Unity
### Programación de Videojuegos · Unity 2D · C#

---

## Introducción

En esta guía construirás una recreación fiel del arcade clásico **Pac-Man (1980)** usando Unity y C#. El juego replica los sistemas originales: movimiento basado en grilla, IA individual de los cuatro fantasmas, ciclo Scatter/Chase, power pellets y el sistema de ghost house.

**Duración estimada:** 8 sesiones de laboratorio (∼90 min c/u)  
**Prerrequisitos:** C# básico, Unity Editor 2022+, conceptos de `MonoBehaviour`, `Update`, prefabs y scenes.

---

## Estructura del Proyecto

```
Assets/_Project/
├── Scenes/
│   └── Pacman_Main.unity
├── Scripts/
│   ├── Core/
│   │   ├── GridConstants.cs      ← sistema de coordenadas
│   │   └── MazeData.cs           ← datos del laberinto
│   ├── World/
│   │   ├── MazeBuilder.cs        ← construye el laberinto en runtime
│   │   ├── Pellet.cs             ← punto comible
│   │   └── PowerPellet.cs        ← punto de poder
│   ├── Player/
│   │   └── PacmanController.cs   ← movimiento de Pac-Man
│   ├── Ghosts/
│   │   ├── GhostState.cs         ← enum de estados
│   │   ├── GhostBase.cs          ← clase base de IA
│   │   ├── Blinky.cs             ← fantasma rojo
│   │   ├── Pinky.cs              ← fantasma rosa
│   │   ├── Inky.cs               ← fantasma cian
│   │   └── Clyde.cs              ← fantasma naranja
│   └── Managers/
│       ├── GameManager.cs        ← puntaje, vidas, flujo
│       ├── GhostHouseManager.cs  ← liberación de fantasmas
│       └── ModeManager.cs        ← ciclo Scatter/Chase
└── Prefabs/
    ├── Wall, Pellet, PowerPellet, GhostDoor
    └── Blinky, Pinky, Inky, Clyde
```

---

## Lab 1 — Sistema de Coordenadas y Grilla

### Concepto

El laberinto de Pac-Man es una grilla de **28 columnas × 31 filas**. Trabajar en coordenadas de grilla (enteros) simplifica la detección de colisiones, la IA y el snap de movimiento.

La relación con el mundo de Unity:
- Cada tile mide `1 unidad` de Unity.
- El tile `(col, fila)` tiene su centro en `world = (col, -fila)` (eje Y invertido).

### Archivo: `GridConstants.cs`

```csharp
using UnityEngine;

public static class GridConstants
{
    public const int COLS = 28;
    public const int ROWS = 31;
    public const float TILE_SIZE = 1f;

    public static readonly Vector2Int UP    = new Vector2Int( 0, -1);
    public static readonly Vector2Int DOWN  = new Vector2Int( 0,  1);
    public static readonly Vector2Int LEFT  = new Vector2Int(-1,  0);
    public static readonly Vector2Int RIGHT = new Vector2Int( 1,  0);

    public static readonly Vector2Int[] DIRS_CW = { UP, RIGHT, DOWN, LEFT };

    // Grilla → Mundo
    public static Vector3 GridToWorld(Vector2Int g) =>
        new Vector3(g.x * TILE_SIZE, -g.y * TILE_SIZE, 0f);

    // Mundo → Grilla (redondeo)
    public static Vector2Int WorldToGrid(Vector3 w) =>
        new Vector2Int(
            Mathf.RoundToInt( w.x / TILE_SIZE),
            Mathf.RoundToInt(-w.y / TILE_SIZE)
        );

    public static Vector2Int Opposite(Vector2Int dir) => -dir;

    // El túnel está en la fila 14, fuera de los bordes laterales
    public static bool IsTunnelTile(Vector2Int g) =>
        g.y == 14 && (g.x < 6 || g.x > 21);
}
```

### Actividades

1. Crea el script `GridConstants.cs` en `Scripts/Core/`.
2. En la consola de Unity, prueba la conversión: el tile `(0, 0)` debe dar `world (0, 0)`, y el tile `(13, 15)` debe dar `world (13, -15)`.
3. Explica por qué `UP = (0, -1)` en este sistema de coordenadas.

### Preguntas

- ¿Qué ventajas tiene trabajar con coordenadas de grilla en lugar de posiciones flotantes?
- ¿Qué pasa si divides la pantalla en tiles de 0.5 unidades en lugar de 1? ¿Qué cambios serían necesarios?

---

## Lab 2 — Datos y Construcción del Laberinto

### Concepto

El laberinto se representa como un arreglo de strings. Cada carácter codifica un tipo de tile:

| Carácter | Significado |
|----------|-------------|
| `#` | Muro |
| `.` | Pellet (10 pts) |
| `o` | Power Pellet (50 pts) |
| `-` | Puerta de la ghost house |
| ` ` (espacio) | Vacío transitable |

### Archivo: `MazeData.cs`

```csharp
public static class MazeData
{
    public static readonly string[] LAYOUT = {
        "############################",
        "#............##............#",
        "#.####.#####.##.#####.####.#",
        "#o####.#####.##.#####.####o#",
        // ... 31 filas totales
        "############################"
    };
}
```

> El layout completo es el canónico del arcade original (28×31).

### Archivo: `MazeBuilder.cs`

```csharp
using UnityEngine;

public enum TileType { Wall, Empty, Pellet, PowerPellet, GhostDoor, GhostHouse }

public class MazeBuilder : MonoBehaviour
{
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pelletPrefab;
    [SerializeField] private GameObject powerPelletPrefab;
    [SerializeField] private GameObject ghostDoorPrefab;
    [SerializeField] private Transform  wallsRoot;
    [SerializeField] private Transform  pelletsRoot;

    public static TileType[,] Tiles { get; private set; }
    public static int TotalPellets  { get; private set; }

    private void Awake() => BuildMaze();

    private void BuildMaze()
    {
        Tiles = new TileType[GridConstants.COLS, GridConstants.ROWS];
        TotalPellets = 0;

        for (int row = 0; row < GridConstants.ROWS; row++)
        {
            string line = MazeData.LAYOUT[row];
            for (int col = 0; col < GridConstants.COLS; col++)
            {
                char c = col < line.Length ? line[col] : ' ';
                Vector3 pos = GridConstants.GridToWorld(new Vector2Int(col, row));
                switch (c)
                {
                    case '#':
                        Tiles[col, row] = TileType.Wall;
                        Instantiate(wallPrefab, pos, Quaternion.identity, wallsRoot);
                        break;
                    case '.':
                        Tiles[col, row] = TileType.Pellet;
                        Instantiate(pelletPrefab, pos, Quaternion.identity, pelletsRoot);
                        TotalPellets++;
                        break;
                    case 'o':
                        Tiles[col, row] = TileType.PowerPellet;
                        Instantiate(powerPelletPrefab, pos, Quaternion.identity, pelletsRoot);
                        TotalPellets++;
                        break;
                    case '-':
                        Tiles[col, row] = TileType.GhostDoor;
                        Instantiate(ghostDoorPrefab, pos, Quaternion.identity, wallsRoot);
                        break;
                    default:
                        Tiles[col, row] = TileType.Empty;
                        break;
                }
            }
        }
    }

    // Devuelve true si el tile es caminable.
    // isGhost: los fantasmas pueden pasar por la puerta solo si allowGhostDoor=true.
    public static bool IsWalkable(Vector2Int g, bool isGhost = false,
                                                bool allowGhostDoor = false)
    {
        if (g.x < 0 || g.x >= GridConstants.COLS ||
            g.y < 0 || g.y >= GridConstants.ROWS)
            return g.y == 14; // solo el túnel lateral

        TileType t = Tiles[g.x, g.y];
        if (t == TileType.Wall)      return false;
        if (t == TileType.GhostDoor) return isGhost && allowGhostDoor;
        return true;
    }
}
```

### Setup en el Editor

1. Crea un GameObject vacío `MazeBuilder` en la escena.
2. Adjunta el script `MazeBuilder`.
3. Crea prefabs simples: `Wall` (cubo blanco), `Pellet` (esfera pequeña amarilla), `PowerPellet` (esfera grande), `GhostDoor` (quad rosado).
4. Crea GameObjects vacíos `WallsRoot` y `PelletsRoot` para organizar la jerarquía.
5. Asigna los prefabs en el Inspector.
6. Al darle Play, deben generarse todos los tiles automáticamente.

### Preguntas

- ¿Por qué `IsWalkable` necesita el parámetro `allowGhostDoor`? ¿Qué comportamiento diferente necesitan los fantasmas?
- Si quisieras agregar un nivel 2 con un layout distinto, ¿qué cambiarías?

---

## Lab 3 — Movimiento de Pac-Man

### Concepto

Pac-Man usa un sistema de **dirección encolada**: el jugador puede presionar la siguiente dirección antes de llegar al cruce. La dirección se aplica solo cuando el personaje está cerca del centro de un tile Y el tile siguiente es transitable. Si no es posible, se mantiene la dirección actual.

```
   Tecla presionada → queuedDir
   Al acercarse al centro del tile:
     ¿Es transitable el tile en queuedDir? → aplicar
     ¿No? → mantener currentDir
```

### Archivo: `PacmanController.cs`

```csharp
using UnityEngine;

public class PacmanController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private Vector2Int currentDir = Vector2Int.zero;
    private Vector2Int queuedDir  = Vector2Int.zero;
    private Vector3 startPos;

    public Vector2Int CurrentGrid => GridConstants.WorldToGrid(transform.position);
    public Vector2Int CurrentDir  => currentDir;

    private void Awake() { startPos = transform.position; }

    public void Respawn()
    {
        transform.position = startPos;
        currentDir = Vector2Int.zero;
        queuedDir  = Vector2Int.zero;
    }

    void Update()
    {
        ReadInput();
        TryApplyQueuedDir();
        Move();
    }

    private void ReadInput()
    {
        if (Input.GetKey(KeyCode.UpArrow)    || Input.GetKey(KeyCode.W)) queuedDir = GridConstants.UP;
        if (Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) queuedDir = GridConstants.DOWN;
        if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) queuedDir = GridConstants.LEFT;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) queuedDir = GridConstants.RIGHT;
    }

    private void TryApplyQueuedDir()
    {
        if (queuedDir == Vector2Int.zero) return;

        Vector2Int g         = CurrentGrid;
        Vector3    center    = GridConstants.GridToWorld(g);
        float      dist      = Vector2.Distance(transform.position, center);
        bool       canTurn   = (currentDir == Vector2Int.zero)
                             || (dist <= 0.1f)
                             || (queuedDir == GridConstants.Opposite(currentDir));

        if (!canTurn) return;

        if (MazeBuilder.IsWalkable(g + queuedDir))
        {
            currentDir = queuedDir;
            // Centrar en el eje perpendicular para evitar drift
            if (queuedDir.x != 0) transform.position = new Vector3(transform.position.x, center.y, 0);
            if (queuedDir.y != 0) transform.position = new Vector3(center.x, transform.position.y, 0);
            queuedDir = Vector2Int.zero;
        }
    }

    private void Move()
    {
        if (currentDir == Vector2Int.zero) return;

        Vector3 delta = new Vector3(currentDir.x, -currentDir.y, 0) * (speed * Time.deltaTime);
        Vector3 next  = transform.position + delta;

        // Detener si el siguiente tile es muro y está en el centro del actual
        Vector2Int g           = CurrentGrid;
        Vector3    center      = GridConstants.GridToWorld(g);
        float      distCenter  = Vector2.Distance(transform.position, center);

        if (!MazeBuilder.IsWalkable(g + currentDir) && distCenter < 0.05f)
        {
            transform.position = center;
            currentDir = Vector2Int.zero;
            return;
        }

        // Túnel wrap-around (fila 14)
        if (Mathf.RoundToInt(-next.y) == 14)
        {
            if (next.x < -1.5f)  next.x = 28.4f;
            if (next.x > 28.5f)  next.x = -1.4f;
        }

        transform.position = next;
    }
}
```

### Setup en el Editor

1. Crea un `Sprite` circular amarillo para Pac-Man.
2. Adjunta `PacmanController`, un `Collider2D` (Circle, Is Trigger) y asigna el tag `"Player"`.
3. Posiciona en el tile de inicio: columna 13, fila 23 → `world (13, -23)`.

### Actividades

1. Prueba que Pac-Man se detiene al llegar a un muro y no lo atraviesa.
2. Prueba que puedes presionar anticipadamente la siguiente dirección.
3. Observa en el Inspector los campos `currentDir` y `queuedDir` durante el juego.

### Preguntas

- ¿Por qué se necesita el "centrado en eje perpendicular" al cambiar dirección?
- ¿Qué pasaría si aplicaras `queuedDir` sin verificar si el tile destino es transitable?

---

## Lab 4 — Pellets, Power Pellets y Puntaje

### Concepto

Cuando Pac-Man toca un pellet, se destruye y notifica al `GameManager`. Los power pellets activan el modo Frightened en todos los fantasmas.

### Archivo: `Pellet.cs`

```csharp
using UnityEngine;

public class Pellet : MonoBehaviour
{
    [SerializeField] protected int  points  = 10;
    [SerializeField] protected bool isPower = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance.OnPelletEaten(points, isPower);
        Destroy(gameObject);
    }
}
```

### Archivo: `PowerPellet.cs`

```csharp
public class PowerPellet : Pellet
{
    private void Awake()
    {
        isPower = true;
        points  = 50;
    }
}
```

### Archivo: `GameManager.cs` (puntaje y vidas)

```csharp
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int score       = 0;
    [SerializeField] private int pelletsEaten = 0;
    [SerializeField] private int lives       = 3;

    public int PelletsEaten => pelletsEaten;
    public int Score        => score;
    public int Lives        => lives;

    [SerializeField] private PacmanController pacman;
    [SerializeField] private GhostBase[]      ghosts;

    public PacmanController Pacman => pacman;
    public GhostBase[]      Ghosts => ghosts;

    private bool isDying       = false;
    private int  ghostEatenCombo = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnPelletEaten(int points, bool isPower)
    {
        score        += points;
        pelletsEaten++;

        if (isPower)
            foreach (var g in FindObjectsOfType<GhostBase>())
                g.EnterFrightened();

        GhostHouseManager.Instance?.OnPelletEaten(pelletsEaten);
    }

    public void OnGhostEaten()
    {
        // Combo: 200 → 400 → 800 → 1600
        int pts = 200 * (1 << ghostEatenCombo);
        score += pts;
        ghostEatenCombo = Mathf.Min(ghostEatenCombo + 1, 3);
    }

    public void ResetGhostCombo() => ghostEatenCombo = 0;

    public void OnPacmanDied()
    {
        if (isDying) return;
        isDying = true;
        lives--;
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        // Pausar todo
        pacman.enabled = false;
        foreach (var g in FindObjectsOfType<GhostBase>()) g.enabled = false;

        yield return new WaitForSeconds(1.5f);

        if (lives <= 0) { isDying = false; yield break; } // Game Over

        // Reiniciar
        pacman.Respawn(); pacman.enabled = true;
        foreach (var g in FindObjectsOfType<GhostBase>()) { g.ResetToStart(); g.enabled = true; }
        GhostHouseManager.Instance?.ResetReleaseState(pelletsEaten);
        isDying = false;
    }

    public Vector2Int GetBlinkyTile()
    {
        foreach (var g in FindObjectsOfType<GhostBase>())
            if (g is Blinky) return g.CurrentGrid;
        return Vector2Int.zero;
    }
}
```

### Setup en el Editor

1. Crea un GameObject vacío `GameManager` y adjunta el script.
2. Asigna la referencia al `PacmanController` en el Inspector.
3. Agrega `Collider2D (Circle Trigger)` a los prefabs Pellet y PowerPellet.

### Preguntas

- ¿Por qué `isDying` previene llamadas múltiples a `OnPacmanDied`?
- El combo de fantasmas usa `200 * (1 << n)`. ¿Qué valores genera para n = 0, 1, 2, 3?

---

## Lab 5 — IA Base de los Fantasmas

### Concepto Central: Target Tile

La IA de los fantasmas del arcade original se reduce a **un solo principio**: en cada intersección, el fantasma elige la dirección que minimiza la distancia euclidiana a su **tile objetivo** (target tile). El target cambia según el estado.

```
Chase    → target individual (personalidad de cada fantasma)
Scatter  → target en su esquina del laberinto
Frightened → dirección aleatoria
Eaten    → target = puerta de la ghost house
```

**Reglas de movimiento:**
- El fantasma **no puede invertir** su dirección (excepto en transiciones de modo).
- En Frightened, elige dirección aleatoria entre las opciones válidas.
- Prioridad de desempate: UP > LEFT > DOWN > RIGHT.

### Archivo: `GhostState.cs`

```csharp
public enum GhostState
{
    InHouse,    // esperando dentro de la ghost house
    Leaving,    // saliendo hacia la puerta
    Scatter,    // yendo a su esquina
    Chase,      // persiguiendo según su lógica personal
    Frightened, // asustado (power pellet activo)
    Eaten       // comido, regresando a la ghost house
}
```

### Archivo: `GhostBase.cs` (resumen de la estructura)

La clase base tiene cuatro responsabilidades principales:

#### 1. Variables de estado

```csharp
[SerializeField] protected float normalSpeed     = 5f;
[SerializeField] protected float frightenedSpeed = 2.5f;
[SerializeField] protected float tunnelSpeed     = 3f;
[SerializeField] protected float eatenSpeed      = 8f;
[SerializeField] protected Vector2Int scatterCorner; // esquina asignada
[SerializeField] protected Vector2Int homeTile;      // posición dentro de la casa
[SerializeField] protected GhostState state = GhostState.InHouse;
```

#### 2. Método abstracto: `GetChaseTarget()`

Cada subclase implementa **su propia lógica** para determinar a qué tile apunta en modo Chase. Esta es la "personalidad" de cada fantasma:

```csharp
protected abstract Vector2Int GetChaseTarget();
```

#### 3. Selección de dirección

```csharp
protected virtual Vector2Int ChooseNextDirection(Vector2Int g)
{
    if (state == GhostState.Frightened)
        return ChooseRandomDirection(g);

    if (state == GhostState.InHouse)
    {
        // rebote vertical dentro de la casa
        Vector2Int dir = currentDir != Vector2Int.zero ? currentDir : GridConstants.DOWN;
        if (MazeBuilder.IsWalkable(g + dir, isGhost: true, allowGhostDoor: false))
            return dir;
        return GridConstants.Opposite(dir);
    }

    // Prioridad: UP > LEFT > DOWN > RIGHT
    Vector2Int[] priority = { GridConstants.UP, GridConstants.LEFT,
                               GridConstants.DOWN, GridConstants.RIGHT };
    Vector2Int best     = currentDir;
    float      bestDist = float.MaxValue;

    foreach (var dir in priority)
    {
        if (dir == GridConstants.Opposite(currentDir)) continue; // no invertir

        bool ghostAllowed = (state == GhostState.Eaten || state == GhostState.Leaving);
        if (!MazeBuilder.IsWalkable(g + dir, isGhost: true, allowGhostDoor: ghostAllowed))
            continue;

        float dist = Vector2.Distance(g + dir, targetTile);
        if (dist < bestDist) { bestDist = dist; best = dir; }
    }
    return best;
}
```

#### 4. Transiciones de estado

```csharp
public virtual void EnterScatter()
{
    if (state is GhostState.InHouse or GhostState.Leaving or GhostState.Eaten) return;
    if (state is GhostState.Chase or GhostState.Scatter)
        currentDir = GridConstants.Opposite(currentDir); // reversar al cambiar modo
    state = GhostState.Scatter;
}

public virtual void EnterFrightened()
{
    if (state == GhostState.Eaten)
    {
        if (rend != null) rend.material.color = Color.blue; // visual sin cambiar estado
        return;
    }
    currentDir = GridConstants.Opposite(currentDir);
    state = GhostState.Frightened;
    if (rend != null) rend.material.color = Color.blue;
    CancelInvoke(nameof(EndFrightened));
    Invoke(nameof(EndFrightened), 7f);
}

public virtual void GetEaten()
{
    state = GhostState.Eaten;
    if (rend != null) rend.material.color = new Color(1, 1, 1, 0.3f); // "solo ojos"
}

public virtual void LeaveHouse()
{
    if (state != GhostState.InHouse) return;
    state = GhostState.Leaving;
}
```

#### 5. Colisión con Pac-Man

```csharp
protected void CheckPacmanCollision()
{
    var pacman = GameManager.Instance.Pacman;
    if (pacman == null) return;
    if (Vector2.Distance(transform.position, pacman.transform.position) > 0.6f) return;

    if (state == GhostState.Frightened)
    {
        GameManager.Instance.OnGhostEaten();
        GetEaten();
    }
    else if (state != GhostState.Eaten)
    {
        GameManager.Instance.OnPacmanDied();
    }
}
```

### Actividades

1. Crea `GhostBase.cs` con la estructura completa. Usa `[SerializeField]` en todos los campos para poder verlos en el Inspector.
2. Implementa `Move()`: muévete hacia `currentDir` a velocidad variable, y al llegar al centro de un tile llama `ChooseNextDirection`.
3. Implementa el wrap del túnel en `Move()` para la fila 14.
4. Observa en el Inspector cómo cambia el `state` al activar `EnterFrightened()` desde código.

### Preguntas

- ¿Por qué la inversión de dirección `Opposite(currentDir)` ocurre al cambiar de Scatter a Chase y viceversa, pero no cuando se entra a Frightened desde InHouse?
- ¿Por qué los fantasmas en estado `Eaten` o `Leaving` pueden usar la puerta de la ghost house (`allowGhostDoor: true`) pero los demás no?

---

## Lab 6 — Personalidades de los Fantasmas

### Blinky — El Perseguidor Directo (Rojo)

**Target en Chase:** posición exacta de Pac-Man.  
**Cruise Elroy:** cuando quedan pocos pellets, aumenta su velocidad.

```csharp
public class Blinky : GhostBase
{
    [SerializeField] private int   elroyThreshold = 20;
    [SerializeField] private float elroySpeed     = 6f;

    protected override Vector2Int GetChaseTarget()
    {
        var p = GameManager.Instance.Pacman;
        return p != null ? p.CurrentGrid : Vector2Int.zero;
    }

    protected override void Update()
    {
        base.Update();
        int remaining = MazeBuilder.TotalPellets - GameManager.Instance.PelletsEaten;
        if (remaining <= elroyThreshold && state == GhostState.Chase)
            normalSpeed = elroySpeed;
    }
}
```

**Esquina Scatter:** superior-derecha `(25, 0)`.

---

### Pinky — La Emboscadora (Rosa)

**Target en Chase:** 4 tiles adelante de Pac-Man en su dirección actual.  
**Bug histórico del arcade:** cuando Pac-Man mira hacia arriba, el target se desplaza también 4 tiles a la izquierda (desbordamiento de entero en el hardware original).

```csharp
public class Pinky : GhostBase
{
    protected override Vector2Int GetChaseTarget()
    {
        var p = GameManager.Instance.Pacman;
        if (p == null) return Vector2Int.zero;

        Vector2Int target = p.CurrentGrid + p.CurrentDir * 4;

        // Reproducir el bug histórico del arcade original
        if (p.CurrentDir == GridConstants.UP)
            target += GridConstants.LEFT * 4;

        return target;
    }
}
```

**Esquina Scatter:** superior-izquierda `(2, 0)`.

---

### Inky — El Impredecible (Cian)

**Target en Chase:** usa una construcción geométrica basada en Blinky.

1. Calcula el punto **A** = 2 tiles adelante de Pac-Man.
2. Calcula el vector de la posición de **Blinky** a **A**.
3. El target es **A + (A − Blinky) = 2A − Blinky**.

Esto hace que Inky apunte "al otro lado de Pac-Man respecto a Blinky", creando flanqueo.

```
Blinky -----> A(2 tiles adelante) -----> Target
             ↑ vector duplicado
```

```csharp
public class Inky : GhostBase
{
    protected override Vector2Int GetChaseTarget()
    {
        var p = GameManager.Instance.Pacman;
        if (p == null) return Vector2Int.zero;

        Vector2Int A = p.CurrentGrid + p.CurrentDir * 2;
        if (p.CurrentDir == GridConstants.UP)
            A += GridConstants.LEFT * 2; // bug histórico

        Vector2Int B = GameManager.Instance.GetBlinkyTile();
        return 2 * A - B;
    }
}
```

**Esquina Scatter:** inferior-derecha `(27, 30)`.

---

### Clyde — El Cobarde (Naranja)

**Target en Chase:** si está lejos de Pac-Man (> 8 tiles), se comporta como Blinky. Si se acerca demasiado, huye a su esquina Scatter.

```csharp
public class Clyde : GhostBase
{
    [SerializeField] private float fearRadius = 8f;

    protected override Vector2Int GetChaseTarget()
    {
        var p = GameManager.Instance.Pacman;
        if (p == null) return scatterCorner;

        float dist = Vector2Int.Distance(CurrentGrid, p.CurrentGrid);
        return dist > fearRadius ? p.CurrentGrid : scatterCorner;
    }
}
```

**Esquina Scatter:** inferior-izquierda `(0, 30)`.

---

### Comparativa de Personalidades

| Fantasma | Color | Chase target | Esquina Scatter | Característica |
|----------|-------|--------------|-----------------|----------------|
| Blinky | Rojo | Posición de Pac-Man | Superior-derecha | Persecución directa + Elroy |
| Pinky | Rosa | 4 tiles adelante | Superior-izquierda | Emboscada |
| Inky | Cian | 2×(2 tiles adelante) − Blinky | Inferior-derecha | Flanqueo impredecible |
| Clyde | Naranja | Pac-Man si lejos, esquina si cerca | Inferior-izquierda | Cobardía estratégica |

### Actividades

1. Crea los cuatro scripts de fantasmas heredando de `GhostBase`.
2. Crea prefabs para cada fantasma con su color correspondiente.
3. Con Gizmos activados, observa en la escena a dónde apunta el `targetTile` de cada fantasma durante el juego.
4. Modifica temporalmente el `fearRadius` de Clyde a 4 y observa cómo cambia su comportamiento.

### Preguntas

- ¿Por qué Inky es el fantasma más peligroso cuando Blinky está cerca de Pac-Man?
- ¿Qué ocurriría si Clyde tuviera `fearRadius = 0`? ¿Se comportaría igual que Blinky?
- ¿Por qué se reproduce intencionalmente el bug del arcade original en Pinky e Inky?

---

## Lab 7 — Ghost House Manager

### Concepto

Al inicio de cada partida, los fantasmas no salen todos a la vez. Salen en función de cuántos pellets ha comido Pac-Man:

| Fantasma | Umbral (pellets) | Nota |
|----------|-----------------|------|
| Blinky | 0 | Sale inmediatamente, comienza fuera de la casa |
| Pinky | 0 | Sale al instante también |
| Inky | 30 | Sale al comer 30 pellets |
| Clyde | 60 | Sale al comer 60 pellets |

Cuando un fantasma es comido y regresa a la ghost house, debe salir automáticamente (no esperar el contador de pellets de nuevo).

### Archivo: `GhostHouseManager.cs`

```csharp
using UnityEngine;

public class GhostHouseManager : MonoBehaviour
{
    public static GhostHouseManager Instance { get; private set; }

    [Header("Umbrales (pellets comidos para salir)")]
    [SerializeField] private int blinkyThreshold = 0;
    [SerializeField] private int pinkyThreshold  = 0;
    [SerializeField] private int inkyThreshold   = 30;
    [SerializeField] private int clydeThreshold  = 60;

    [SerializeField] private GhostBase blinky;
    [SerializeField] private GhostBase pinky;
    [SerializeField] private GhostBase inky;
    [SerializeField] private GhostBase clyde;

    private bool blinkyReleased, pinkyReleased, inkyReleased, clydeReleased;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (blinky != null) { blinky.LeaveHouse(); blinkyReleased = true; }
        OnPelletEaten(0); // Pinky sale inmediatamente
    }

    public void OnPelletEaten(int pelletsEaten)
    {
        if (!pinkyReleased  && pelletsEaten >= pinkyThreshold)  { pinky.LeaveHouse();  pinkyReleased  = true; }
        if (!inkyReleased   && pelletsEaten >= inkyThreshold)   { inky.LeaveHouse();   inkyReleased   = true; }
        if (!clydeReleased  && pelletsEaten >= clydeThreshold)  { clyde.LeaveHouse();  clydeReleased  = true; }
    }

    public void ResetReleaseState(int currentPellets)
    {
        blinkyReleased = pinkyReleased = inkyReleased = clydeReleased = false;
        if (blinky != null) { blinky.LeaveHouse(); blinkyReleased = true; }
        OnPelletEaten(currentPellets);
    }
}
```

> **Nota importante:** cuando un fantasma regresa a la ghost house después de ser comido, el auto-release está implementado en `GhostBase.Move()` con un `Invoke(nameof(LeaveHouse), 1f)` — no requiere que el `GhostHouseManager` lo gestione nuevamente.

### Actividades

1. Posiciona los cuatro fantasmas en sus posiciones de inicio dentro de la ghost house.
2. Asigna las referencias en el Inspector de `GhostHouseManager`.
3. Observa en qué momento exacto salen Inky y Clyde comiendo pellets.
4. Prueba el caso de morir antes de que salgan Inky/Clyde: verifica que el `ResetReleaseState` funciona correctamente.

---

## Lab 8 — Mode Manager: Ciclo Scatter / Chase

### Concepto

En el arcade original, todos los fantasmas alternan periódicamente entre **Scatter** (ir a su esquina) y **Chase** (perseguir con su lógica personal). El ciclo del Nivel 1 es:

```
Scatter 7s → Chase 20s → Scatter 7s → Chase 20s →
Scatter 5s → Chase 20s → Scatter 5s → Chase ∞
```

Al cambiar de modo, todos los fantasmas invierten su dirección.

### Archivo: `ModeManager.cs`

```csharp
using UnityEngine;

public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance { get; private set; }

    [System.Serializable]
    public struct Phase
    {
        public bool  isScatter; // true = Scatter, false = Chase
        public float duration;  // segundos; -1 = permanente
    }

    [SerializeField]
    private Phase[] phases = new Phase[]
    {
        new Phase { isScatter = true,  duration =  7f },
        new Phase { isScatter = false, duration = 20f },
        new Phase { isScatter = true,  duration =  7f },
        new Phase { isScatter = false, duration = 20f },
        new Phase { isScatter = true,  duration =  5f },
        new Phase { isScatter = false, duration = 20f },
        new Phase { isScatter = true,  duration =  5f },
        new Phase { isScatter = false, duration = -1f }, // Chase permanente
    };

    private int   currentPhase = 0;
    private float phaseTimer   = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() => ApplyCurrentPhase();

    private void Update()
    {
        if (phases[currentPhase].duration < 0f) return; // fase permanente
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= phases[currentPhase].duration)
        {
            currentPhase = Mathf.Min(currentPhase + 1, phases.Length - 1);
            phaseTimer   = 0f;
            ApplyCurrentPhase();
        }
    }

    private void ApplyCurrentPhase()
    {
        foreach (var g in GameManager.Instance.Ghosts)
        {
            if (phases[currentPhase].isScatter) g.EnterScatter();
            else                                g.EnterChase();
        }
    }

    public bool IsScatter => phases[currentPhase].isScatter;
}
```

### Actividades

1. En el editor, activa los Gizmos del `GhostBase` para ver el `targetTile` de cada fantasma.
2. Observa cómo los cuatro fantasmas se van a sus esquinas durante Scatter.
3. Modifica las duraciones de fase y observa el efecto en la dificultad.
4. Agrega un `Debug.Log` en `ApplyCurrentPhase` para registrar en consola cada cambio de modo con su timestamp.

### Preguntas

- ¿Por qué los fantasmas invierten su dirección al cambiar de modo? ¿Qué pasaría si no lo hicieran?
- ¿En qué situaciones `EnterScatter()` y `EnterChase()` ignoran la llamada? ¿Por qué es importante ese filtrado?

---

## Lab 9 — Integración Final y Pruebas

### Checklist de integración

Verifica cada punto en el Editor antes de considerar el juego completo:

**Escena y jerarquía**
- [ ] `MazeBuilder` genera el laberinto al darle Play
- [ ] Los 240 pellets y 4 power pellets aparecen en las posiciones correctas
- [ ] `GameManager`, `GhostHouseManager` y `ModeManager` están en la escena como GameObjects

**Pac-Man**
- [ ] Se mueve con flechas y WASD
- [ ] No atraviesa paredes
- [ ] El wrap del túnel funciona en ambas direcciones
- [ ] Al comer un pellet se destruye y suma puntaje

**Fantasmas**
- [ ] Blinky comienza fuera y persigue directamente
- [ ] Pinky y Blinky salen al inicio; Inky a los 30 pellets, Clyde a los 60
- [ ] Al comer un power pellet, los cuatro se ponen azules
- [ ] Un fantasma azul comido por Pac-Man se vuelve semi-transparente y regresa a la casa
- [ ] Tras regresar, el fantasma recupera su color y sale automáticamente en ~1 segundo
- [ ] Los fantasmas van a sus esquinas durante Scatter

**Colisiones**
- [ ] Tocar un fantasma en Chase/Scatter mata a Pac-Man
- [ ] Tocar un fantasma en Frightened lo come (+200/400/800/1600 pts en combo)
- [ ] Al morir, Pac-Man reaparece después de 1.5s con los fantasmas reiniciados

### Tabla de puntajes canónicos

| Acción | Puntos |
|--------|--------|
| Pellet | 10 |
| Power Pellet | 50 |
| 1er fantasma comido | 200 |
| 2do fantasma (mismo PP) | 400 |
| 3er fantasma | 800 |
| 4to fantasma | 1.600 |

### Actividades de extensión

1. **UI**: agrega un `TextMeshPro` que muestre el puntaje y las vidas en tiempo real.
2. **Animación**: haz que Pac-Man rote según su `currentDir` y que los fantasmas tengan ojos que apunten al `targetTile`.
3. **Game Over**: cuando `lives == 0`, muestra un panel "GAME OVER" y permite reiniciar.
4. **Nivel 2**: aumenta las velocidades y reduce el tiempo Frightened al completar todos los pellets.
5. **Sonido**: agrega clips de audio para comer pellets, power pellets y morir.

---

## Apéndice A — Fidelidad al Arcade Original

Este proyecto reproduce los siguientes aspectos del arcade Pac-Man de 1980:

| Sistema | Implementado | Notas |
|---------|-------------|-------|
| Grilla 28×31 | ✅ | Layout canónico |
| Blinky apunta directo | ✅ | |
| Pinky apunta 4 tiles adelante | ✅ | |
| Bug UP de Pinky (+4 izquierda) | ✅ | Reproducción intencional del bug histórico |
| Inky usa vector 2×(A−Blinky) | ✅ | |
| Bug UP de Inky (+2 izquierda) | ✅ | |
| Clyde huye al acercarse | ✅ | Radio de 8 tiles |
| Ciclo Scatter/Chase Nivel 1 | ✅ | S7-C20-S7-C20-S5-C20-S5-C∞ |
| Inversión al cambiar modo | ✅ | |
| Tunnel slowdown | ✅ | Velocidad reducida en el túnel |
| Cruise Elroy de Blinky | ⚠️ | 1 umbral (original tiene 2) |
| Timer Frightened fijo 7s | ⚠️ | Original varía por nivel |
| Contadores por fantasma (ghost house) | ⚠️ | Simplificado a contador global |

---

## Apéndice B — Diagrama de Estados del Fantasma

```
                  ┌─────────────┐
                  │   InHouse   │◄──── Eaten llega a casa
                  └──────┬──────┘
                         │ LeaveHouse()
                         ▼
                  ┌─────────────┐
                  │   Leaving   │
                  └──────┬──────┘
                         │ llega a GHOST_HOUSE_EXIT
                         ▼
        EnterScatter() ◄─┤├─► EnterChase()
                         │
              ┌──────────┴──────────┐
              ▼                     ▼
        ┌──────────┐          ┌──────────┐
        │ Scatter  │◄────────►│  Chase   │
        └────┬─────┘          └────┬─────┘
             │     EnterFrightened()│
             └──────────┬──────────┘
                        ▼
                 ┌────────────┐
                 │ Frightened │
                 └─────┬──────┘
          EndFrightened│          │ GetEaten()
                       ▼          ▼
                    Chase      ┌───────┐
                               │ Eaten │
                               └───────┘
```

---

## Apéndice C — Recursos y Referencias

- **The Pac-Man Dossier** (Jamey Pittman) — análisis técnico completo del arcade original: comportamiento exacto de cada fantasma, timings, bugs documentados.
- **Unity Docs — MonoBehaviour**: `Update`, `Awake`, `Start`, `Invoke`, `Coroutines`.
- **Unity Docs — Physics 2D**: `OnTriggerEnter2D`, `Collider2D`.

---

*Guía generada para el proyecto `proyunity/Pacman` — Unity 2022+ / C# — Nivel 1 canónico*
