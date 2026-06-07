using UnityEngine;

public class PacmanController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 5f;

    [Header("Estado actual (debug)")]
    [SerializeField] private Vector2Int currentDir = Vector2Int.zero;
    [SerializeField] private Vector2Int queuedDir = Vector2Int.zero;

    private Vector3 startPos;

    public Vector2Int CurrentGrid => GridConstants.WorldToGrid(transform.position);
    public Vector2Int CurrentDir => currentDir;

    private void Awake()
    {
        startPos = transform.position;
    }

    public void Respawn()
    {
        transform.position = startPos;
        currentDir = Vector2Int.zero;
        queuedDir = Vector2Int.zero;
    }

    void Update()
    {
        ReadInput();
        TryApplyQueuedDir();
        Move();
    }

    private void ReadInput()
    {
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) queuedDir = GridConstants.UP;
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) queuedDir = GridConstants.DOWN;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) queuedDir = GridConstants.LEFT;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) queuedDir = GridConstants.RIGHT;
    }

    private void TryApplyQueuedDir()
    {
        if (queuedDir == Vector2Int.zero) return;

        Vector2Int g = CurrentGrid;
        Vector3 tileCenter = GridConstants.GridToWorld(g);
        float dist = Vector2.Distance(transform.position, tileCenter);

        // Solo cambiamos dirección cerca del centro del tile (tolerancia 0.1).
        // Si no hay dirección actual (inicio o detenido), omitir la restricción de distancia.
        if (currentDir != Vector2Int.zero && dist > 0.1f && queuedDir != GridConstants.Opposite(currentDir)) return;

        Vector2Int target = g + queuedDir;
        if (MazeBuilder.IsWalkable(target))
        {
            currentDir = queuedDir;
            // centrar para evitar drift acumulado
            if (queuedDir.x != 0) transform.position = new Vector3(transform.position.x, tileCenter.y, 0);
            if (queuedDir.y != 0) transform.position = new Vector3(tileCenter.x, transform.position.y, 0);
            queuedDir = Vector2Int.zero;
        }
    }

    private void Move()
    {
        if (currentDir == Vector2Int.zero) return;

        Vector3 delta = new Vector3(currentDir.x, -currentDir.y, 0) * (speed * Time.deltaTime);
        Vector3 next = transform.position + delta;

        //Detener si el siguiente tile en la dirección actual es muro
        Vector2Int g = CurrentGrid;
        Vector2Int target = g + currentDir;
        Vector3 tileCenter = GridConstants.GridToWorld(g);
        float distToCenter = Vector2.Distance(transform.position, tileCenter);

        if (!MazeBuilder.IsWalkable(target) && distToCenter < 0.05f)
        {
            // Blouqeado contra muro: detener y centrar
            transform.position = tileCenter;
            currentDir = Vector2Int.zero;
            return;
        }

        // Al final de Move(), antes de "transform.position = next":
        // Wrap-around en el túnel (fila 14).
        const int TUNNEL_ROW = 14;
        const float LEFT_EXIT = -1.5f;
        const float RIGHT_EXIT = 28.5f;

        if (Mathf.RoundToInt(-next.y) == TUNNEL_ROW)
        {
            if (next.x < LEFT_EXIT) next.x = RIGHT_EXIT - 0.1f;
            if (next.x > RIGHT_EXIT) next.x = LEFT_EXIT + 0.1f;
        }

        transform.position = next;
    }
}
