using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utility;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
	async Task PlayMinionStrategy(List<Waypoint> waypoints, Minion minion)
	{
		// Implement the bot's strategy for each unit here
		await SimulateDominateFort(waypoints, minion);
	}
}
