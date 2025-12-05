using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// Minion attacks enemies based on waypoints.
public partial class AttackState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        // Enter AttackMove as a base, change if you wanna
        TransitionToChild("AttackMoveState");
        return true;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => [];
    // Always waiting for a child
}
