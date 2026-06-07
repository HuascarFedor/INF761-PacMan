using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Score")]
    [SerializeField] private int score = 0;
    [SerializeField] private int pelletsEaten = 0;

    public int PelletsEaten => pelletsEaten;
    public int Score => score;

    [Header("Vidas")]
    [SerializeField] private int lives = 3;
    public int Lives => lives;

    [Header("Referencias")]
    [SerializeField] private PacmanController pacman;
    [SerializeField] private GhostBase[] ghosts;

    public PacmanController Pacman => pacman;
    public GhostBase[] Ghosts => ghosts;

    private bool isDying = false;
    private int ghostEatenCombo = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnPelletEaten(int points, bool isPower)
    {
        score += points;
        pelletsEaten++;
        Debug.Log($"Score: {score}  |  Pellets: {pelletsEaten}/{MazeBuilder.TotalPellets}");

        if (isPower)
        {
            foreach (var g in FindObjectsByType<GhostBase>(FindObjectsSortMode.None)) g.EnterFrightened();
        }
        GhostHouseManager.Instance?.OnPelletEaten(pelletsEaten);
    }

    public void OnGhostEaten()
    {
        int points = 200 * (1 << ghostEatenCombo); // 200, 400, 800, 1600
        score += points;
        ghostEatenCombo = Mathf.Min(ghostEatenCombo + 1, 3);
        Debug.Log($"¡Fantasma comido! +{points}  Score: {score}");
    }

    public void ResetGhostCombo() => ghostEatenCombo = 0;

    public void OnPacmanDied()
    {
        if (isDying) return;
        isDying = true;
        lives--;
        Debug.Log($"Pac-Man murió. Vidas restantes: {lives}");
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        // Pausar a Pac-Man y fantasmas desactivando sus scripts.
        if (pacman != null) pacman.enabled = false;
        var allGhosts = FindObjectsByType<GhostBase>(FindObjectsSortMode.None);
        foreach (var g in allGhosts) g.enabled = false;

        yield return new WaitForSeconds(1.5f);

        if (lives <= 0)
        {
            Debug.Log("Game Over");
            isDying = false;
            yield break;
        }

        // Reiniciar posiciones.
        if (pacman != null) { pacman.Respawn(); pacman.enabled = true; }
        foreach (var g in allGhosts) { g.ResetToStart(); g.enabled = true; }

        // Reiniciar liberación de la ghost house.
        GhostHouseManager.Instance?.ResetReleaseState(pelletsEaten);

        isDying = false;
    }

    public Vector2Int GetBlinkyTile()
    {
        foreach (var g in FindObjectsByType<GhostBase>(FindObjectsSortMode.None))
            if (g is Blinky) return g.CurrentGrid;
        return Vector2Int.zero;
    }
}
