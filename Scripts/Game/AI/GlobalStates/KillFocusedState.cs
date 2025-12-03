using Components;
using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public partial class KillFocusedState : State, IGlobalState
{
    public bool TryChangeState()
    {
        // TODO: Determine where to transition: To a sibling: OffensiveFortFocusedState or KillFocusedState.
        
		TransitionToSibling("ExampleState"); // Has to be a sibling state of this state, otherwise push error.
        return false;
    }

    public List<Waypoint> GenerateWaypoints(WaypointsNavigator navigator)
    {
        List<Waypoint> waypoints = [];

        // TODO: Move here the waypoint generation logic based on this state behaviour

        return waypoints;
    }
}
