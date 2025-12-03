using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// <summary>Minion stays close to a target teammate and kills nearby enemies.</summary>
public partial class ProtectTeammateState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        // TODO: Determine where to transition: ProtectTeammateState or SpreadState.
        
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
