using Components;
using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public partial class GlobalRootState : State, IGlobalState
{
    public bool TryChangeState()
    {
        // WE SHOULD NEVER BE IN THIS STATE DIRECTLY, since it does nothing and acts as folder for its substates. Please ALWAYS return true and transition to a child state.
		TransitionToChild("Offensive");
        return true;
    }

    public List<Waypoint> GenerateWaypoints(WaypointsNavigator navigator) => []; // We will treat this state as a 'folder'. It's always expected to have an active child state.
}