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

        if (HasManaToDeploy(mana))
            return false;
        
		TransitionToSibling("DefensiveFortFocusedState"); 
        return false;
    }

    public List<Waypoint> GenerateWaypoints(WaypointsNavigator navigator)
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

            Element.Types chosenType = preferredDeployType;
            if (predominantEnemy == Element.Types.None)
                chosenType = Element.GetTypeFromMostMana(myMana);

            waypoints.Add(new Waypoint
            {
                Type = Waypoint.Types.Deploy,
                Cell = cell,
                ElementAffinity = chosenType,
                Priority = priority
            });
        }

        return waypoints;
    }

    /* ========== HELPERS ========== */

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


}
