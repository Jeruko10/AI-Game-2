using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion positions himself to protect an ally or defensive waypoint.
public partial class ProtectTeammateState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        // If not move waypoints, change state
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

        // If already good position, no worries, we calculate one
        if (waypoints != null)
        {
            var defendPoint = waypoints
                .Where(w => w.Type == Waypoint.Types.Move)
                .OrderByDescending(w => w.Priority)
                .FirstOrDefault();

            if (defendPoint != null)
            {
                int dist = Mathf.Abs(minion.Position.X - defendPoint.Cell.X)
                + Mathf.Abs(minion.Position.Y - defendPoint.Cell.Y);

                float t  = influence.TroopInfluence[minion.Position.X, minion.Position.Y];
                float enemy = Mathf.Max(0f, t);
                float ally  = Mathf.Max(0f, -t);

                bool closeEnough = dist <= 2;
                bool notTooDangerous = enemy <= ally * 1.2f;

                if (closeEnough && notTooDangerous)
                {
                    TransitionToSibling("SpreadState");
                    return true;
                }
            }
        }

        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;
        List<Vector2I> clickedCells = [];

        // Looking the highest priority defend point
        var defendPoint = waypoints?
            .Where(w => w.Type == Waypoint.Types.Move)
            .OrderByDescending(w => w.Priority)
            .FirstOrDefault();

        Vector2I? target = null;

        if (defendPoint != null)
        {
            // If not found, look for one near the waypoint
            target = influence.FindBestCell(
                cell =>
                {
                    var data = boardState.GetCellData(cell);
                    if (data.Tile == null) return false;
                    if (influence.MoveCostMap[cell.X, cell.Y] == float.PositiveInfinity) return false;

                    int dist = Mathf.Abs(cell.X - defendPoint.Cell.X) 
                    + Mathf.Abs(cell.Y - defendPoint.Cell.Y);

                    return dist <= 4; // i hate this manhattan guy
                },
                cell =>
                {
                    float t      = influence.TroopInfluence[cell.X, cell.Y];
                    float enemy  = Mathf.Max(0f, t);
                    float ally   = Mathf.Max(0f, -t);
                    
                    // Near the point + allies covering + some enemies
                    int dist = Mathf.Abs(cell.X - defendPoint.Cell.X) 
                    + Mathf.Abs(cell.Y - defendPoint.Cell.Y);

                    float distScore = -dist;

                    return ally * 1.5f
                         + enemy * 0.5f
                         + distScore * 1.0f;
                });
        }

        // if not defensive point, fallback mode
        if (target == null)
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
                    float t      = influence.TroopInfluence[cell.X, cell.Y];
                    float enemy  = Mathf.Max(0f, t);
                    float ally   = Mathf.Max(0f, -t);
                    return ally - enemy;
                });
        }

        if (target == null)
            return [.. clickedCells];

        Vector2I[] path = GridNavigation.GetPathForMinion(minion, target.Value);
        if (path == null || path.Length == 0)
            return [.. clickedCells];

        clickedCells.Add(path[0]); //Click the minion
        clickedCells.Add(path[path.Length-1]); //Click the last position

        return [.. clickedCells];
    }
}
