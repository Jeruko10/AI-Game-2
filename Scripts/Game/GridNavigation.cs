using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game;

public static class GridNavigation
{
	public static Vector2I[] GetReachableCells(Minion minion)
	{
		if (minion == null || !Board.Grid.IsInsideGrid(minion.Position)) return [];

		HashSet<Vector2I> blockedCells = GetObstructorsForMinion(minion);

		// BFS queue: (cell, remainingPoints)
		Queue<(Vector2I Cell, int RemainingPoints)> frontier = new();
		frontier.Enqueue((minion.Position, minion.MovePoints));

		// best remaining points seen per cell
		Dictionary<Vector2I, int> visited = new() { [minion.Position] = minion.MovePoints };

		while (frontier.Count > 0)
		{
			var (current, remaining) = frontier.Dequeue();

			// cost to leave the current cell (use current tile)
			int leaveCost = Board.State.Tiles.TryGetValue(current, out Tile currentTile) ? currentTile.MoveCost : 1;

			foreach (Vector2I neighbor in Board.Grid.GetAdjacents(current))
			{
				// Can't enter blocked cells
				if (blockedCells.Contains(neighbor)) continue;
				if (!Board.Grid.IsInsideGrid(neighbor)) continue;

				int newRemaining = remaining - leaveCost;

				// Skip if out of move points
				if (newRemaining < 0) continue;

				if (!visited.TryGetValue(neighbor, out int best) || best < newRemaining)
				{
					visited[neighbor] = newRemaining;
					frontier.Enqueue((neighbor, newRemaining));
				}
			}
		}

		// Return all reachable cells except the starting one
		return visited.Keys.Where(c => c != minion.Position).ToArray();
	}

	public static bool IsReachableByMinion(Minion minion, Vector2I cell)
	{
		if (minion == null) return false;
		if (!Board.Grid.IsInsideGrid(cell)) return false;

		HashSet<Vector2I> blockedCells = GetObstructorsForMinion(minion);
		if (blockedCells.Contains(cell)) return false;

		return GetReachableCells(minion).Contains(cell);
	}

	public static Vector2I[] GetPathForMinion(Minion minion, Vector2I destination)
	{
		if (minion == null) return [];

		Vector2I start = minion.Position;
		if (start == destination) return [start];

		HashSet<Vector2I> blockedCells = GetObstructorsForMinion(minion);
		if (!Board.Grid.IsInsideGrid(destination) || blockedCells.Contains(destination)) return [];

		PriorityQueue<Vector2I, int> frontier = new();
		frontier.Enqueue(start, 0);

		Dictionary<Vector2I, Vector2I> cameFrom = new() { [start] = start };
		Dictionary<Vector2I, int> costSoFar = new() { [start] = 0 };

		while (frontier.Count > 0)
		{
			Vector2I current = frontier.Dequeue();

			if (current == destination) break;

			// cost to leave current cell
			int leaveCost = Board.State.Tiles.TryGetValue(current, out Tile currentTile) ? currentTile.MoveCost : 1;

			foreach (Vector2I neighbor in Board.Grid.GetAdjacents(current))
			{
				if (!Board.Grid.IsInsideGrid(neighbor)) continue;
				if (blockedCells.Contains(neighbor)) continue;

				int newCost = costSoFar[current] + leaveCost;

				// respect minion move points
				if (newCost > minion.MovePoints) continue;

				if (!costSoFar.TryGetValue(neighbor, out int best) || newCost < best)
				{
					costSoFar[neighbor] = newCost;
					int priority = newCost + Board.Grid.GetDistance(neighbor, destination); // heuristic
					frontier.Enqueue(neighbor, priority);
					cameFrom[neighbor] = current;
				}
			}
		}

		if (!cameFrom.ContainsKey(destination)) return [];

		// Reconstruct path including origin and destination
		List<Vector2I> path = [];
		Vector2I step = destination;

		while (true)
		{
			path.Add(step);
			if (step == start) break;
			step = cameFrom[step];
		}

		path.Reverse();
		return [.. path];
	}

	static HashSet<Vector2I> GetObstructorsForMinion(Minion minion)
	{
		HashSet<Vector2I> obstructedCells = [];

		foreach (Vector2I cell in Board.Grid.GetAllCells())
		{
			var data = Board.State.GetCellData(cell);

			if ((data.Tile != null && data.Tile.Obstructs) || data.Minion != null)
				obstructedCells.Add(cell);
		}

		return obstructedCells;
	}

	public static Vector2I[] RotatedDamageArea(Vector2I[] area, Vector2I direction)
	{
		if (area == null || area.Length == 0) return [];

		float angle = Vector2.Up.AngleTo(direction);

		List<Vector2I> rotatedArea = [];
		foreach (Vector2 cell in area)
		{
			Vector2 rotated = cell.Rotated(angle);
			rotatedArea.Add(new Vector2I(Mathf.RoundToInt(rotated.X), Mathf.RoundToInt(rotated.Y)));
		}

		return rotatedArea.ToArray();
	}
}
