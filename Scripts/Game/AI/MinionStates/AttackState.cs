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
        // por si las moscas
        if (waypoints == null || waypoints.Count == 0)
        {
            GD.Print("me bengo");
            TransitionToChild("AttackMoveState");
            return true;
        }

        // take the waypoint with highest priority
        Waypoint top = waypoints
            .OrderByDescending(w => w.Priority)
            .First();

        GD.Print(top);

        switch (top.Type)
        {
            case Waypoint.Types.Attack:
                TransitionToChild("AttackMoveState");
                break;

            case Waypoint.Types.Capture:
                TransitionToSibling("DominateState");
                break;

            case Waypoint.Types.Move: //asumo move como defensa
                TransitionToSibling("DefendState");
                break;

            
        }

        return true;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => [];
}
