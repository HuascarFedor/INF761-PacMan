using UnityEngine;

public class Pinky : GhostBase
{
    protected override Vector2Int GetChaseTarget()
    {
        var pacman = GameManager.Instance.Pacman;
        if (pacman == null) return Vector2Int.zero;

        Vector2Int p = pacman.CurrentGrid;
        Vector2Int d = pacman.CurrentDir;

        // 4 tiles delante de Pac-Man.
        Vector2Int target = p + d * 4;

        // Bug histórico: cuando Pac-Man mira UP, también se desplaza 4 a la izquierda.
        if (d == GridConstants.UP)
            target += GridConstants.LEFT * 4;

        return target;
    }

}
