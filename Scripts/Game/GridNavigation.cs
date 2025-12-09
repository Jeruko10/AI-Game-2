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
            int leaveCost;

            if (minion.Element.Tag == Element.Types.Water) leaveCost = 1; // Water element ignores terrain move costs
			else leaveCost = Board.State.Tiles.TryGetValue(current, out Tile currentTile) ? currentTile.MoveCost : 1;

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
		return [.. visited.Keys.Where(c => c != minion.Position)];
	}

	public static bool IsReachableByMinion(Minion minion, Vector2I cell)
	{
		if (minion == null) return false;
		if (!Board.Grid.IsInsideGrid(cell)) return false;

		HashSet<Vector2I> blockedCells = GetObstructorsForMinion(minion);
		if (blockedCells.Contains(cell)) return false;

		return GetReachableCells(minion).Contains(cell);
	}

	static Vector2I[] GetMinionAStar(Minion minion, Vector2I destination, bool ignoreMovePoints = false)
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

			// cost to leave the current cell (use current tile)
            int leaveCost;
			int realMovePoints = ignoreMovePoints ? int.MaxValue : minion.MovePoints;

            if (minion.Element.Tag == Element.Types.Water) leaveCost = 1; // Water element ignores terrain move costs
			else leaveCost = Board.State.Tiles.TryGetValue(current, out Tile currentTile) ? currentTile.MoveCost : 1;

			foreach (Vector2I neighbor in Board.Grid.GetAdjacents(current))
			{
				if (!Board.Grid.IsInsideGrid(neighbor)) continue;
				if (blockedCells.Contains(neighbor)) continue;

				int newCost = costSoFar[current] + leaveCost;

				// respect minion move points
				if (newCost > realMovePoints) continue;

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

	public static Vector2I[] GetPathForMinion(Minion minion, Vector2I destination)
    {
        if(IsReachableByMinion(minion, destination))
        {
            return GetMinionAStar(minion, destination);
		}

		Vector2I[] pathIgnoringMovePoints = GetMinionAStar(minion, destination, ignoreMovePoints: true);
		Vector2I[] reversedPath = [.. pathIgnoringMovePoints.Reverse()];

		foreach (var cell in reversedPath)
        {
            if(IsReachableByMinion(minion, cell))
			{
				return GetMinionAStar(minion, cell);
			}
        }
		return [];
    }

	public static HashSet<Vector2I> GetObstructorsForMinion(Minion minion)
	{
		HashSet<Vector2I> obstructedCells = [];

		foreach (Vector2I cell in Board.Grid.GetAllCells())
		{
			var data = Board.State.GetCellData(cell);
            bool tileExists = data.Tile != null;
            bool wallOnCell = tileExists && data.Tile.Obstructs && minion.Element.Tag != Element.Types.Plant;
            bool minionOnCell = data.Minion != null;

			if (wallOnCell || minionOnCell)
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

		return [.. rotatedArea];
	}

	public static Vector2I[] GetAllPossibleAttacks(Minion minion)
    {
        HashSet<Vector2I> cells = [];
        Vector2I[] directions = [Vector2I.Up, Vector2I.Down, Vector2I.Right, Vector2I.Left];

        foreach (Vector2I direction in directions)
        {
            Vector2I[] damageArea = RotatedDamageArea(minion.DamageArea, direction);

            foreach (Vector2I cell in damageArea)
                if (Board.Grid.IsInsideGrid(cell))
                    cells.Add(cell + minion.Position);
        }
        return [.. cells];
    }

	public static Minion[] GetCloseEnemyMinions(Minion minion)
	{
		List<Minion> enemies = [];
		Vector2I pos = minion.Position;
		int maxDistance = 2;

		for (int dx = -maxDistance; dx <= maxDistance; dx++)
		{
			for (int dy = -maxDistance; dy <= maxDistance; dy++)
			{
				if (dx == 0 && dy == 0) continue;

				Vector2I cell = new(pos.X + dx, pos.Y + dy);

				if (!Board.Grid.IsInsideGrid(cell)) continue;

				int manhattanDist = Math.Abs(dx) + Math.Abs(dy);
				if (manhattanDist > maxDistance || manhattanDist < 1) continue;

				var data = Board.State.GetCellData(cell);
				if (data.Minion != null && data.Minion.Owner != Board.State.GetActivePlayer())
				{
					enemies.Add(data.Minion);
				}
			}
		}

		return [.. enemies];
	}

	public static Minion GetTopLowHealthAlly()
	{
		var allies = Board.State.GetPlayerMinions(Board.State.GetActivePlayer());

		foreach (var ally in allies)
		{
			if (ally.Health >= ally.MaxHealth) continue;
			if(ally.Health <= ally.MaxHealth * 0.3f)
			{
				return ally;
			}
		}

		return allies[0];
	}
 
    public static Vector2I[] GetPunchStrategy(Minion minion)
    {
        List<Vector2I> clickedCells = [];

        Vector2I[] directions = [Vector2I.Up, Vector2I.Down, Vector2I.Right, Vector2I.Left];
        Vector2I bestDirection = Vector2I.Zero;
        int bestDamage = 0;

        foreach (Vector2I direction in directions)
        {
            int score = GetAttackScore(minion, direction);

            if (score > bestDamage)
            {
                bestDamage = score;
                bestDirection = direction;
            }
        }

        if (bestDirection != Vector2I.Zero)
        {
            clickedCells.Add(minion.Position); //Click the minion 2 times
            clickedCells.Add(minion.Position);

            clickedCells.Add(minion.Position + bestDirection); //Attack
        }

        return [.. clickedCells];
    }

    static int GetAttackScore(Minion minion, Vector2I direction)
    {
        int score = 0;
        Vector2I[] damageArea = RotatedDamageArea(minion.DamageArea, direction);

        foreach (Vector2I cell in damageArea)
        {
            Vector2I worldCell = cell + minion.Position;

            if (Board.Grid.IsInsideGrid(worldCell))
            {
                var cellData = Board.State.GetCellData(worldCell);
                Minion victim = cellData.Minion;

                if (victim == null) continue;

                int sign = victim.Owner == Board.State.GetActivePlayer() ? -1 : 1; // If minion is friendly, points will be negative, otherwise positive
                int damage = Board.State.GetAttackDamage(minion, victim);

                score += damage * sign;
            }
        }

        return score;
    }
}
