using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion spreads around the defensive area so allies are not stacked in the same tile.
public partial class SpreadState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        // Si waypoint principal deja de ser Defend, cambiar de modo
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
            if (top.Type == Waypoint.Types.Capture)
            {
                TransitionToSibling("DominateState");
                return true;
            }
        }

        // Si aparece mucho peligro cerca, volver a ProtectTeammateState
        float t      = influence.TroopInfluence[minion.Position.X, minion.Position.Y];
        float enemy  = Mathf.Max(0f, t);
        float ally   = Mathf.Max(0f, -t);
        bool tooDangerous = enemy > ally * 1.2f;

        if (tooDangerous)
        {
            TransitionToSibling("ProtectTeammateState");
            return true;
        }

        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;
        Grid2D grid = Board.Grid;
        List<Vector2I> clickedCells = [];

        // Waypoint defensivo principal, si existe
        var defendPoint = waypoints?
            .Where(w => w.Type == Waypoint.Types.Move)
            .OrderByDescending(w => w.Priority)
            .FirstOrDefault();

        // Buscar una casilla cercana al área defensiva, libre y no muy pegada a aliados
        Vector2I? target = influence.FindBestCell(
            cell =>
            {
                var data = boardState.GetCellData(cell);
                if (data.Tile == null) return false;
                if (influence.MoveCostMap[cell.X, cell.Y] == float.PositiveInfinity) return false;
                if (boardState.GetCellData(cell).Minion != null) return false; // no pisar aliados

                if (defendPoint != null)
                {
                    int dist = Mathf.Abs(cell.X - defendPoint.Cell.X) 
                     + Mathf.Abs(cell.Y - defendPoint.Cell.Y);

                    return dist >= 1 && dist <= 4; // anillo alrededor del punto
                }

                return true;
            },
            cell =>
            {
                // Penalizar estar pegado a muchos aliados
                int allyAdj = 0;
                foreach (var adj in grid.GetAdjacents(cell, includeDiagonals: false))
                {
                    var data = boardState.GetCellData(adj);
                    if (data.Minion != null && data.Minion.Owner == minion.Owner)
                        allyAdj++;
                }

                float t      = influence.TroopInfluence[cell.X, cell.Y];
                float enemy  = Mathf.Max(0f, t);
                float ally   = Mathf.Max(0f, -t);

                float spreadScore = -allyAdj; // menos aliados pegados, mejor

                return ally * 1.0f
                     + enemy * 0.5f
                     + spreadScore * 1.5f;
            });

        if (target == null)
            return clickedCells.ToArray();

        Vector2I[] path = GridNavigation.GetPathForMinion(minion, target.Value);
        if (path == null || path.Length == 0)
            return clickedCells.ToArray();

        clickedCells.Add(path[0]); //Click the minion
        clickedCells.Add(path[path.Length-1]); //Click the last position

        return clickedCells.ToArray();
    }
}
