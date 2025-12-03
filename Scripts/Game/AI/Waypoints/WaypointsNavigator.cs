using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Godot;

namespace Game;
public class WaypointsNavigator
{

    // This shit should be a singleton 
    static InfluenceMapManager InfluenceMap => Board.State.GetNode<InfluenceMapManager>("../../InfluenceMapManager");

    /// <summary>
    /// Generate tactical waypoints for a single minion with different priorities
    /// </summary>
    /// <param name="bot">type of minion for which waypoints are in benefit</param>
    /// <returns></returns>
    public List<Waypoint> GenerateWaypoints(Minion bot)
    {
        List<Waypoint> waypoints = [];


        AddAttackWaypoints(waypoints, bot);
        AddCaptureWaypoints(waypoints, bot);
        //AddMoveWaypoints(waypoints, bot);
        AddDeployWaypoints(waypoints);

        waypoints.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return waypoints;
    }

    public List<Waypoint> GenerateDeployWaypoints()
    {
        List<Waypoint> waypoints = [];
        AddDeployWaypoints(waypoints);
        waypoints.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return waypoints;
    }

    static void AddDeployWaypoints(List<Waypoint> waypoints)
    {
        Mana resources = Board.State.Player2Mana;
        var myForts = Board.State.Forts.Where(f => f.Owner == Board.Players.Player2).ToArray();

        List<Vector2I> candidateCells = [];

        foreach (var fort in myForts)
            candidateCells.AddRange(Board.Grid.GetAdjacents(fort.Position));

        // todo: use insted of this the ZONA MUERTA of @Juanan
        var frontierCell = InfluenceMap.FindWeakAllyFrontierCell();
        
        if (frontierCell.HasValue)
            candidateCells.Add(frontierCell.Value);


        foreach (var cell in candidateCells)
        {
            if (!Board.Grid.IsInsideGrid(cell)) continue;

            Element.Types type = EvaluateMinionType();

            if (!CanAfford(type, resources)) continue;

            Waypoint w = new()
            {
                Type = Waypoint.Types.Deploy,
                Cell = cell,
                ElementAffinity = type,
                Priority = CalculateDeployPriority(cell)
            };

            waypoints.Add(w);
        }
    }

    // I know this is ugly but idc
    static Element.Types EvaluateMinionType()
    {
        int fireCount = Board.State.Minions.Count(m => m.Owner == Board.Players.Player1 && m.Element.Tag == Element.Types.Fire);
        int waterCount = Board.State.Minions.Count(m => m.Owner == Board.Players.Player1 && m.Element.Tag == Element.Types.Water);
        int plantCount = Board.State.Minions.Count(m => m.Owner == Board.Players.Player1 && m.Element.Tag == Element.Types.Plant);

        if (fireCount >= waterCount && fireCount >= plantCount)
            return Element.Types.Water;
        else if (waterCount >= fireCount && waterCount >= plantCount)
            return Element.Types.Plant;
        else
            return Element.Types.Fire;
    }

    static int CalculateDeployPriority(Vector2I cell)
    {
        int priority = 1;

        float influence = InfluenceMap.GetInfluenceAt(cell);
        if (influence < 0)
            priority -= 2;
        else if (influence > 0)
            priority += 2;

        foreach (var enemy in Board.State.Minions.Where(m => m.Owner != Board.Players.Player2))
        {
            int distance = Board.Grid.GetDistance(cell, enemy.Position);
            if (distance < 2) priority -= 3;
            else if (distance < 5) priority += 1;
        }

        var myForts = Board.State.Forts.Where(f => f.Owner == Board.Players.Player2);
        foreach (var fort in myForts)
        {
            int distFort = Board.Grid.GetDistance(cell, fort.Position);
            if (distFort <= 2) priority += 2;
        }

        Element.Types type = Element.Types.None;
        if (!CanAfford(type, Board.State.Player2Mana))
            priority = 0;

        return Math.Max(priority, 0);
    }


