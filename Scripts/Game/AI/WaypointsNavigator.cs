using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Godot;

namespace Game
{
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

            // This is kinda bullshit 
            if(bot == null)
                return waypoints;

            AddAttackWaypoints(waypoints, bot);
            AddCaptureWaypoints(waypoints, bot);
            //AddMoveWaypoints(waypoints, bot);

            waypoints.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            return waypoints;
        }

        void AddAttackWaypoints(List<Waypoint> waypoints, Minion bot)
        {
            var enemies = Board.State.Minions.Where(m => m.Owner != bot.Owner);

            foreach (var enemy in enemies)
            {
                Waypoint w = new()
                {
                    Type = WaypointType.Attack,
                    Cell = enemy.Position,
                    ElementAffinity = enemy.Element.LosesTo(),
                    Priority = CalculateAttackPriority(enemy, bot)
                };
                waypoints.Add(w);
            }
        }

        void AddCaptureWaypoints(List<Waypoint> waypoints, Minion bot)
        {
            var fortsToCapture = Board.State.Forts.Where(f => f.Owner != bot.Owner);
            GD.Print($"Forts to capture for {bot.Name}: {fortsToCapture.Count()}");

            foreach (var fort in fortsToCapture)
            {
                Waypoint w = new()
                {
                    Type = WaypointType.Capture,
                    Cell = fort.Position,
                    ElementAffinity = fort.Element != null 
                        ? fort.Element.LosesTo() 
                        : Element.Type.None,
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

            if (bot.Element.Beats() == enemy.Element.Tag)
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


        // This probably should go to InfluenceMapManager later @Joao
        /// <summary>
        ///  Gets the bot's influence in a given cell, normalized according to the bot's owner.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="bot"></param>
        /// <returns></returns>
        static float GetBotInfluence(Vector2I cell, Minion bot)
        {
            float raw = InfluenceMap.GetInfluenceAt(cell);
            return (bot.Owner == Board.Players.Player1) ? raw : -raw;
        }

        public void ClearWaypoints()
        {
            //Board.State.ClearWaypoints(); or something like that
            throw new NotImplementedException();
        }


    }



    public class Waypoint
    {
        public WaypointType Type { get; set; }
        public Element.Type ElementAffinity { get; set; }
        public Vector2I Cell { get; set; }
        // priority is in terms of 10x
        public int Priority { get; set; }
    }

    public enum WaypointType
    {
        Attack,
        Capture,
        Move

        // Positioning ?
    }

    // if no tropes in board we need a Positioning waypoint
}
