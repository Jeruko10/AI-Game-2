using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion moves to a good defensive position near a defend waypoint.
public partial class ProtectTeammateState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;


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


        var defendPoint = waypoints?
            .Where(w => w.Type == Waypoint.Types.Move || w.Type == Waypoint.Types.Move)
            .OrderByDescending(w => w.Priority)
            .FirstOrDefault();

        if (defendPoint == null)
            return false; // sin punto a defender, no cambiamos aquí


        int dist =
            Mathf.Abs(minion.Position.X - defendPoint.Cell.X) +
            Mathf.Abs(minion.Position.Y - defendPoint.Cell.Y);

        bool closeEnough = dist <= 2;


        Vector2I pos = minion.Position;
        float totalInfluence = influence.TroopInfluence[pos.X, pos.Y];
        float enemy = Mathf.Max(0f, totalInfluence);
        float ally = Mathf.Max(0f, -totalInfluence);

        bool notTooDangerous = enemy <= ally * 1.2f; 

        if (closeEnough && notTooDangerous)
        {
            TransitionToSibling("SpreadState");
            return true;
        }

        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;
        List<Vector2I> clickedCells = [];


        var defendPoint = waypoints?
            .Where(w => w.Type == Waypoint.Types.Move || w.Type == Waypoint.Types.Move)
            .OrderByDescending(w => w.Priority)
            .FirstOrDefault();

        if (defendPoint == null)
            return [.. clickedCells];


        Vector2I? target = influence.FindBestCell(
            cell =>
            {
                var data = boardState.GetCellData(cell);
                if (data.Tile == null) return false;
                if (influence.MoveCostMap[cell.X, cell.Y] == float.PositiveInfinity) return false;

                int dist =
                    Mathf.Abs(cell.X - defendPoint.Cell.X) +
                    Mathf.Abs(cell.Y - defendPoint.Cell.Y);

                return dist <= 4; 
            },
            cell =>
            {
                float total = influence.TroopInfluence[cell.X, cell.Y];
                float enemy = Mathf.Max(0f, total);
                float ally = Mathf.Max(0f, -total);


                return enemy * 1.0f - ally * 0.3f;
            });

        if (target == null)
            return [.. clickedCells];

        Vector2I[] path = GridNavigation.GetPathForMinion(minion, target.Value);
        if (path == null || path.Length == 0)
            return [.. clickedCells];


        clickedCells.Add(minion.Position);


        for (int i = 1; i < path.Length; i++)
            clickedCells.Add(path[i]);

        return [.. clickedCells];
    }
}
