using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utility;

namespace Game;

[GlobalClass]
public partial class BotInputProvider() : VirtualInputProvider
{
	[Export] Board.Players self;
	[Export] float playSpeed = 1f;
	[Export] float courtesyDelay = 1f;

    readonly WaypointsNavigator navigator = new();

    public override void _Ready()
    {
        Board.State.TurnStarted += OnTurnStarted;
    }

    async Task PlayTurn()
    {
        List<Waypoint> waypoints = GetWaypoints();
        await Wait(courtesyDelay);

        await DeployUnit(waypoints);

		foreach(Minion minion in GetFriendlyMinions())
        	await SimulateGoingFort(waypoints, minion);


		//HERE WILL BE THE BOT LOGIC FSM @Joao


        // USE THESE INPUT SIMULATION METHODS TO CONTROL THE BOT:
        //
        // SimulateHover(Vector2I?);
        // SimulateLeftClick(Vector2I);
        // SimulateRightClick(Vector2I);
        // SimulateHumanClick(Vector2I, bool, float, float)
        // SimulatePassTurn();

        // if (GetFriendlyMinions().Count <= 4) // Few minions? Spawn some first
        // {
        // 	List<Vector2I> spawnPositions = [];
        // 	Vector2I[] allCells = Board.Grid.GetAllCells();
        // 	int minionAmount = GD.RandRange(4, 10);

        // 	for (int i = 0; i < minionAmount; i++)
        // 		spawnPositions.Add(allCells.GetRandomElement());

        // 	foreach (Vector2I cell in spawnPositions)
        // 		await SimulateHumanClick(cell, true);
        // }

        // foreach (Minion minion in GetFriendlyMinions())
        // {
        //     await SimulateHumanClick(minion.Position);

        //     Vector2I[] minionRange = GridNavigation.GetReachableCells(minion);

        //     if (minionRange.IsEmpty()) continue;

        //     Vector2I randomCell = minionRange.GetRandomElement();

        //     await SimulateHumanClick(randomCell, false, 2);
        // }


        navigator.ClearWaypoints();
        SimulatePassTurn();
    }


    private List<Waypoint> GetWaypoints()
    {
        List<Waypoint> waypoints = [];

        if (GetFriendlyMinions().Count == 0)
            waypoints = navigator.GenerateDeployWaypoints();
        else
            foreach (Minion minion in GetFriendlyMinions())
                waypoints = navigator.GenerateWaypoints(minion);

        GD.Print($"Generated {waypoints.Count} waypoints for bot.");

        if (GetFriendlyMinions().Count != 0)
            foreach (Waypoint wp in waypoints)
            {
                GD.Print($"Waypoint: Type={wp.Type}, Cell={wp.Cell}, ElementAffinity={wp.ElementAffinity}, Priority={wp.Priority}");
                Board.State.AddWaypoint(wp);
            }

        return waypoints;
    }
    async Task SimulateGoingFort(List<Waypoint> waypoints, Minion minion)
	{
		if (minion == null) return;

		Vector2I[] reachable = GridNavigation.GetReachableCells(minion);
		List<Vector2I> minionRange = [.. reachable];
		if (!minionRange.Contains(minion.Position))
			minionRange.Add(minion.Position);

		if (minionRange.Count == 0)
			return;

		var goFortWaypoints = waypoints
			.Where(wp => wp.Type == WaypointType.Capture)
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
		await SimulateHumanClick(targetCell, false);
	}


    async Task DeployUnit(List<Waypoint> waypoints)
    {
        var deployWaypoints = waypoints
			.Where(wp => wp.Type == WaypointType.Deploy)
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
		await Wait(hoverTime);

		if (rightClick) SimulateRightClick(cell);
		else SimulateLeftClick(cell);

		await Wait(afterClickTime);
	}

	async Task Wait(float seconds) => await Task.Delay((int)Mathf.Round(seconds * 1000 / playSpeed));

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

		while (!InputHandler.InteractionEnabled)
			await Wait(0.1f); // Check each 0.1s if interaction is enabled

		await PlayTurn();
	}
}
