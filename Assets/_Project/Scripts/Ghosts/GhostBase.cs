using UnityEngine;
public abstract class GhostBase : MonoBehaviour
{
    [Header("Velocidades (tiles/segundo)")]
    [SerializeField] protected float normalSpeed = 5f;
    [SerializeField] protected float frightenedSpeed = 2.5f;
    [SerializeField] protected float tunnelSpeed = 3f;
    [SerializeField] protected float eatenSpeed = 8f;

    [Header("Esquina de Scatter")]
    [SerializeField] protected Vector2Int scatterCorner;

    [Header("Spawn dentro de la ghost house")]
    [SerializeField] protected Vector2Int homeTile;

    [Header("Estado actual (debug)")]
    [SerializeField] protected GhostState state = GhostState.InHouse;
    [SerializeField] protected Vector2Int currentDir;

    public GhostState State => state;
    public Vector2Int CurrentGrid => GridConstants.WorldToGrid(transform.position);
    public Vector2Int CurrentDir => currentDir;

    protected SpriteRenderer spriteRend;
    protected Color originalColor;
    protected Vector2Int targetTile;
    protected Vector2Int lastChosenTile = new Vector2Int(-999, -999);

    private Vector3 startPos;
    private GhostState startState;

    // Punto de salida de la ghost house (encima de la puerta).
    protected static readonly Vector2Int GHOST_HOUSE_EXIT = new Vector2Int(13, 11);

    protected virtual void Awake()
    {
        spriteRend = GetComponentInChildren<SpriteRenderer>();
        if (spriteRend != null) originalColor = spriteRend.color;
        startPos = transform.position;
        startState = state;
    }

    public virtual void ResetToStart()
    {
        transform.position = startPos;
        state = startState;
        currentDir = Vector2Int.zero;
        lastChosenTile = new Vector2Int(-999, -999);
        CancelInvoke(nameof(EndFrightened));
        CancelInvoke(nameof(LeaveHouse));
        if (spriteRend != null) spriteRend.color = originalColor;
    }

    protected virtual void Update()
    {
        UpdateTargetTile();
        Move();
    }

    /// <summary>
    /// Cada subclase implementa el cálculo de su target en modo Chase.
    /// </summary>
    protected abstract Vector2Int GetChaseTarget();

    /// <summary>
    /// Selecciona el target tile según el estado actual.
    /// </summary>
    protected virtual void UpdateTargetTile()
    {
        switch (state)
        {
            case GhostState.Chase: targetTile = GetChaseTarget(); break;
            case GhostState.Scatter: targetTile = scatterCorner; break;
            case GhostState.Frightened: targetTile = currentDir; break; // ignorado en Move
            case GhostState.Eaten: targetTile = GHOST_HOUSE_EXIT; break;
            case GhostState.Leaving: targetTile = GHOST_HOUSE_EXIT; break;
            case GhostState.InHouse:    /* dirección resuelta en ChooseNextDirection */ break;
        }
    }

    protected virtual void Move()
    {
        float speed = GetCurrentSpeed();
        Vector2Int g = CurrentGrid;
        Vector3 tileCenter = GridConstants.GridToWorld(g);
        float distToCenter = Vector2.Distance(transform.position, tileCenter);

        // Al llegar al centro de un tile (una sola vez por tile), elegimos siguiente dirección.
        if (distToCenter < 0.05f && g != lastChosenTile)
        {
            transform.position = tileCenter;
            lastChosenTile = g;

            if (state == GhostState.Leaving && g == GHOST_HOUSE_EXIT)
            {
                bool scatter = ModeManager.Instance != null && ModeManager.Instance.IsScatter;
                state = scatter ? GhostState.Scatter : GhostState.Chase;
                currentDir = GridConstants.LEFT;
                return;
            }

            if (state == GhostState.Eaten && g == GHOST_HOUSE_EXIT)
            {
                state = GhostState.InHouse;
                currentDir = GridConstants.DOWN;
                if (spriteRend != null) spriteRend.color = originalColor;
                Invoke(nameof(LeaveHouse), 1f); // auto-sale tras llegar a casa
                return;
            }

            currentDir = ChooseNextDirection(g);
        }

        Vector3 delta = new Vector3(currentDir.x, -currentDir.y, 0) * (speed * Time.deltaTime);
        Vector3 next = transform.position + delta;

        // Wrap del túnel.
        if (g.y == 14)
        {
            if (next.x < -1.5f) next.x = 28.4f;
            if (next.x > 28.5f) next.x = -1.4f;
        }

        transform.position = next;

        // Detección de colisión con Pac-Man.
        CheckPacmanCollision();
    }

