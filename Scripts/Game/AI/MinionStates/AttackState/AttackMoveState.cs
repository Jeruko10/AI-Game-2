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
        List<Vector2I> clickedCells = [];


        var attackPoints = waypoints
            .Where(w => w.Type == Waypoint.Types.Attack)
            .OrderByDescending(w => w.Priority)
            .ToList();

        Vector2I? target = null;

        if (attackPoints.Count > 0)
        {
            target = attackPoints[0].Cell;
        }
        else
        {
            target = influence.FindBestCell(
                cell =>
                {
                    var data = boardState.GetCellData(cell);
                    return data.Tile != null &&
                        influence.MoveCostMap[cell.X, cell.Y] < float.PositiveInfinity;
                },
                cell =>
                {
                    float total   = influence.TroopInfluence[cell.X, cell.Y];
                    float enemy   = Mathf.Max(0f, total);
                    float ally    = Mathf.Max(0f, -total);
                    float structV = influence.StructureValueMap[cell.X, cell.Y];
                    float moveC   = influence.MoveCostMap[cell.X, cell.Y];

                    return enemy * 1.5f
                        + structV * 2f
                        - ally * 0.5f
                        - moveC * 0.1f;
                });
        }

        if (target == null)
            return [.. clickedCells];

        // DEBUG
        GD.Print($"[AttackMove] minion at {minion.Position}, target = {target}");
        var reachable = GridNavigation.GetReachableCells(minion);
        GD.Print($"[AttackMove] reachable count = {reachable.Length}");


        Vector2I[] path = GridNavigation.GetPathForMinion(minion, target.Value);


        if (path == null || path.Length == 0)
        {
            if (reachable.Length == 0)
                return [.. clickedCells];

            Vector2I best = reachable[0];
            int bestDist = Board.Grid.GetDistance(best, target.Value);

            foreach (var cell in reachable)
            {
                int d = Board.Grid.GetDistance(cell, target.Value);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cell;
                }
            }

            GD.Print($"[AttackMove] fallback best reachable = {best}");

            path = GridNavigation.GetPathForMinion(minion, best);
            if (path == null || path.Length == 0)
                return [.. clickedCells];
        }


        clickedCells.Add(minion.Position);
        clickedCells.Add(path[path.Length-1]);

        return [.. clickedCells];
    }


}
