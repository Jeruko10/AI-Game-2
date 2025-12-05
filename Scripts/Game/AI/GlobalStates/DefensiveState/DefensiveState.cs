using Components;
using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public partial class DefensiveState : State, IGlobalState
{

    public bool TryChangeState()
    {
        Fort[] myForts = Board.State.GetPlayerForts(Board.Players.Player2);
        Mana myMana = Board.State.Player2Mana;
        List<FortThreatInfo> threatenedForts = [];
        bool canSummon = DeployFocusedState.HasManaToDeploy(myMana);

        foreach (var fort in myForts)
        {
            FortThreatInfo info = DefensiveFortFocusedState.EvaluateFortThreat(fort);
            if (info.IsThreatened)
                threatenedForts.Add(info);
        }

        if (canSummon)
        {
            TransitionToChild("DeployFocusedState");
            return true;
        }

        if (threatenedForts.Count > 0)
        {
            TransitionToChild("DefensiveFortFocusedState");
            return true;
        }

        if (!canSummon && threatenedForts.Count == 0)
        {
            TransitionToSibling("OffensiveState");
            return true;
        }

        return true;
    }

    public List<Waypoint> GenerateWaypoints(WaypointsNavigator navigator) => []; // We will treat this state as a 'folder'. It's always expected to have an active child state.
}
