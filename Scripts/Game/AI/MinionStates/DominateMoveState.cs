using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion moves to the best fort. When reached, it may change to Prevail state if he desires to guarantee dominance.
public partial class DominateMoveState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        Vector2I pos = minion.Position;
        float structHere = influence.StructureValueMap[pos.X, pos.Y];

        if (structHere > 0f)
        {
            var data = boardState.GetCellData(pos);
            bool ownedByMe = data.Fort != null && data.Fort.Owner == minion.Owner;

            if (!ownedByMe)
            {
                TransitionToSibling("PrevailState");
                return true;
            }
        }

        return false;
    }


    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;
        List<Vector2I> clickedCells = [];

        // Take the highest capture point if more than one
        var capturePoints = waypoints?
            .Where(w => w.Type == Waypoint.Types.Capture)
            .OrderByDescending(w => w.Priority)
            .ToList() ?? [];

        Vector2I? target = null;

        foreach (var wp in capturePoints)
        {
            var data = boardState.GetCellData(wp.Cell);

            // aquí decides qué significa "conquistada"
            bool ownedByMe = data.Fort != null
                            && data.Fort.Owner == minion.Owner;

            if (!ownedByMe)
            {
                target = wp.Cell;
                break;          // usamos la primera no conquistada
            }
        }



        if (target == null)
        {
            // if not found, calculate one urself u son of a Alonso
            target = influence.FindBestCell(
                cell =>
                {
                    var data = boardState.GetCellData(cell);
                    return data.Tile != null &&
                           influence.MoveCostMap[cell.X, cell.Y] < float.PositiveInfinity;
                },
                cell =>
                {
                    float structV = influence.StructureValueMap[cell.X, cell.Y];
                    float total   = influence.TroopInfluence[cell.X, cell.Y];
                    float enemy   = Mathf.Max(0f,  total);
                    float ally    = Mathf.Max(0f, -total);
                    return structV * 3.0f + enemy * 1.0f - ally * 0.5f;
                });
        }

        if (target == null)
            return [.. clickedCells];

        Vector2I[] path = GridNavigation.GetPathForMinion(minion, target.Value);
        if (path == null || path.Length == 0)
            return [.. clickedCells];

        clickedCells.Add(path[0]); //Click the minion
        if(path.Length>1) clickedCells.Add(path[path.Length-1]); //Click the last position
        else clickedCells.Add(path[1]);
        

        GD.Print($"DominateMoveState: Moving minion {minion.Name} from {minion.Position} to {target} via path length {path.Length}");

        return [.. clickedCells];
    }
}
