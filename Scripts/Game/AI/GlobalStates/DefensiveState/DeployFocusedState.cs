using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game;

public partial class DeployFocusedState : State, IGlobalState
{
    [Export] private int MaxWaypointsHardCap = 6;
    public bool TryChangeState()
    {
        Mana mana = Board.State.Player2Mana;

        if  (Board.State.GetPlayerMinions(Board.Players.Player2).Length >= Board.State.GetPlayerMinions(Board.Players.Player1).Length +1)
        {
            TransitionToSibling("DefensiveFortFocusedState"); 
            return true;
        }

        if (HasManaToDeploy(mana))
            return false;
        return false;
        
    }

    public List<Waypoint> GenerateWaypoints()
    {
        List<Waypoint> waypoints = [];

        var influence = Board.State.influence;
        var myForts = Board.State.GetPlayerForts(Board.Players.Player2);
        Mana myMana = Board.State.Player2Mana;

        int totalResources = myMana.FireMana + myMana.WaterMana + myMana.PlantMana;
        int toCreate = Mathf.Clamp(totalResources, 0, MaxWaypointsHardCap);
        if (toCreate == 0) return waypoints;

        Element.Types predominantEnemy = Board.State.GetPlayerDominantElement(Board.Players.Player2);
        Element.Types preferredDeployType = Element.GetAdvantage(predominantEnemy);

        List<Vector2I> candidates = [];
        var noMan = influence.FindNoMansLandCells();
        foreach (var c in noMan)
            if (BoardState.IsCellDeployable(c))
                candidates.Add(c);
        // Mezclar candidatos sin alterar su valor estratégico
        var rng = new Random();
        candidates = [.. candidates.OrderBy(_ => rng.Next())];

        int need = toCreate - candidates.Count;
        if (need > 0)
        {
            foreach (var fort in myForts)
            {
                var cell = GetFortDeployableCell(fort.Position, influence, candidates);
                if (cell != null) candidates.Add(cell.Value);
                if (candidates.Count >= toCreate) break;
            }
        }

        if (candidates.Count < toCreate)
        {
            var tried = new HashSet<Vector2I>(candidates);
            for (int i = 0; i < (toCreate - candidates.Count); i++)
            {
                Vector2I? best = influence.FindBestCell(
                    filter: cell => BoardState.IsCellDeployable(cell) && !tried.Contains(cell),
                    score: cell =>
                    {
                        float inf = influence.GetInfluenceAt(cell);
                        float safety = Math.Max(0f, -inf); // mayor = mas segura
                        float moveCost = influence.MoveCostMap[cell.X, cell.Y];
                        float nearFortBonus = 0f;
                        foreach (var fort in myForts)
                        {
                            int d = Board.Grid.GetDistance(cell, fort.Position);
                            nearFortBonus = Math.Max(nearFortBonus, Mathf.Max(0f, (4 - d) / 4f));
                        }
                        return safety * 10f + nearFortBonus * 5f - moveCost * 0.5f;
                    }
                );

                if (best == null) break;
                tried.Add(best.Value);
                candidates.Add(best.Value);
            }
        }

        for (int i = 0; i < Math.Min(toCreate, candidates.Count); i++)
        {
            Vector2I cell = candidates[i];
            float inf = influence.GetInfluenceAt(cell);
            float safety = Math.Max(0f, -inf);
            int nearFortMinDist = myForts.Length != 0 ? myForts.Min(f => Board.Grid.GetDistance(cell, f.Position)) : int.MaxValue;
            int priority = 30 + Mathf.RoundToInt(safety * 50f) + Mathf.Clamp(10 - nearFortMinDist, 0, 8);

            Element.Types chosenType = GetElementDeploy();

            waypoints.Add(new Waypoint
            {
                Type = Waypoint.Types.Deploy,
                Cell = cell,
                ElementAffinity = chosenType,
                Priority = priority
            });
        }

        waypoints.AddRange(OffensiveFortFocusedState.CreateFortMovementWaypoints());

        return waypoints;
    }






    /* ========== HELPERS ========== */

    Element.Types GetElementDeploy()
    {
        var enemyType = Board.State.GetPlayerDominantElement(Board.Players.Player1);
        var myMana = Board.State.GetPlayerMana(Board.Players.Player2);

        if (enemyType == Element.Types.None)
            return GetCheapestAvailableType(myMana);

        Element.Types counter = Element.GetDisadvantage(enemyType);
        if (CanDeployAnyMinionOf(counter, myMana))
            return counter;

        if (CanDeployAnyMinionOf(enemyType, myMana))
            return enemyType;

        Element.Types weak = Element.GetAdvantage(enemyType);
        if (CanDeployAnyMinionOf(weak, myMana))
            return weak;

        return Element.Types.None;
    }

    bool CanDeployAnyMinionOf(Element.Types type, Mana mana)
    {
        if (type == Element.Types.None) 
            return false;

        foreach (var m in Minions.AllMinionDatas)
        {
            if (m.Element.Tag != type)
                continue;

            if (HasEnoughMana(m.Cost, mana))
                return true;
        }

        return false;
    }


    Element.Types GetCheapestAvailableType(Mana mana)
    {
        Element.Types[] all =
        {
            Element.Types.Fire,
            Element.Types.Water,
            Element.Types.Plant
        };

        Element.Types cheapestType = Element.Types.None;
        int cheapestCost = int.MaxValue;

        foreach (var type in all)
        {
            int cost = GetCheapestMinionCost(type, mana);

            if (cost >= 0 && cost < cheapestCost)
            {
                cheapestCost = cost;
                cheapestType = type;
            }
        }

        return cheapestType;
    }

    int ComputeCapturePriority(Fort fort)
    {
        float enemyInfluence = 0f;
        const int radius = 2;

        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            Vector2I cell = new(fort.Position.X + dx, fort.Position.Y + dy);
            if (!Board.Grid.IsInsideGrid(cell)) continue;

            float inf = Board.State.influence.GetInfluenceAt(cell);

            if (inf < 0)
                enemyInfluence += -inf;
        }

        enemyInfluence = Mathf.Clamp(enemyInfluence * 0.3f, 0f, 1f);

        float t = 1f - enemyInfluence;

        return 20 + Mathf.RoundToInt(t * 40f);
    }


    static Vector2I? GetFortDeployableCell(Vector2I origin, InfluenceMapManager influence, List<Vector2I> exclude)
    {
        foreach (var c in Board.Grid.GetAdjacents(origin, true))
        {
            if (!Board.Grid.IsInsideGrid(c)) continue;
            if (exclude.Contains(c)) continue;
            if (!BoardState.IsCellDeployable(c)) continue;
        }
        
        return null;
    }

    public static bool HasManaToDeploy(Mana mana)
    {
        return mana.FireMana > 0 || mana.WaterMana > 0 || mana.PlantMana > 0;
    }

    bool HasEnoughMana(Mana cost, Mana available)
    {
        return available.FireMana  >= cost.FireMana &&
            available.WaterMana >= cost.WaterMana &&
            available.PlantMana >= cost.PlantMana;
    }

    int GetCheapestMinionCost(Element.Types type, Mana mana)
    {
        int best = int.MaxValue;

        foreach (var m in Minions.AllMinionDatas)
        {
            if (m.Element.Tag != type)
                continue;

            if (HasEnoughMana(m.Cost, mana))
            {
                int sum = m.Cost.FireMana + m.Cost.WaterMana + m.Cost.PlantMana;
                if (sum < best)
                    best = sum;
            }
        }

        return (best == int.MaxValue) ? -1 : best;
    }



}
