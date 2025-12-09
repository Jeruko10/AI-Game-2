using Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

/// Minion focuses on defending allies or key positions.
public partial class DefendState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        if(GridNavigation.GetAllPossibleAttacks(minion).Length > 0)
        {
            TransitionToSibling("AttackState");
            return true;
        }

        if(Board.Grid.GetDistance(minion.Position, GridNavigation.GetTopLowHealthAlly().Position) <= 8)
        {
            TransitionToChild("ProtectTeammateState");
            return true;
        }

        Waypoint top = waypoints
            .Where(waypoints => waypoints.Type != Waypoint.Types.Deploy)
            .OrderByDescending(w => w.Priority)
            .First();

        switch (top.Type)
        {
            case Waypoint.Types.Move: //asumo Move como Defend
                TransitionToChild("ProtectTeammateState");
                break;

            case Waypoint.Types.Attack:
                TransitionToSibling("AttackState");
                break;

            case Waypoint.Types.Capture:
                TransitionToSibling("DominateState");
                break;

            default: //por si las moscas
                TransitionToChild("ProtectTeammateState");
                break;
        }

        return true;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints) => [];
}
