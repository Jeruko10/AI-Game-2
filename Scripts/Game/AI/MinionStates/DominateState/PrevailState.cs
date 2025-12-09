using Components;
using Godot;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

namespace Game;

/// <summary>Minion may attack but does not move, his intention is to keep his fort untouched. May check waypoints for a transition if there's a higher priority.</summary>
public partial class PrevailState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        if(GridNavigation.GetAllPossibleAttacks(minion).Length > 0)
        {
            TransitionToParent();
            return true;
        }

        if(minion.Health <= minion.MaxHealth * 0.3f)
        {
            TransitionToParent();
            return true;
        }
        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        Grid2D grid = Board.Grid;
        List<Vector2I> clickedCells = [];

        // No moving here
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

        if (bestTarget != null)
        {
            clickedCells.Add(minion.Position); //Click the minion
            clickedCells.Add(bestCell); //Click the last position
        }
        clickedCells.AddRange(GridNavigation.GetPunchStrategy(minion));

        // If there are not enemies, dont move
        return [.. clickedCells];
    }
}
