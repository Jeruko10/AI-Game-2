using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game;

public partial class DefensiveFortFocusedState : State, IGlobalState
{
    /// <summary>
    /// Radius around the fort to evaluate threat level.
    /// </summary>
    [Export] int threatRadius = 4;
    Fort[] forts;

    public bool TryChangeState()
    {
        forts = Board.State.GetPlayerForts(Board.Players.Player2);   
        bool anyFortThreatened = forts.Any(f => EvaluateFortThreat(f).IsThreatened);

        if (!anyFortThreatened)
        {
            ParentState?.TransitionToParent();
            return true;
        }
        return false;
    }



    public List<Waypoint> GenerateWaypoints(WaypointsNavigator navigator)
    {
        List<Waypoint> waypoints = [];
        forts = Board.State.GetPlayerForts(Board.Players.Player2);

        foreach (var fort in forts)
        {
            var threat = EvaluateFortThreat(fort);

            if (threat.IsThreatened)
            {
                waypoints = AddMoveWaypoints(waypoints, fort.Position, threat.EnemyPressure, threat.PredominantElement());
            }
        }

        waypoints = AddAttackWaypoints(waypoints, forts);

        return waypoints;
    }

    private static void UpdateEnemyInfluence(ref float enemyPressure, ref int samples, ref int fireCount, ref int waterCount, ref int plantCount, Vector2I p, float inf)
    {
        enemyPressure += inf;
        samples++;
        Minion enemy = Board.State.Minions.Find(m => m.Position.Equals(p));
        if (enemy != null)
        {
            switch (enemy.Element.Tag)
            {
                case Element.Types.Fire: fireCount++; break;
                case Element.Types.Water: waterCount++; break;
                case Element.Types.Plant: plantCount++; break;
            }
        }
    }

    private static List<Waypoint> AddAttackWaypoints(List<Waypoint> waypoints, Fort[] forts)
    {
        foreach (var fort in forts)
        {
            foreach (var enemy in Board.State.GetPlayerMinions(Board.Players.Player1))
            {
                if (Board.Grid.GetDistance(enemy.Position, fort.Position) <= 6)
                {
                    waypoints.Add(new Waypoint
                    {
                        Type = Waypoint.Types.Attack,
                        ElementAffinity = Element.GetAdvantage(enemy.Element.Tag),
                        Cell = enemy.Position,
                        Priority = 80
                    });
                }
            }
        }

        return waypoints;
    }

    private static List<Waypoint> AddMoveWaypoints(List<Waypoint> waypoints, Vector2I pos, float enemyPressure, Element.Types predominant)
    {
        int wpCount = Mathf.Clamp(Mathf.RoundToInt(enemyPressure), 1, 6);

        Vector2I[] offsets =
        [
            new(1, 0),  new(1, 1),  new(0, 1),  new(-1, 1),
                    new(-1, 0), new(-1,-1), new(0,-1), new(1,-1)
        ];

        int idx = 0;
        for (int i = 0; i < wpCount; i++)
        {
            Vector2I wp = pos + offsets[idx];

            if (Board.Grid.IsInsideGrid(wp))
                waypoints.Add(new Waypoint
                {
                    Type = Waypoint.Types.Move,
                    ElementAffinity = predominant,
                    Cell = wp,
                    Priority = 100 + (int)enemyPressure
                });

            idx = (idx + 1) % offsets.Length;
        }

        return waypoints;
    }

    private static Element.Types GetPredominantElement(int fireCount, int waterCount, int plantCount)
    {
        Element.Types predominant = Element.Types.None;
        int maxCount = Math.Max(fireCount, Math.Max(waterCount, plantCount));

        if (maxCount > 0)
        {
            if (maxCount == fireCount) predominant = Element.Types.Fire;
            else if (maxCount == waterCount) predominant = Element.Types.Water;
            else predominant = Element.Types.Plant;
        }

        return predominant;
    }

    public static FortThreatInfo EvaluateFortThreat(Fort fort, int threatRadius = 4)
    {
        var influence = Board.State.influence;
        Vector2I pos = fort.Position;
        float localInf = influence.GetInfluenceAt(pos);
        bool directThreat = localInf > 0;

        float enemyPressure = 0f;
        int samples = 0, fireCount = 0, waterCount = 0, plantCount = 0;

        for (int dx = -threatRadius; dx <= threatRadius; dx++)
        {
            for (int dy = -threatRadius; dy <= threatRadius; dy++)
            {
                Vector2I p = new(pos.X + dx, pos.Y + dy);
                if (!Board.Grid.IsInsideGrid(p)) continue;

                float inf = influence.GetInfluenceAt(p);
                if (inf > 0)
                {
                    UpdateEnemyInfluence(ref enemyPressure, ref samples, ref fireCount, ref waterCount, ref plantCount, p, inf);
                }
            }
        }

        return new FortThreatInfo
        {
            Fort = fort,
            DirectThreat = directThreat,
            EnemyPressure = enemyPressure,
            Samples = samples,
            FireCount = fireCount,
            WaterCount = waterCount,
            PlantCount = plantCount
        };
    }

}
