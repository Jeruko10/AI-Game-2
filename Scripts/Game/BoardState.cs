using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class BoardState : Node
{
	[Export] Mana Player1StartingMana;
	[Export] Mana Player2StartingMana;

	public Dictionary<Vector2I, Tile> Tiles { get; private set; } = [];
	public List<Minion> Minions { get; private set; } = [];
	public List<Fort> Forts { get; private set; } = [];
	public Mana Player1Mana { get; set; }
	public Mana Player2Mana { get; set; }
	public Vector2I? SelectedCell { get; set; }
	public Minion SelectedMinion { get; private set; }
	public bool AttackMode { get; private set; }
	public MinionData SelectedDeployTroop { get; set; }
	public event Action<Board.Players> TurnStarted;
	public event Action<Minion> MinionDeath;
	public event Action<Minion, int> MinionDamaged;
	public event Action<Minion, Vector2I> MinionAttack;
	public event Action<Minion> MinionRestored;
	public event Action<Minion, Vector2I[]> MinionMoved;
	public event Action<Minion> MinionAdded;
	public event Action<Minion> MinionUnselected;
	public event Action<Minion> MinionSelected;
	public event Action<bool> AttackModeToggled;
	public event Action<Fort, Minion> FortDominated;
	public event Action<Fort> FortHarvested;
	public event Action<Tile, Vector2I> TileAdded;
	public event Action<Fort> FortAdded;
	public event Action<Waypoint> WaypointAdded;
	public event Action<Waypoint> WaypointRemoved;

	public InfluenceMapManager influence;

	bool isPlayer1Turn = false;

	public struct CellData(Tile tile, Minion minion, Fort fort)
	{
		public Tile Tile { get; private set; } = tile;
		public Minion Minion { get; private set; } = minion;
		public Fort Fort { get; private set; } = fort;
	}
	public override void _Ready()
	{
		influence = GetNode<InfluenceMapManager>("../../InfluenceMapManager");

		Player1Mana = Player1StartingMana;
		Player2Mana = Player2StartingMana;

		PassTurn();
		GodotExtensions.CallDeferred(CreateBoard);
	}
	

	public Board.Players GetActivePlayer() => isPlayer1Turn ? Board.Players.Player1 : Board.Players.Player2;

	public Minion[] GetPlayerMinions(Board.Players player) => [.. Minions.Where(m => m.Owner == player)];
	public Fort[] GetPlayerForts(Board.Players player) => [.. Forts.Where(f => f.Owner == player)];

	public void SelectMinion(Minion minion)
	{
		SelectedMinion = minion;
		MinionSelected?.Invoke(minion);
	}

	public void UnselectMinion()
	{
		Minion oldSelection = SelectedMinion;
		SelectedMinion = null;
		AttackMode = false;
		MinionUnselected?.Invoke(oldSelection);
	}

	public void SetAttackMode(bool value)
	{
		AttackMode = value;
		AttackModeToggled?.Invoke(value);
	}

	public void PassTurn()
	{
		isPlayer1Turn = !isPlayer1Turn;
		Board.Players oldTurnOwner = Board.GetRival(GetActivePlayer());
		Board.Players newTurnOwner = GetActivePlayer();
		InputHandler.InteractionEnabled = false;
		UnselectMinion();
		SelectedCell = null;

		foreach (Minion minion in Minions)
			if (minion.Owner == oldTurnOwner)
				RestoreMinion(minion);
		
		foreach (Fort fort in Forts)
			if (fort.Owner == oldTurnOwner)
				HarvestMana(fort);

		TurnStarted?.Invoke(newTurnOwner);
	}

	public CellData GetCellData(Vector2I cell)
	{
		Tile tile = Tiles.GetValueOrDefault(cell);
		Minion minion = Minions.FirstOrDefault(m => m.Position == cell);
		Fort fort = Forts.FirstOrDefault(f => f.Position == cell);

		return new CellData(tile, minion, fort);
	}

	public bool IsCellOccupied(Vector2I cell)
	{
		CellData data = GetCellData(cell);
		return data.Minion != null || (data.Tile != null && data.Tile.Obstructs);
	}

	public bool IsMinionInCell(Vector2I cell)
	{
		CellData data = GetCellData(cell);
		return data.Minion != null;
	}

	public Minion GetMinionAt(Vector2I cell)
	{
		CellData data = GetCellData(cell);
		return data.Minion;
	}
	
	public void PlayMinion(MinionData minion, Vector2I cell)
	{
		Mana mana = GetPlayerMana(GetActivePlayer());

		if (!minion.IsAffordable(mana))
		{
			GD.PushWarning("Trying to play a minion with insuficient mana. This should be avoided.");
			return;
		}

		mana.Spend(minion.Cost);
		Minion playedMinion = new(minion, cell);
		AddMinion(playedMinion);
	}

	public void MoveMinion(Minion minion, Vector2I[] path)
	{
		minion.Selectable = false;
        GD.Print(path);
		foreach (Vector2I pathCell in path[..^1]) // Skip last one
		{
			GD.Print($"Moving through cell: {Tiles[pathCell]}");
			Tile tile = Tiles[pathCell];
			minion.MovePoints -= (minion.Element.Tag == Element.Types.Water) ? 1 : tile.MoveCost;
		}
		Vector2I pathEnd = (path.Length > 0) ? path[^1] : minion.Position;
		minion.Position = pathEnd;
		SelectedMinion = null;

		Fort fort = GetCellData(pathEnd).Fort;

		if (fort != null && fort.Element != minion.Element) DominateFort(fort, minion);
		if (Tiles[pathEnd].Damage > 0 && minion.Element.Tag != Element.Types.Fire) DamageMinion(minion, Tiles[pathEnd].Damage);

		MinionMoved?.Invoke(minion, path);
	}

	public void AttackWithMinion(Minion minion, Vector2I direction)
	{
		Vector2I[] damageArea = GridNavigation.RotatedDamageArea(minion.DamageArea, direction);

		foreach (Vector2I cell in damageArea)
		{
			Minion victim = GetCellData(cell + minion.Position).Minion;
			if (victim != null) DamageMinion(victim, minion.Damage);
		}
		minion.Exhausted = true;
		Board.State.UnselectMinion();
		MinionAttack?.Invoke(minion, direction);
	}

	public void DamageMinion(Minion minion, int damage)
	{
		minion.Health -= damage;
		if (minion.Health <= 0) KillMinion(minion);
		MinionDamaged?.Invoke(minion, damage);
	}

	public void KillMinion(Minion minion)
	{
		Minions.Remove(minion);
		MinionDeath?.Invoke(minion);
	}

	public void RestoreMinion(Minion minion)
	{
		minion.Exhausted = false;
		minion.MovePoints = minion.MaxMovePoints;
		MinionRestored?.Invoke(minion);
	}

	public Mana GetPlayerMana(Board.Players player) => (player == Board.Players.Player1) ? Player1Mana : Player2Mana;

	void DominateFort(Fort fort, Minion minion)
	{
		fort.Element = minion.Element;
		fort.Owner = minion.Owner;
		FortDominated?.Invoke(fort, minion);
	}

	void HarvestMana(Fort fort)
	{
		Mana earned =
			fort.Element.Tag == Element.Types.Fire ? new Mana(1, 0, 0) :
			fort.Element.Tag == Element.Types.Water ? new Mana(0, 1, 0) :
			new Mana(0, 0, 1); // Plant mana

		GetPlayerMana(GetActivePlayer()).Obtain(earned);
		FortHarvested?.Invoke(fort);
	}

	void AddTile(Tile tile, Vector2I cell)
	{
		if (!Board.Grid.IsInsideGrid(cell))
		{
			GD.PushError("Trying to add a tile outside of grid boundaries.");
			return;
		}

		Tiles.Add(cell, tile);
		TileAdded?.Invoke(tile, cell);
	}

	void AddFort(Fort fort)
	{
		if (!Board.Grid.IsInsideGrid(fort.Position))
		{
			GD.PushError("Trying to add a fort outside of grid boundaries.");
			return;
		}

		Forts.Add(fort);
		FortAdded?.Invoke(fort);
	}

	// New method added for waypoints... this should be public?
	public void AddWaypoint(Waypoint waypoint)
	{
		if (!Board.Grid.IsInsideGrid(waypoint.Cell))
		{
			GD.PushError("Trying to add a waypoint outside of grid boundaries.");
			return;
		}

		WaypointAdded?.Invoke(waypoint);
	}

	public void ClearWaypoints()
	{
		WaypointRemoved?.Invoke(null);
	}

	public Element.Types GetPlayerDominantElement(Board.Players player)
	{
		var elementCount = new Dictionary<Element.Types, int>
		{
			{ Element.Types.Fire, 0 },
			{ Element.Types.Water, 0 },
			{ Element.Types.Plant, 0 }
		};

		foreach (var minion in GetPlayerMinions(player))
			elementCount[minion.Element.Tag]++;

		return elementCount.OrderByDescending(kv => kv.Value).First().Key;
	}

	void AddMinion(Minion minion)
	{
		if (!Board.Grid.IsInsideGrid(minion.Position))
		{
			GD.PushError("Trying to add a minion outside of grid boundaries.");
			return;
		}

		Minions.Add(minion);
		MinionAdded?.Invoke(minion);
	}

	public static bool IsCellDeployable(Vector2I cell)
    {
        if (!Board.Grid.IsInsideGrid(cell)) return false;
        var data = Board.State.GetCellData(cell);
        if (data.Tile == null) return false;
        if (data.Minion != null) return false;
        if (data.Fort != null && data.Fort.Owner != Board.Players.Player2) return false;
        var tile = Board.State.Tiles.GetValueOrDefault(cell);
        if (tile != null && tile.Obstructs) return false;
        return true;
    }

	void CreateBoard() // TODO: Replace this method's content and create interesting way of designing the board
	{
		int width = 17;
		int height = 9;

		Vector2I[] fortPositions =
		[
			new(2, 2),   
			new(14, 6)   
		];

		foreach (Vector2I cell in Board.Grid.GetAllCells())
		{
			Tile tile;

			if (cell.X == 0 || cell.X == width - 1 || cell.Y == 0 || cell.Y == height - 1)
			{
				tile = Game.Tiles.Wall;
			}
			// Río central vertical
			else if (cell.X == 8 || cell.X == 9)
			{
				tile = Game.Tiles.Water;
			}
			else if (cell.Y >= 5 && cell.X >= 11)
			{
				tile = Game.Tiles.Fire;
			}
			else
			{
				tile = Game.Tiles.Ground;
			}

			AddTile(tile, cell);

			if (fortPositions.Contains(cell))
				AddFort(new(cell));
		}


		influence.Initialize(this);
	}
}
