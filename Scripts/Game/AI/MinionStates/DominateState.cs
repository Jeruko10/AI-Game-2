using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// <summary>Minion focuses on dominating forts.</summary>
public partial class DominateState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        // WE SHOULD NEVER BE IN THIS STATE DIRECTLY, since it does nothing and acts as folder for its substates. Please ALWAYS return true and transition to a child or sibling state.
        // TODO: Determine where to transition: To a child: DominateMoveState or PrevailState. Or to a sibling: AttackState or DefendState.
        
		TransitionToChild("ExampleState"); // Has to be a child state of this state, otherwise push error.
		TransitionToSibling("ExampleState"); // Has to be a sibling state of this state, otherwise push error.
        return true;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => []; // We will treat this state as a 'folder'. It's always expected to have an active child state.
}
