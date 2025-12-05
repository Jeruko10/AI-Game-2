using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// Minion damages the best target close to him, no moving involved. When finished he may want to transition to AttackMove state.
public partial class PunchState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        Grid2D grid = Board.Grid;

        // Looking at 4 directions (I could look at 8 but I'm tired)

        bool enemyFound = false;

        foreach (var cell in grid.GetAdjacents(minion.Position, includeDiagonals: false))
        {
            var data = boardState.GetCellData(cell);
            if (data.Minion != null && data.Minion.Owner != minion.Owner)
            {
                enemyFound = true;
                break;
            }
        }

        // No enemies in range, go to attackmove
        if (!enemyFound)
        {
            TransitionToSibling("AttackMoveState");
            return true;
        }

        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;

        List<Vector2I> clickedCells = new();

        // To check if there are more enemies (I think there was a OBTAIN ADJACENTS or something but Ill check it later)
        Vector2I[] cardinalDirs =
        {
            new Vector2I(1, 0),
            new Vector2I(-1, 0),
            new Vector2I(0, 1),
            new Vector2I(0, -1)
        };

        Minion bestTarget = null;
        Vector2I bestDir = default;
        int bestHealth = int.MaxValue;

        foreach (var dir in cardinalDirs)
        {
            Vector2I cell = minion.Position + dir;
            var data = boardState.GetCellData(cell);
            Minion enemy = data.Minion;

            if (enemy == null || enemy.Owner == minion.Owner)
                continue;

            if (enemy.Health < bestHealth)
            {
                bestHealth = enemy.Health;
                bestTarget = enemy;
                bestDir = dir;
            }
        }

        // If not enemies found, do nothing
        if (bestTarget == null)
            return clickedCells.ToArray();

        // Pass only the attack cell
        Vector2I attackCell = minion.Position + bestDir;
        clickedCells.Add(attackCell);

        return clickedCells.ToArray();
    }
}
