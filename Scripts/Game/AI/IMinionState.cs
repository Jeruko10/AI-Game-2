using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public interface IMinionState
{
    /// <summary>Checks if the state needs to transition and if so it does.</summary>
    /// <returns>True if a transition occurred, false otherwise.</returns>
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints);

    /// <summary>Returns an array of cells the minion should click this turn based on the provided waypoints.</summary>
    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints);
}