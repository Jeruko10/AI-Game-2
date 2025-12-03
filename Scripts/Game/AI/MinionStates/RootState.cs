using Components;
using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public partial class RootState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        // WE SHOULD NEVER BE IN THIS STATE DIRECTLY, since it does nothing and acts as folder for its substates. Please ALWAYS return true and transition to a child state.
        TransitionToChild("Attack");
        return true;
    }
    
    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => []; // We will treat this state as a 'folder'. It's always expected to have an active child state.
}
