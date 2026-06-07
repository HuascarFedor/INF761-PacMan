using UnityEngine;

public class Blinky : GhostBase
{
    [Header("Cruise Elroy")]
    [Tooltip("Si quedan menos pellets que este umbral, Blinky acelera.")]
    [SerializeField] private int elroyThreshold = 20;
    [SerializeField] private float elroySpeed = 6f;

    protected override Vector2Int GetChaseTarget()
    {
        var pacman = GameManager.Instance.Pacman;
        return pacman != null ? pacman.CurrentGrid : Vector2Int.zero;
    }

    protected override void Update()
    {
        base.Update();
        // Cruise Elroy: acelera cuando quedan pocos pellets.
        int remaining = MazeBuilder.TotalPellets - GameManager.Instance.PelletsEaten;
        if (remaining <= elroyThreshold && state == GhostState.Chase)
            normalSpeed = elroySpeed;
    }

}
