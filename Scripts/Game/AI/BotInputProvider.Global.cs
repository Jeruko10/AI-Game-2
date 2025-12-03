using Components;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
    [Export] GlobalRootState RootState;
    
    readonly WaypointsNavigator navigator = new();
    
    async Task PlayTurn()
    {
        IGlobalState globalState = ChangeGlobalState(); // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
        List<Waypoint> waypoints = globalState.GenerateWaypoints(navigator);

        await SimulateDelay(courtesyDelay);
        await SimulateDeployMinions(waypoints);
        
		foreach(Minion minion in GetFriendlyMinions())
        	await PlayMinionStrategy(minion, waypoints);

        navigator.ClearWaypoints();
        SimulatePassTurn();
    }

    IGlobalState ChangeGlobalState()
    {
        IGlobalState globalState;
        // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
        do
        {
            State activeLeafState = RootState.GetDeepestActiveState();
            
            if (activeLeafState is IGlobalState) globalState = activeLeafState as IGlobalState;
            else
            {
                GD.PushError($"Active leaf state '{activeLeafState.Name}' does not implement IGlobalState interface.");
                return null;
            }
        }
        while (globalState.TryChangeState());

        return globalState;
    }

    List<Waypoint> GetWaypoints() // TODO: Move this to each of the global states GenerateWaypoints method. Each state will have its own way of generating waypoints.
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
