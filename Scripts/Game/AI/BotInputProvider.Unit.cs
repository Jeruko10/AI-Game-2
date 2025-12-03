using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utility;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
	async Task PlayMinionStrategy(Minion minion, List<Waypoint> waypoints)
	{
		Vector2I[] strategy = minion.HFSM.GetStrategy(minion, waypoints);

        foreach (Vector2I cell in strategy)
        {
            if (!Board.State.Minions.Contains(minion)) // Minion died during its own turn, he will be forever remembered as a hero.
                return;

            await SimulateHumanClick(cell);
        }
	}
}
