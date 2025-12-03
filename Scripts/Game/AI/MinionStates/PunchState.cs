using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// <summary>Minion damages the best target close to him, no moving involved. When finished he may want to transition to AttackMove state.</summary>
public partial class PunchState : State, IMinionState
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
