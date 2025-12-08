using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
    [Export] public GlobalRootState RootState;
    
    async Task PlayTurn()
    {
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
            Board.State.SelectedDeployTroopPlayer2 = GetMinionToDeploy(waypoint.ElementAffinity);
            await SimulateHumanClick(waypoint.Cell, true);
        }
    }

    private static MinionData GetMinionToDeploy(Element.Types elementAffinity)
    {
        foreach (MinionData minionData in Minions.AllMinionDatas)
        {
            if (minionData.Element.Tag == elementAffinity)
                return minionData;
        }
        return null;
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
}
