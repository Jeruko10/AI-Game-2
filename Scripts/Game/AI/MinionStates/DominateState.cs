using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// <summary>Minion focuses on dominating forts.</summary>
public partial class DominateState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            TransitionToChild("DominateMoveState");
            return true;
        }

        Waypoint top = waypoints
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

            case Waypoint.Types.Move: //asumo move como defensa
                TransitionToSibling("DefendState");
                break;

            default: //por si las moscas
                TransitionToChild("DominateMoveState");
                break;
        }

        return true;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => [];
}
