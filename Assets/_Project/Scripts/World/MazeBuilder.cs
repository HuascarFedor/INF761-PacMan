using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType { Wall, Empty, Pellet, PowerPellet, GhostDoor, GhostHouse };

public class MazeBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject pelletPrefab;
    [SerializeField] private GameObject powerPelletPrefab;
    [SerializeField] private GameObject ghostDoorPrefab;

    [Header("Referencias")]
    [SerializeField] private Transform wallsRoot;
    [SerializeField] private Transform pelletsRoot;

    // Matriz logica accesible para la IA
    public static TileType[,] Tiles { get; private set; }
    public static int TotalPellets { get; private set; }

    private void Awake()
    {
        BuildMaze();
    }

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
                        if (ghostDoorPrefab != null)
                            Instantiate(ghostDoorPrefab, pos, Quaternion.identity, wallsRoot);
                        break;
                    default:
                        Tiles[col, row] = TileType.Empty;
                        break;
                }
            }
        }
    }

    public static bool IsWalkable(Vector2Int g, bool isGhost = false, bool allowGhostDoor = false)
    {
        if (g.x < 0 || g.x >= GridConstants.COLS || g.y < 0 || g.y >= GridConstants.ROWS)
        {
            return g.y == 14; // solo el túnel permite salir del borde
        }
        TileType t = Tiles[g.x, g.y];
        if(t == TileType.Wall) return false;
        if(t == TileType.GhostDoor) return isGhost && allowGhostDoor;
        return true;
    }
}
