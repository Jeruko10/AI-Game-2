using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utility;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
    readonly WaypointsNavigator navigator = new();
    
    async Task PlayTurn()
    {
        List<Waypoint> waypoints = GetWaypoints();

        await SimulateDelay(courtesyDelay);
        await SimulateDeployMinions(waypoints);
        
		foreach(Minion minion in GetFriendlyMinions())
        	await PlayMinionStrategy(minion, waypoints);

        navigator.ClearWaypoints();
        SimulatePassTurn();
    }

    List<Waypoint> GetWaypoints()
    {
        List<Waypoint> waypoints = [];

        if (GetFriendlyMinions().Count == 0)
            waypoints = navigator.GenerateDeployWaypoints();
        else
            foreach (Minion minion in GetFriendlyMinions())
                waypoints = navigator.GenerateWaypoints(minion);

        // GD.Print($"Generated {waypoints.Count} waypoints for bot.");

        if (GetFriendlyMinions().Count != 0)
            foreach (Waypoint wp in waypoints)
            {
                // GD.Print($"Waypoint: Type={wp.Type}, Cell={wp.Cell}, ElementAffinity={wp.ElementAffinity}, Priority={wp.Priority}");
                Board.State.AddWaypoint(wp);
            }

        return waypoints;
    }
}
