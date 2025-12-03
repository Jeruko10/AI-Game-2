using Components;
using Godot;
using System;
using System.Collections.Generic;

namespace Game;

public partial class DefensiveFortFocusedState : State, IGlobalState
{
    public bool TryChangeState()
    {
        // TODO: Determine where to transition: To a sibling: OffensiveFortFocusedState or KillFocusedState.
        
		TransitionToSibling("ExampleState"); // Has to be a sibling state of this state, otherwise push error.
        return false;
    }

    public List<Waypoint> GenerateWaypoints(WaypointsNavigator navigator)
    {
        List<Waypoint> waypoints = new();

        var influence = Board.InfluenceManager;
        var forts = Board.State.Forts;

        // ============================
        // 1) Fortalezas en peligro
        // ============================
        foreach (var fort in forts)
        {
            Vector2I pos = fort.Position;

            float inf = influence.GetInfluenceAt(pos);

            if (inf < 0)
            {
                waypoints.Add(new Waypoint(
                    Waypoint.Types.Capture,
                    pos,
                    Priority: 100  // muy alta
                ));

                // Añadimos waypoints de movimiento defensivo
                foreach (var adj in Board.Grid.GetAdjacents(pos, false))
                {
                    float localInf = influence.GetInfluenceAt(adj);
                    if (localInf >= 0) // zona que podemos reforzar
                    {
                        waypoints.Add(new Waypoint(
                            WaypointType.Move,
                            adj,
                            Priority: 70 + localInf
                        ));
                    }
                }
            }
        }

        // ============================
        // 2) Brecha defensiva
        // ============================
        var weakFrontier = influence.FindWeakAllyFrontierCell();
        if (weakFrontier != null)
        {
            waypoints.Add(new Waypoint(
                WaypointType.Move,
                weakFrontier.Value,
                Priority: 60
            ));
        }

        // ============================
        // 3) Zona de nadie útil
        // ============================
        var noMans = influence.FindNoMansLandCells();
        foreach (var cell in noMans)
        {
            float struct = influence.StructureValueMap[cell.X, cell.Y];
            if (struct > 0) // cerca de un fort = útil
            {
                waypoints.Add(new Waypoint(
                    WaypointType.Move,
                    cell,
                    Priority: 40
                ));
            }
        }

        // ============================
        // 4) Posibles ataques cercanos a fortaleza
        // ============================
        foreach (var fort in forts)
        {
            foreach (var enemy in Board.State.GetEnemyMinions(Board.CurrentPlayer))
            {
                if (Board.Grid.GetDistance(enemy.Position, fort.Position) <= 3)
                {
                    waypoints.Add(new Waypoint(
                        WaypointType.Attack,
                        enemy.Position,
                        Priority: 80
                    ));
                }
            }
        }

        return waypoints;
    }

}
