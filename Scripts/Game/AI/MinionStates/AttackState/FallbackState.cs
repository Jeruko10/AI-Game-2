using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// Minion plays safely due to its risk of dying. If health is extremely low and player has enough resources it may transition to Kamikaze state.
public partial class FallbackState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        // Low health equals boom boom
        if (minion.Health <= minion.MaxHealth * 0.15f)
        {
            TransitionToSibling("KamikazeState");
            return true;
        }

        // If not in danger, go back to attackmove
        Vector2I pos = minion.Position;
        float total   = influence.TroopInfluence[pos.X, pos.Y];
        float enemy   = Mathf.Max(0f,  total);   // Player
        float ally    = Mathf.Max(0f, -total);   // Enemy

        bool tooDangerous = enemy > ally * 1.2f; 

        if (!tooDangerous)
        {
            TransitionToSibling("AttackMoveState");
            return true;
        }

    
        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        InfluenceMapManager influence = Board.State.influence;

        List<Vector2I> clickedCells = [];

        // Looking for a cell with high influence of my friends and low influence from Alonso troops
        Vector2I? safeCell = influence.FindBestCell(
            cell =>
            {
                var data = boardState.GetCellData(cell);
                return data.Tile != null &&
                       influence.MoveCostMap[cell.X, cell.Y] < float.PositiveInfinity;
            },
            cell =>
            {
                float t      = influence.TroopInfluence[cell.X, cell.Y];
                float enemy  = Mathf.Max(0f,  t);
                float ally   = Mathf.Max(0f, -t);

                
                return ally - enemy;
            });

        if (safeCell == null)
            return [.. clickedCells];

        Vector2I[] path = GridNavigation.GetPathForMinion(minion, safeCell.Value);
        if (path == null || path.Length == 0)
            return [.. clickedCells];


        clickedCells.Add(path[0]); //Click the minion
        clickedCells.Add(path[path.Length-1]); //Click the last position


        return [.. clickedCells];
    }
}
