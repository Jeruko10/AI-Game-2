using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
    /// <summary>Iterates over state transitions until a state demands no transition, then plays the strategy returned by the chosen state.</summary>
	async Task PlayMinionStrategy(Minion minion, List<Waypoint> waypoints)
	{
        IMinionState minionState = ChangeMinionState(minion, waypoints); // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
		Vector2I[] strategy = minionState.GetStrategy(minion, waypoints);

        foreach (Vector2I cell in strategy) // Play the strategy by simulating human clicks
        {
            if (!Board.State.Minions.Contains(minion)) return; // Minion died during its own turn, he will be forever remembered as a hero.

            await SimulateHumanClick(cell);
        }
	}

    static IMinionState ChangeMinionState(Minion minion, List<Waypoint> waypoints)
    {
        IMinionState minionState;
        // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
        do
        {
            State activeLeafState = minion.RootState.GetDeepestActiveState();
            
            if (activeLeafState is IMinionState) minionState = activeLeafState as IMinionState;
            else
            {
                GD.PushError($"Active leaf state '{activeLeafState.Name}' does not implement IMinionState interface.");
                return null;
            }
        }
        while (minionState.TryChangeState(minion, waypoints));

        return minionState;
    }
}
