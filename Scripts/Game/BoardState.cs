using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class BoardState : Node
{
    [Signal] public delegate void MinionAddedEventHandler(Minion minion);
    [Signal] public delegate void TileAddedEventHandler(Tile tile, Vector2I cell);
    [Signal] public delegate void FortAddedEventHandler(Fort fort);

    readonly Dictionary<Vector2I, Tile> tiles = [];
    readonly List<Minion> minions = [];
    readonly List<Fort> forts = [];

    public struct CellData(Tile tile, Minion minion, Fort fort)
    {
        public Tile Tile { get; private set; } = tile;
        public Minion Minion { get; private set; } = minion;
        public Fort Fort { get; private set; } = fort;
    }

    public override void _Ready()
    {
        GodotExtensions.CallDeferred(CreateBoard);
    }

    public CellData GetCellData(Vector2I cell)
    {
        Tile tile = tiles.GetValueOrDefault(cell);
        Minion minion = minions.FirstOrDefault(m => m.Position == cell);
        Fort fort = forts.FirstOrDefault(f => f.Position == cell);

        return new CellData(tile, minion, fort);
    }

    void CreateBoard() // TODO: Replace this method's content and create interesting way of designing the board
    {
        Vector2I[] fortPositions = [new(3, 3), new(13, 5)];

        foreach (Vector2I cell in Board.Grid.GetAllCells())
        {
            Tile tile;

            if (cell.Y < 7 || cell.X < 4) tile = Tiles.Ground;
            else tile = Tiles.Water;
            
            AddTile(tile, cell);

            if (fortPositions.Contains(cell))
                AddFort(new(cell));
        }
    }

    public void AddTile(Tile tile, Vector2I cell)
    {
        if (!Board.Grid.IsInsideGrid(cell))
        {
            GD.PushError("Trying to add a tile outside of grid boundaries.");
            return;
        }

        tiles.Add(cell, tile);
        EmitSignal(SignalName.TileAdded, tile, cell);
    }

    public Minion AddMinion(Minion minion)
    {
        if (!Board.Grid.IsInsideGrid(minion.Position))
        {
            GD.PushError("Trying to add a minion outside of grid boundaries.");
            return null;
        }

        minions.Add(minion);
        EmitSignal(SignalName.MinionAdded, minion);
        return minion;
    }
    
    public void AddFort(Fort fort)
    {
        if (!Board.Grid.IsInsideGrid(fort.Position))
        {
            GD.PushError("Trying to add a fort outside of grid boundaries.");
            return;
        }
        
        forts.Add(fort);
        EmitSignal(SignalName.FortAdded, fort);
    }
}
