using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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

        var myMinions = Board.State.GetPlayerMinions(Board.Players.Player2);
        var enemies = Board.State.GetPlayerMinions(Board.Players.Player1);
        var myForts = Board.State.GetPlayerForts(Board.Players.Player2);
        var influence = Board.State.influence;

        // -----------------------------
        // 1) Attack Waypoints
        // -----------------------------
        waypoints = GenerateAttackWaypoints(waypoints, myMinions, enemies, myForts);

        // -----------------------------
        // 2) Movement Waypoints
        // -----------------------------
        var candidateCells = influence.FindNoMansLandCells()
            .Where(c => BoardState.IsCellDeployable(c))
            .ToList();

        waypoints = GenerateMovementWaypoints(waypoints, enemies, myForts, influence, candidateCells);

        waypoints.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return waypoints;
    }

    private static List<Waypoint> GenerateMovementWaypoints(List<Waypoint> waypoints, Minion[] enemies, Fort[] myForts, InfluenceMapManager influence, List<Vector2I> candidateCells)
    {
        foreach (var cell in candidateCells)
        {
            float enemyInfluence = 0f;
            float fortProximity = 0f;

            foreach (var enemy in enemies)
            {
                int d = Board.Grid.GetDistance(cell, enemy.Position);
                if (d <= 4) enemyInfluence += 1f / d;
            }

            foreach (var fort in myForts)
            {
                int d = Board.Grid.GetDistance(cell, fort.Position);
                if (d <= 4) fortProximity += 1f / d;
            }

            if (enemyInfluence + fortProximity < 0.2f) continue;

            float safety = Math.Max(0f, -influence.GetInfluenceAt(cell));
            int priority = Mathf.RoundToInt(safety * 50f + enemyInfluence * 30f + fortProximity * 20f);

            waypoints.Add(new Waypoint
            {
                Type = Waypoint.Types.Move,
                Cell = cell,
                ElementAffinity = Element.Types.None,
                Priority = priority
            });
        }
        return waypoints;
    }

    private static List<Waypoint> GenerateAttackWaypoints(List<Waypoint> waypoints, Minion[] myMinions, Minion[] enemies, Fort[] myForts)
    {
        foreach (var enemy in enemies)
        {
            int distanceToClosestMinion = myMinions.Min(m => Board.Grid.GetDistance(m.Position, enemy.Position));
            int distanceToClosestFort = myForts.Length > 0 ? myForts.Min(f => Board.Grid.GetDistance(f.Position, enemy.Position)) : 0;

            float healthFactor = 1f - (enemy.Health / (float)enemy.MaxHealth);
            float minionProximityFactor = 1f / (1 + distanceToClosestMinion);
            float fortProximityFactor = 1f / (1 + distanceToClosestFort);

            int priority = Mathf.RoundToInt(50 + healthFactor * 30 + minionProximityFactor * 20 + fortProximityFactor * 20);

            Element.Types preferredType = Element.GetAdvantage(enemy.Element.Tag);

            waypoints.Add(new Waypoint
            {
                Type = Waypoint.Types.Attack,
                Cell = enemy.Position,
                ElementAffinity = preferredType,
                Priority = priority
            });
        }
        return waypoints;
    }
}
