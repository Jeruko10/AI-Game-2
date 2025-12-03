using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public interface IGlobalState
{
    /// <summary>Checks if the state needs to transition and if so it does.</summary>
    /// <returns>True if a transition occurred, false otherwise.</returns>
    public bool TryChangeState();

    /// <summary>Returns an array of waypoints that the global AI will put.</summary>
    public List<Waypoint> GenerateWaypoints(WaypointsNavigator navigator);
}