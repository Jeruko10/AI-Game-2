using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion attacks enemies based on waypoints.
public partial class AttackState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            TransitionToChild("AttackMoveState");
            return true;
        }

        Waypoint top = waypoints
            .Where(waypoints => waypoints.Type != Waypoint.Types.Deploy)
            .OrderByDescending(w => w.Priority)
            .First();

        switch (top.Type)
        {
            case Waypoint.Types.Attack:
                TransitionToChild("AttackMoveState");
                break;

            case Waypoint.Types.Capture:
                TransitionToSibling("DominateState");
                break;

            case Waypoint.Types.Move:
                TransitionToSibling("DefendState");
                break;

            
        }

        return true;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => [];
}