    /// <summary>
    /// Elige la dirección hacia el target tile minimizando distancia euclidiana,
    /// con desempate por prioridad UP > LEFT > DOWN > RIGHT,
    /// y prohibiendo la inversión de dirección.
    /// </summary>
    protected virtual Vector2Int ChooseNextDirection(Vector2Int g)
    {
        if (state == GhostState.Frightened)
            return ChooseRandomDirection(g);

        // InHouse: rebote vertical — permite invertir dirección cuando está bloqueado.
        if (state == GhostState.InHouse)
        {
            Vector2Int dir = currentDir != Vector2Int.zero ? currentDir : GridConstants.DOWN;
            if (MazeBuilder.IsWalkable(g + dir, isGhost: true, allowGhostDoor: false))
                return dir;
            return GridConstants.Opposite(dir);
        }

        Vector2Int[] priority = { GridConstants.UP, GridConstants.LEFT,
                                  GridConstants.DOWN, GridConstants.RIGHT };

        Vector2Int best = currentDir;
        float bestDist = float.MaxValue;

        foreach (var dir in priority)
        {
            if (dir == GridConstants.Opposite(currentDir)) continue; // no invertir
            Vector2Int next = g + dir;
            bool ghostAllowed = (state == GhostState.Eaten || state == GhostState.Leaving);
            if (!MazeBuilder.IsWalkable(next, isGhost: true, allowGhostDoor: ghostAllowed)) continue;

            float dist = Vector2.Distance(next, targetTile);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = dir;
            }
        }
        return best;
    }

    protected Vector2Int ChooseRandomDirection(Vector2Int g)
    {
        System.Collections.Generic.List<Vector2Int> opts = new();
        foreach (var dir in GridConstants.DIRS_CW)
        {
            if (dir == GridConstants.Opposite(currentDir)) continue;
            if (!MazeBuilder.IsWalkable(g + dir, isGhost: true)) continue;
            opts.Add(dir);
        }
        if (opts.Count == 0) return GridConstants.Opposite(currentDir);
        return opts[Random.Range(0, opts.Count)];
    }

    protected float GetCurrentSpeed()
    {
        if (state == GhostState.Frightened) return frightenedSpeed;
        if (state == GhostState.Eaten) return eatenSpeed;
        if (GridConstants.IsTunnelTile(CurrentGrid)) return tunnelSpeed;
        return normalSpeed;
    }

    // ===== Transiciones de estado =====

    public virtual void EnterScatter()
    {
        if (state == GhostState.InHouse || state == GhostState.Leaving ||
            state == GhostState.Eaten  || state == GhostState.Frightened) return;
        if (state == GhostState.Chase || state == GhostState.Scatter)
            currentDir = GridConstants.Opposite(currentDir); // reversión en transición
        state = GhostState.Scatter;
    }

    public virtual void EnterChase()
    {
        if (state == GhostState.InHouse || state == GhostState.Leaving ||
            state == GhostState.Eaten  || state == GhostState.Frightened) return;
        if (state == GhostState.Chase || state == GhostState.Scatter)
            currentDir = GridConstants.Opposite(currentDir);
        state = GhostState.Chase;
    }

    public virtual void EnterFrightened()
    {
        if (state == GhostState.Eaten)
        {
            if (spriteRend != null) spriteRend.color = Color.green;
            return;
        }
        currentDir = GridConstants.Opposite(currentDir);
        state = GhostState.Frightened;
        if (spriteRend != null) spriteRend.color = Color.green;
        CancelInvoke(nameof(EndFrightened));
        Invoke(nameof(EndFrightened), 7f);
    }

    public virtual void EndFrightened()
    {
        if (state != GhostState.Frightened) return;
        if (spriteRend != null) spriteRend.color = originalColor;
        bool scatter = ModeManager.Instance != null && ModeManager.Instance.IsScatter;
        state = scatter ? GhostState.Scatter : GhostState.Chase;
        GameManager.Instance?.ResetGhostCombo();
    }

    public virtual void GetEaten()
    {
        state = GhostState.Eaten;
        if (spriteRend != null) spriteRend.color = new Color(1, 1, 1, 0.7f);
        // El target se actualiza automáticamente al GHOST_HOUSE_EXIT en UpdateTargetTile.
    }

    public virtual void LeaveHouse()
    {
        if (state != GhostState.InHouse) return;
        state = GhostState.Leaving;
    }

    // ===== Colisión con Pac-Man =====

    protected void CheckPacmanCollision()
    {
        var pacman = GameManager.Instance.Pacman;
        if (pacman == null) return;
        float dist = Vector2.Distance(transform.position, pacman.transform.position);
        if (dist > 0.6f) return;

        if (state == GhostState.Frightened) { GameManager.Instance.OnGhostEaten(); GetEaten(); }
        else if (state != GhostState.Eaten) GameManager.Instance.OnPacmanDied();
    }

    // ===== Gizmos =====

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GridConstants.GridToWorld(targetTile), 0.3f);
        Gizmos.DrawLine(transform.position, GridConstants.GridToWorld(targetTile));

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GridConstants.GridToWorld(scatterCorner), Vector3.one * 0.8f);
    }
}
