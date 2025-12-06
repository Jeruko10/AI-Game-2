using Components;
using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public partial class OffensiveState : State, IGlobalState
{
    public bool TryChangeState()
    {
        Fort[] myForts = Board.State.GetPlayerForts(Board.Players.Player2);

        if(Board.State.GetPlayerMinions(Board.Players.Player2).Length <= Board.State.GetPlayerMinions(Board.Players.Player1).Length)
        {
            TransitionToSibling("DefensiveState");
            return true;
        }
        
        if (myForts.Length > Board.State.GetPlayerForts(Board.Players.Player1).Length)
        {
            TransitionToChild("KillFocusedState");
            return true;
        }

        if(myForts.Length <= Board.State.GetPlayerForts(Board.Players.Player1).Length || Board.State.GetPlayerMinions(Board.Players.Player1).Length <= 0)
        {
            TransitionToChild("OffensiveFortFocusedState");
            return true;
        }


        return true;
    }

    public List<Waypoint> GenerateWaypoints() => []; // We will treat this state as a 'folder'. It's always expected to have an active child state.
}
