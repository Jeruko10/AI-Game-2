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

    bool isPlayer1Turn = false;

    public struct CellData(Tile tile, Minion minion, Fort fort)
    {
        public Tile Tile { get; private set; } = tile;
        public Minion Minion { get; private set; } = minion;
        public Fort Fort { get; private set; } = fort;
    }

    public override void _Ready()
    {
        Player1Mana = Player1StartingMana;
        Player2Mana = Player2StartingMana;

        PassTurn();
        GodotExtensions.CallDeferred(CreateBoard);
    }

    public Board.Players GetActivePlayer() => isPlayer1Turn ? Board.Players.Player1 : Board.Players.Player2;

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
    
    public void PlayMinion(MinionData minion, Vector2I cell)
    {
        Mana mana = GetActiveRivalMana();

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

        foreach (Vector2I pathCell in path[..^1]) // Skip last one
        {
            Tile tile = Tiles[pathCell];
            minion.MovePoints -= tile.MoveCost;
        }
		Vector2I pathEnd = (path.Length > 0) ? path[^1] : minion.Position;
        minion.Position = pathEnd;
        SelectedMinion = null;

        Fort fort = GetCellData(pathEnd).Fort;

        if (fort != null && fort.Element != minion.Element) DominateFort(fort, minion);
        if (Tiles[pathEnd].Damage > 0) DamageMinion(minion, Tiles[pathEnd].Damage);

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

    public Mana GetActiveRivalMana() => isPlayer1Turn ? Player1Mana : Player2Mana;

    void DominateFort(Fort fort, Minion minion)
    {
        fort.Element = minion.Element;
        fort.Owner = minion.Owner;
        FortDominated?.Invoke(fort, minion);
    }

    void HarvestMana(Fort fort)
    {
        Mana earned =
            fort.Element.Tag == Element.Type.Fire ? new Mana(1, 0, 0) :
            fort.Element.Tag == Element.Type.Water ? new Mana(0, 1, 0) :
            new Mana(0, 0, 1); // Plant mana

        GetActiveRivalMana().Obtain(earned);
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

    void CreateBoard() // TODO: Replace this method's content and create interesting way of designing the board
    {
        Vector2I[] fortPositions = [new(3, 3), new(13, 5)];

        foreach (Vector2I cell in Board.Grid.GetAllCells())
        {
            Tile tile;

            if (cell.Y < 7 || cell.X < 4) tile = Game.Tiles.Ground;
            else tile = Game.Tiles.Fire;

            AddTile(tile, cell);

            if (fortPositions.Contains(cell))
                AddFort(new(cell));
        }
    }
}
