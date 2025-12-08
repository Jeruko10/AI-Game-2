using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static Game.BoardState;

namespace Game;

public partial class OffensiveFortFocusedState : State, IGlobalState
{
    public bool TryChangeState()
    {
        if(Board.State.GetPlayerForts(Board.Players.Player2).Length > Board.State.GetPlayerForts(Board.Players.Player1).Length)
        {
            TransitionToSibling("KillFocusedState");
            return true;
        }
        return false;
    }

    public List<Waypoint> GenerateWaypoints()
    {
        List<Waypoint> waypoints = [];

        var influence = Board.State.influence;
        var allForts = Board.State.Forts;

        var myForts = Board.State.GetPlayerForts(Board.Players.Player2);
        var enemyForts = allForts
            .Where(f => !myForts.Contains(f))
            .ToArray();

        Element.Types enemyDominant = Board.State.GetPlayerDominantElement(Board.Players.Player1);
        Element.Types preferredType = Element.GetAdvantage(enemyDominant);

        foreach (var fort in enemyForts)
        {
            if (fort.Owner == Board.Players.Player2) 
                continue;

            bool isNeutral = fort.Owner == null;
            int fortBasePriority = isNeutral ? 90 : 60;

            waypoints = CreateFortMovementWaypoints();

            waypoints = CreateLowPriorityAttackWaypoints(waypoints, fort, influence, fortBasePriority - 25);

            //waypoints = CreateDeployWaypoints()
        }

        return waypoints;
    }

    public static List<Waypoint> CreateFortMovementWaypoints()
    {
        List<Waypoint> output = [];

        var allForts = Board.State.Forts;

        var enemyOrNeutralForts = allForts
            .Where(f => f.Owner != Board.Players.Player2)
            .ToList();

        if (enemyOrNeutralForts.Count == 0)
            return output;

        var influence = Board.State.influence;

        foreach (var fort in enemyOrNeutralForts)
        {
            Vector2I pos = fort.Position;

            float infAtFort = influence.GetInfluenceAt(pos);

            float localInfluenceScore = 0f;
            const int radius = 2;

            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                Vector2I c = new(pos.X + dx, pos.Y + dy);
                if (!Board.Grid.IsInsideGrid(c)) continue;
                localInfluenceScore += influence.GetInfluenceAt(c);
            }
            int priority = Mathf.RoundToInt(-localInfluenceScore * 5f);
            priority += 20;
            output.Add(new Waypoint
            {
                Type = Waypoint.Types.Capture,
                Cell = pos,
                Priority = priority,
                ElementAffinity = Element.Types.None
            });
        }

        return output;
    }




    private static List<Waypoint> CreateLowPriorityAttackWaypoints(List<Waypoint> output, Fort fort, InfluenceMapManager influence, int basePriority)
    {
        const int scanRange = 3;
        Vector2I origin = fort.Position;

        for (int dx = -scanRange; dx <= scanRange; dx++)
        {
            for (int dy = -scanRange; dy <= scanRange; dy++)
            {
                Vector2I cell = new(origin.X + dx, origin.Y + dy);
                if (!Board.Grid.IsInsideGrid(cell)) continue;

                bool isMinion = Board.State.IsMinionInCell(cell);
                if (!isMinion) continue;
                Minion unit = Board.State.GetMinionAt(cell);
                if (unit.Owner != Board.Players.Player1) continue;

                float inf = influence.GetInfluenceAt(cell);

                int priority =
                    basePriority
                    + Mathf.Clamp(3 - Board.Grid.GetDistance(cell, origin), 0, 3)
                    + Mathf.RoundToInt(inf * 5f);

                output.Add(new Waypoint
                {
                    Type = Waypoint.Types.Attack,
                    Cell = cell,
                    Priority = priority
                });
            }
        }
        return output;
    }


}
