using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
    [Export] Label actualMinionStateLabel;
    public IMinionState minionState;
    /// <summary>Iterates over state transitions until a state demands no transition, then plays the strategy returned by the chosen state.</summary>
	async Task PlayMinionStrategy(Minion minion, List<Waypoint> waypoints)
	{
        minionState = ChangeMinionState(minion, waypoints); // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
		Vector2I[] strategy = minionState.GetStrategy(minion, waypoints);

        foreach(var wp in waypoints)
        {
            GD.Print($"Waypoint: Type={wp.Type}, ElementAffinity={wp.ElementAffinity}, Cell={wp.Cell}, Priority={wp.Priority}");
        }
        GD.Print($"BotInputProvider: Minion '{minion.Name}' strategy determined by state '{minionState.GetType().Name}': {string.Join(" -> ", strategy)}");
        foreach (Vector2I cell in strategy) // Play the strategy by simulating human clicks
        {
            if (!Board.State.Minions.Contains(minion)) return; // Minion died during its own turn, he will be forever remembered as a hero.

            await SimulateHumanClick(cell);
        }
	}

    IMinionState ChangeMinionState(Minion minion, List<Waypoint> waypoints)
    {
        int iterations = 0, maxIterations = 10;
        List<string> lastStates = [];

        // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
        do
        {
            iterations++;

            State activeLeafState = minion.RootState.GetDeepestActiveState();
            actualMinionStateLabel.Text = $"Estado del Minion: {activeLeafState.StateName}";
            lastStates.Add(activeLeafState.StateName);

            if (activeLeafState is IMinionState) minionState = activeLeafState as IMinionState;
            else
            {
                GD.PushError($"Active leaf state '{activeLeafState.Name}' does not implement IMinionState interface.");
                return null;
            }
        }
        while (minionState.TryChangeState(minion, waypoints) && iterations < maxIterations);

        if (iterations >= maxIterations)
        {
            string iteratedStates = "";
            foreach (string state in lastStates)
                iteratedStates += $"{state} -> ";

            GD.PushError($"State loop detected, emergency exit at iteration number {iterations}. Iterated over: {iteratedStates}.");
        }

        return minionState;
    }
}
