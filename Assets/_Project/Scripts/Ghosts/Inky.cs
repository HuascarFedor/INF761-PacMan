using UnityEngine;

public class Inky : GhostBase
{
    protected override Vector2Int GetChaseTarget()
    {
        var pacman = GameManager.Instance.Pacman;
        if (pacman == null) return Vector2Int.zero;

        // Punto A: 2 tiles delante de Pac-Man.
        Vector2Int A = pacman.CurrentGrid + pacman.CurrentDir * 2;

        // (Reproducir bug similar al de Pinky cuando Pac-Man mira UP)
        if (pacman.CurrentDir == GridConstants.UP)
            A += GridConstants.LEFT * 2;

        // Punto B: posición de Blinky.
        Vector2Int B = GameManager.Instance.GetBlinkyTile();

        // Target: A + (A - B) = 2A - B
        return 2 * A - B;
    }

}
