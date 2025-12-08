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
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        Vector2I pos = minion.Position;
        float structHere = influence.StructureValueMap[pos.X, pos.Y];

        if (structHere <= 0f)
        {
            TransitionToSibling("DominateMoveState");
            return true;
        }

        var data = boardState.GetCellData(pos);
        bool ownedByMe = data.Fort != null && data.Fort.Owner == minion.Owner;

        if (ownedByMe)
        {
            // fort asegurado: no spamear transiciones, nos quedamos aquí
            return false;
        }

        // If the highest priority is another thing, change state
        if (waypoints != null && waypoints.Count > 0)
        {
            Waypoint top = waypoints
                .OrderByDescending(w => w.Priority)
                .First();

            if (top.Type == Waypoint.Types.Attack)
            {
                TransitionToSibling("AttackState");
                return true;
            }
            if (top.Type == Waypoint.Types.Move) //assuming (creo que se dice asi en ingles) move como defend
            {
                TransitionToSibling("DefendState");
                return true;
            }
        }

        // IF THERE IS AN ENEMY NEARBY, WE COULD CHANGE TO PUNCHSTATE OR SOMETHING
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

        // If there are not enemies, dont move
        return [.. clickedCells];
    }
}
