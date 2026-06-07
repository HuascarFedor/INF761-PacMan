using UnityEngine;

public static class GridConstants
{
    public const int COLS = 28;
    public const int ROWS = 31;
    public const float TILE_SIZE = 1f;

    // Direccion de los 4 movimientos posibles
    public static readonly Vector2Int UP = new Vector2Int(0, -1);
    public static readonly Vector2Int DOWN = new Vector2Int(0, 1);
    public static readonly Vector2Int LEFT = new Vector2Int(-1, 0);
    public static readonly Vector2Int RIGHT = new Vector2Int(1, 0);

    public static readonly Vector2Int[] DIRS_CW = { UP, RIGHT, DOWN, LEFT };

    // Convierte coordenadas de grilla (col, row) a posicion de mundo
    public static Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(g.x * TILE_SIZE, -g.y * TILE_SIZE, 0f);
    }

    // Convierte una posicion del mundo al tile de grilla mas cercanos
    public static Vector2Int WorldToGrid(Vector3 w)
    {
        return new Vector2Int(
            Mathf.RoundToInt(w.x / TILE_SIZE),
            Mathf.RoundToInt(-w.y / TILE_SIZE)
        );
    }

    public static Vector2Int Opposite(Vector2Int dir) => -dir;

    public static bool IsTunnelTile(Vector2Int g)
    {
        return g.y == 14 && (g.x < 6 || g.x > 21);
    }

}
