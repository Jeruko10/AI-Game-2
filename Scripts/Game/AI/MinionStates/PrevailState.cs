using Components;
using Godot;
using System.Collections.Generic;
using System.Data.SqlTypes;

namespace Game;

/// <summary>Minion may attack but does not move, his intention is to keep his fort untouched. May check waypoints for a transition if there's a higher priority.</summary>
public partial class PrevailState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        // TODO: Determine where to transition: DominateMoveState or PrevailState.

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