        static bool CanAfford(Element.Types elementType, Mana mana)
    {
        MinionData minionData = Minions.AllMinionDatas.FirstOrDefault(md => md.Element.Tag == elementType);

        if (minionData == null)
            return false;

        return minionData.IsAffordable(mana);
    }


    void AddAttackWaypoints(List<Waypoint> waypoints, Minion bot)
    {
        var enemies = Board.State.Minions.Where(m => m.Owner != bot.Owner);

        foreach (var enemy in enemies)
        {
            Waypoint w = new()
            {
                Type = Waypoint.Types.Attack,
                Cell = enemy.Position,
                ElementAffinity = enemy.Element.GetDisadvantage(),
                Priority = CalculateAttackPriority(enemy, bot)
            };
            waypoints.Add(w);
        }
    }

    void AddCaptureWaypoints(List<Waypoint> waypoints, Minion bot)
    {
        var fortsToCapture = Board.State.Forts.Where(f => f.Owner != bot.Owner);
        // GD.Print($"Forts to capture for {bot.Name}: {fortsToCapture.Count()}");

        foreach (var fort in fortsToCapture)
        {
            Waypoint w = new()
            {
                Type = Waypoint.Types.Capture,
                Cell = fort.Position,
                ElementAffinity = fort.Element != null 
                    ? fort.Element.GetDisadvantage() 
                    : Element.Types.None,
                Priority = CalculateCapturePriority(fort, bot)
            };
            waypoints.Add(w);
        }
    }

    void AddMoveWaypoints(List<Waypoint> waypoints, Minion bot)
    {
        // todo : Adapt this in the future when Juanan implement it
        //var safeCells = InfluenceMap.GetSafeCellsFor(bot); 

        // foreach (var cell in safeCells)
        // {
        //     Waypoint w = new()
        //     {
        //         Type = WaypointType.Move,
        //         Cell = cell,
        //         Priority = CalculateMovePriority(cell, bot)
        //     };
        //     waypoints.Add(w);
        // }
    }

    int CalculateAttackPriority(Minion enemy, Minion bot)
    {
        int priority = 10;

        if (bot.Element.GetAdvantage() == enemy.Element.Tag)
            priority += 5;

        int distance = Board.Grid.GetDistance(bot.Position, enemy.Position);
        priority += Math.Max(0, 5 - distance);

        float influence = GetBotInfluence(enemy.Position, bot);

        if (influence > 0)
            priority += 4;
        else if (influence < 0)
            priority -= 4;

        return priority;
    }

    int CalculateCapturePriority(Fort fort, Minion bot)
    {
        int priority = 5;

        if (fort.Owner == null)
            priority += 5;

        int distance = Board.Grid.GetDistance(bot.Position, fort.Position);
        priority += Math.Max(0, 5 - distance);

        float influence = GetBotInfluence(fort.Position, bot);

        if (influence > 0)
            priority += 3;
        else if (influence < 0)
            priority -= 5;

        return priority;
    }

    int CalculateMovePriority(Vector2I cell, Minion bot)
    {
        int priority = 1;

        float influence = GetBotInfluence(cell, bot);

        if (influence > 0)
            priority += 3;
        else if (influence < 0)
            priority -= 3;

        foreach (var enemy in Board.State.Minions.Where(m => m.Owner != bot.Owner))
        {
            int distance = Board.Grid.GetDistance(cell, enemy.Position);
            if (distance < 3)
                priority -= 2;
        }

        return priority;
    }


    // This probably should go to InfluenceMapManager later
    /// <summary>
    ///  Gets the bot's influence in a given cell, normalized according to the bot's owner.
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="bot"></param>
    /// <returns></returns>
    static float GetBotInfluence(Vector2I cell, Minion bot)
    {
        float raw = InfluenceMap.GetInfluenceAt(cell);
        return (bot.Owner == Board.Players.Player2) ? raw : -raw;
    }

    public void ClearWaypoints()
    {
        Board.State.ClearWaypoints();
    }
}

