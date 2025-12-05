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
        Grid2D grid = Board.Grid;
        List<Vector2I> clickedCells = new();

        Minion bestTarget = null;
        Vector2I bestCell = default;
        int bestHealth = int.MaxValue;

        foreach (var cell in grid.GetAdjacents(minion.Position, includeDiagonals: false))
        {
            var data = boardState.GetCellData(cell);
            Minion enemy = data.Minion;
            if (enemy == null || enemy.Owner == minion.Owner)
                continue;

            if (enemy.Health < bestHealth)
            {
                bestHealth = enemy.Health;
                bestTarget = enemy;
                bestCell = cell;
            }
        }

        if (bestTarget == null)
            return clickedCells.ToArray();

        clickedCells.Add(bestCell);
        return clickedCells.ToArray();
    }

}
