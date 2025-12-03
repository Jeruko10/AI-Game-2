using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// <summary>Minion moves to the best target based on waypoints and if close enough transitions to Punch state. If low health may transition to Fallback state.</summary>
public partial class AttackMoveState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        // TODO: Determine where to transition: AttackMoveState, PunchState, FallbackState or KamikazeState.
        
		TransitionToSibling("ExampleState"); // Has to be a sibling state of this state, otherwise push error.
        return false; // Return true if a state transition occurred, otherwise false.
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
		List<Vector2I> clickedCells = [];

        // Implement here the logic the minion should use when playing a turn while in this state.


		// ---------------------------------- End of the logic. ----------------------------------

		return clickedCells.ToArray();
    }
}
