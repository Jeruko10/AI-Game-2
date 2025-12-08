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

            waypoints = CreateFortMovementWaypoints(waypoints, fort, influence, fortBasePriority, preferredType);

            waypoints = CreateLowPriorityAttackWaypoints(waypoints, fort, influence, fortBasePriority - 25);
        }

        return waypoints;
    }

    private static List<Waypoint> CreateFortMovementWaypoints(List<Waypoint> output, Fort fort, InfluenceMapManager influence, int basePriority, Element.Types preferredType)
    {
        output.Add(new Waypoint
        {
            Type = Waypoint.Types.Capture,
            Cell = fort.Position,
            ElementAffinity = preferredType,
            Priority = basePriority + 20
        });

        const int range = 3;
        const int maxWaypoints = 4; 
        List<Vector2I> candidateCells = new();

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                var cell = new Vector2I(fort.Position.X + dx, fort.Position.Y + dy);

                if (!Board.Grid.IsInsideGrid(cell)) continue;
                if (Board.State.IsCellOccupied(cell)) continue;
                if (Board.Grid.GetDistance(cell, fort.Position) > range) continue;

                float inf = influence.GetInfluenceAt(cell);
                if (inf > 0) continue; 

                candidateCells.Add(cell);
            }
        }

        if (candidateCells.Count == 0)
            return output;

        RandomNumberGenerator rng = new();
        rng.Randomize();
        candidateCells = [.. candidateCells.OrderBy(_ => rng.Randi()).Take(maxWaypoints)];

        foreach (var cell in candidateCells)
        {
            float inf = influence.GetInfluenceAt(cell);
            int dist = Board.Grid.GetDistance(cell, fort.Position);

            int priority =
                basePriority
                - dist * 3
                + Mathf.RoundToInt(Math.Max(0f, -inf) * 10f);

            output.Add(new Waypoint
            {
                Type = Waypoint.Types.Move,
                Cell = cell,
                ElementAffinity = preferredType,
                Priority = priority
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
