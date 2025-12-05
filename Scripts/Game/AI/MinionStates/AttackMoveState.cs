using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion moves to the best target based on waypoints and if close enough transitions to Punch state. If low health may transition to Fallback state.
public partial class AttackMoveState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;
        Grid2D grid = Board.Grid;

        //4 DIRECTIONS NOT 8

        // If enemy nearby, change to punching punching
        foreach (var cell in grid.GetAdjacents(minion.Position, includeDiagonals: false))
        {
            var data = boardState.GetCellData(cell);
            if (data.Minion != null && data.Minion.Owner != minion.Owner)
            {
                TransitionToSibling("PunchState");
                return true;
            }
        }

        // Low health, dont go sir
        if (minion.Health <= minion.MaxHealth * 0.3f)
        {
            TransitionToSibling("FallbackState");
            return true;
        }

        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        List<Vector2I> clickedCells = new();

        // WE CAN ALSO FILTER BY THE TYPE OF WAYPOINTS IF WE WANT TO

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
                float moveC   = influence.MoveCostMap[cell.X, cell.Y];

                return enemy * 1.5f
                     + structV * 2f
                     - ally * 0.5f
                     - moveC * 0.1f;
            });

        if (target == null)
            return clickedCells.ToArray();

        Vector2I[] path = GridNavigation.GetPathForMinion(minion, target.Value);
        if (path == null || path.Length == 0)
            return clickedCells.ToArray();

        foreach (var cell in path)
            clickedCells.Add(cell);

        return clickedCells.ToArray();
    }
}
