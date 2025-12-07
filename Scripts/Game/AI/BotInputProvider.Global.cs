using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
    [Export] GlobalRootState RootState;
    
    async Task PlayTurn()
    {
        GD.Print("BotInputProvider: PlayTurn started.");
        IGlobalState globalState = ChangeGlobalState(); // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
        List<Waypoint> waypoints = globalState.GenerateWaypoints();

        await SimulateDeploy(waypoints);

        await SimulateDelay(courtesyDelay);

        
		foreach(Minion minion in GetFriendlyMinions())
        	await PlayMinionStrategy(minion, waypoints);

        SimulatePassTurn();
    }

    private async Task SimulateDeploy(List<Waypoint> waypoints)
    {
        List<Waypoint> deployWaypoints = [.. waypoints.Where(wp => wp.Type == Waypoint.Types.Deploy)];
        deployWaypoints.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        foreach (Waypoint waypoint in deployWaypoints)
        {
            await SimulateHumanClick(waypoint.Cell, true);
        }
    }

    IGlobalState ChangeGlobalState()
    {
        GD.Print("BotInputProvider: Changing global state.");
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
}
