using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class BoardState : Node
{
    [Signal] public delegate void MinionDeathEventHandler(Minion minion);
    [Signal] public delegate void MinionDamagedEventHandler(Minion minion, int damageReceived);
    [Signal] public delegate void MinionAttackEventHandler(Minion minion, Vector2I direction);
    [Signal] public delegate void MinionRestoredEventHandler(Minion minion);
    [Signal] public delegate void MinionMovedEventHandler(Minion minion, Godot.Collections.Array<Vector2I> path);
    [Signal] public delegate void MinionAddedEventHandler(Minion minion);
    [Signal] public delegate void FortDominatedEventHandler(Fort fort, Minion dominator);
    [Signal] public delegate void FortHarvestedEventHandler(Fort fort);
    [Signal] public delegate void TileAddedEventHandler(Tile tile, Vector2I cell);
    [Signal] public delegate void FortAddedEventHandler(Fort fort);
    [Export] Mana PlayerStartingMana;
    [Export] Mana EnemyStartingMana;

    public Dictionary<Vector2I, Tile> Tiles { get; private set; } = [];
    public List<Minion> Minions { get; private set; } = [];
    public List<Fort> Forts { get; private set; } = [];
    public Minion SelectedMinion { get; set; }
    public bool IsPlayerTurn { get; set; }
    public Mana PlayerMana { get; set; }
    public Mana OpponentMana { get; set; }

    public struct CellData(Tile tile, Minion minion, Fort fort)
    {
        public Tile Tile { get; private set; } = tile;
        public Minion Minion { get; private set; } = minion;
        public Fort Fort { get; private set; } = fort;
    }

    public override void _Ready()
    {
        PlayerMana = PlayerStartingMana;
        OpponentMana = EnemyStartingMana;

        GodotExtensions.CallDeferred(CreateBoard);
    }

    public CellData GetCellData(Vector2I cell)
    {
        Tile tile = Tiles.GetValueOrDefault(cell);
        Minion minion = Minions.FirstOrDefault(m => m.Position == cell);
        Fort fort = Forts.FirstOrDefault(f => f.Position == cell);

        return new CellData(tile, minion, fort);
    }

    public void DominateFort(Fort fort, Minion minion)
    {
        fort.Element = minion.Element;
        fort.Owner = minion.Owner;
        EmitSignal(SignalName.FortDominated, fort, minion);
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

    public void MoveMinion(Minion minion, Vector2I[] path, bool autoatack = true)
    {
        minion.Selectable = false;

        foreach (Vector2I pathCell in path[..^1]) // Skip last one
        {
            Tile tile = Tiles[pathCell];
            minion.MovePoints -= tile.MoveCost;
        }
		Vector2I pathEnd = (path.Length > 0) ? path[^1] : minion.Position;
        Vector2I pathPenultimate = (path.Length > 1) ? path[^2] : minion.Position;
        Vector2I attackDirection = pathEnd - pathPenultimate;

        minion.Position = pathEnd;
        SelectedMinion = null;

        Fort fort = GetCellData(pathEnd).Fort;

        if (fort != null && fort.Element != minion.Element) DominateFort(fort, minion);
        if (autoatack) AttackWithMinion(minion, attackDirection);
        if (Tiles[pathEnd].Damage > 0) DamageMinion(minion, Tiles[pathEnd].Damage);

        Godot.Collections.Array<Vector2I> arrayPath = [.. path];
        EmitSignal(SignalName.MinionMoved, minion, arrayPath);
    }

    public void AttackWithMinion(Minion minion, Vector2I direction)
    {
        Vector2I[] damageArea = GridNavigation.RotatedDamageArea(minion.DamageArea, direction);

        foreach (Vector2I cell in damageArea)
        {
            Minion victim = GetCellData(cell + minion.Position).Minion;
            if (victim != null) DamageMinion(victim, minion.Damage);
        }
        EmitSignal(SignalName.MinionAttack, minion, direction);
    }

    public void DamageMinion(Minion minion, int damage)
    {
        minion.Health -= damage;
        if (minion.Health <= 0) KillMinion(minion);
        EmitSignal(SignalName.MinionDamaged, minion, damage);
    }

    void KillMinion(Minion minion)
    {
        Minions.Remove(minion);
        EmitSignal(SignalName.MinionDeath, minion);
    }

    public void RestoreMinion(Minion minion)
    {
        minion.MovePoints = minion.MaxMovePoints;
        EmitSignal(SignalName.MinionRestored, minion);
    }

    public Mana GetActiveRivalMana() => IsPlayerTurn ? PlayerMana : OpponentMana;

    void OnTurnEnded(Board.Rivals turnOwner)
    {
        foreach (Fort fort in Forts)
            if (fort.Owner == turnOwner)
                HarvestMana(fort);

        foreach (Minion minion in Minions)
            if (minion.Owner == turnOwner)
                RestoreMinion(minion);
    }

    void HarvestMana(Fort fort)
    {
        Mana earned =
            fort.Element.Tag == Element.Type.Fire ? new Mana(1, 0, 0) :
            fort.Element.Tag == Element.Type.Water ? new Mana(0, 1, 0) :
            new Mana(0, 0, 1); // Plant mana

        GetActiveRivalMana().Obtain(earned);
        EmitSignal(SignalName.FortHarvested, fort);
    }

    void AddTile(Tile tile, Vector2I cell)
    {
        if (!Board.Grid.IsInsideGrid(cell))
        {
            GD.PushError("Trying to add a tile outside of grid boundaries.");
            return;
        }

        Tiles.Add(cell, tile);
        EmitSignal(SignalName.TileAdded, tile, cell);
    }

    void AddFort(Fort fort)
    {
        if (!Board.Grid.IsInsideGrid(fort.Position))
        {
            GD.PushError("Trying to add a fort outside of grid boundaries.");
            return;
        }

        Forts.Add(fort);
        EmitSignal(SignalName.FortAdded, fort);
    }

    void AddMinion(Minion minion)
    {
        if (!Board.Grid.IsInsideGrid(minion.Position))
        {
            GD.PushError("Trying to add a minion outside of grid boundaries.");
            return;
        }

        Minions.Add(minion);
        EmitSignal(SignalName.MinionAdded, minion);
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
