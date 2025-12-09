using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// <summary> Minion focuses on dominating forts. </summary>
public partial class DominateState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {

        var damageCells = GridNavigation.GetAllPossibleAttacks(minion);
        Minion[] minionsInRange = Board.State.Minions
            .Where(m => m.Owner != minion.Owner && damageCells.Contains(m.Position))
            .ToArray();

        if(minionsInRange.Length > 0)
        {
            TransitionToSibling("AttackState");
            return true;
        }

        // if(Board.Grid.GetDistance(minion.Position, GridNavigation.GetTopLowHealthAlly().Position) <= 8)
        // {
        //     TransitionToSibling("DefendState");
        //     return true;
        // }

        Waypoint top = waypoints
            .Where(waypoints => waypoints.Type != Waypoint.Types.Deploy)
            .OrderByDescending(w => w.Priority)
            .First();

        switch (top.Type)
        {
            case Waypoint.Types.Capture: 
                TransitionToChild("DominateMoveState");
                break;

            case Waypoint.Types.Attack:
                TransitionToSibling("AttackState");
                break;

            case Waypoint.Types.Move:
                TransitionToSibling("AttackState");
                break;

            default:
                TransitionToChild("DominateMoveState");
                break;
        }

        return true;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => [];
}
