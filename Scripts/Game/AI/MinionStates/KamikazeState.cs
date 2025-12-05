using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion plays extremely agressively, he does not care about dying because the player can replace him afterwards.
public partial class KamikazeState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        Grid2D grid = Board.Grid;

        //if healthy enough not boom boom
        if (minion.Health > minion.MaxHealth * 0.25f)
        {
            TransitionToSibling("AttackMoveState");
            return true;
        }

        foreach (var cell in grid.GetAdjacents(minion.Position, includeDiagonals: false))
        {
            var data = boardState.GetCellData(cell);
            if (data.Minion != null && data.Minion.Owner != minion.Owner)
            {
                TransitionToSibling("PunchState");
                return true;
            }
        }

        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        List<Vector2I> clickedCells = new();

        // Looking for juicy cells
        Vector2I? target = influence.FindBestCell(
            cell =>
            {
                var data = boardState.GetCellData(cell);
                return data.Tile != null &&
                       influence.MoveCostMap[cell.X, cell.Y] < float.PositiveInfinity;
            },
            cell =>
            {
                float total   = influence.TroopInfluence[cell.X, cell.Y];
                float enemy   = Mathf.Max(0f,  total);   // Player
                float ally    = Mathf.Max(0f, -total);   // Enemy
                float structV = influence.StructureValueMap[cell.X, cell.Y];

                // He doesnt care so much about the influences
                return enemy * 2.0f
                     + structV * 3.0f
                     - ally * 0.1f;
            });

        if (target == null)
            return clickedCells.ToArray();

        Vector2I[] path = GridNavigation.GetPathForMinion(minion, target.Value);
        if (path == null || path.Length == 0)
            return clickedCells.ToArray();

        // THE WHOLE PATH AS CLICKED CELLS, CHANGE TO ONLY THE FIRST AND LAST ONE IF NEEDED
        foreach (var cell in path)
            clickedCells.Add(cell);

        return clickedCells.ToArray();
    }
}
