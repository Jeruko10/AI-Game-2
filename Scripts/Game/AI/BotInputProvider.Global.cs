using Components;
using Godot;
using System.Collections.Generic;
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

        await SimulateDelay(courtesyDelay);
        
		foreach(Minion minion in GetFriendlyMinions())
        	await PlayMinionStrategy(minion, waypoints);

        //navigator.ClearWaypoints();
        SimulatePassTurn();
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
