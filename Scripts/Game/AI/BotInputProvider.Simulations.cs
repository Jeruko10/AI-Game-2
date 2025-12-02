using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utility;

namespace Game;

[GlobalClass]
public partial class BotInputProvider : VirtualInputProvider
{
	[Export] Board.Players self;
	[Export] float playSpeed = 1f;
	[Export] float courtesyDelay = 1f;

    public override void _Ready() => Board.State.TurnStarted += OnTurnStarted;

    async Task SimulateDominateFort(List<Waypoint> waypoints, Minion minion)
	{
		if (minion == null) return;

		Vector2I[] reachable = GridNavigation.GetReachableCells(minion);
		List<Vector2I> minionRange = [.. reachable];
		if (!minionRange.Contains(minion.Position))
			minionRange.Add(minion.Position);

		if (minionRange.Count == 0)
			return;

		var goFortWaypoints = waypoints
			.Where(wp => wp.Type == Waypoint.Types.Capture)
			.OrderByDescending(wp => wp.Priority)
			.ToList();

		if (goFortWaypoints.Count == 0)
			return;

		var bestGoFortWaypoint = goFortWaypoints.First();
		Vector2I targetCell = bestGoFortWaypoint.Cell;

		if (!minionRange.Contains(bestGoFortWaypoint.Cell))
		{
			int shortestDist = int.MaxValue;
			Vector2I bestReachable = minion.Position;

			foreach (var cell in minionRange)
			{
				int distToFort = Board.Grid.GetDistance(cell, bestGoFortWaypoint.Cell);
				if (distToFort < shortestDist)
				{
					shortestDist = distToFort;
					bestReachable = cell;
				}
			}

			targetCell = bestReachable;
		}

		await SimulateHumanClick(minion.Position);
		await SimulateHumanClick(targetCell);
	}

    async Task SimulateDeployMinions(List<Waypoint> waypoints)
    {
        var deployWaypoints = waypoints
			.Where(wp => wp.Type == Waypoint.Types.Deploy)
			.OrderByDescending(wp => wp.Priority)
			.ToList();

		if (deployWaypoints.Count == 0)
			return;

		var bestDeployWaypoint = deployWaypoints.First();

		await SimulateHumanClick(bestDeployWaypoint.Cell, true);
    }

    async Task SimulateHumanClick(Vector2I cell, bool rightClick = false, float hoverTime = 0.2f, float afterClickTime = 0.2f)
	{
		SimulateHover(cell);
		await SimulateDelay(hoverTime);

		if (rightClick) SimulateRightClick(cell);
		else SimulateLeftClick(cell);

		await SimulateDelay(afterClickTime);
	}

	async Task SimulateDelay(float seconds) => await Task.Delay((int)Mathf.Round(seconds * 1000 / playSpeed));

	List<Minion> GetEnemyMinions() => GetMinionsOwnedBy(Board.GetRival(self));

	List<Minion> GetFriendlyMinions() => GetMinionsOwnedBy(self);

	static List<Minion> GetMinionsOwnedBy(Board.Players player)
	{
		List<Minion> minions = [];

		foreach (Minion minion in Board.State.Minions)
			if (minion.Owner == player)
				minions.Add(minion);

		return minions;
	}

	async void OnTurnStarted(Board.Players newTurnOwner)
	{
		if (newTurnOwner != self) return;

		await GetTree().DelayUntil(() => InputHandler.InteractionEnabled);
		await PlayTurn();
	}
}
