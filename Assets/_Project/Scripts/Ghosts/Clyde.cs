using UnityEngine;

public class Clyde : GhostBase
{
    [Tooltip("Distancia (en tiles) bajo la cual Clyde huye en lugar de perseguir.")]
    [SerializeField] private float fearRadius = 8f;

    protected override Vector2Int GetChaseTarget()
    {
        var pacman = GameManager.Instance.Pacman;
        if (pacman == null) return scatterCorner;

        float dist = Vector2Int.Distance(CurrentGrid, pacman.CurrentGrid);
        if (dist > fearRadius) return pacman.CurrentGrid;
        return scatterCorner;
    }

}
