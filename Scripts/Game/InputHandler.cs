using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class InputHandler : Node
{
    [Signal] public delegate void MinionSelectedEventHandler(Minion minion);
    
    BoardDisplay boardDisplay;

    public override void _Ready()
    {
        Board.Grid.CellLeftClicked += OnCellLeftClicked;
        Board.Grid.CellRightClicked += OnCellRightClicked;
        MinionSelected += OnMinionSelected;

        boardDisplay = GetTree().Root.GetChildrenOfType<BoardDisplay>(true).First();
    }

    public static Vector2I[] GetPathToCursor(Vector2I origin)
    {
        HashSet<Vector2I> blockedCells = [];
        Vector2I[] path = [];
        Vector2I? hoveredCell = Board.Grid.GetHoveredCell();

        if (hoveredCell == null) return path;

        path = Board.Grid.GetShortestPathBFS(origin, hoveredCell.Value, blockedCells);
        return path;
    }

    void OnCellLeftClicked(Vector2I cell)
    {
        var data = Board.State.GetCellData(cell);
        Minion clickedMinion = data.Minion;

        if (clickedMinion != null)
            OnMinionClicked(clickedMinion);
    }

    void OnCellRightClicked(Vector2I cell)
    {
        if (Board.State.SelectedMinion != null)
        {
            Board.State.SelectedMinion = null;
            return;
        }

        SpawnRandomMinion(cell);
    }

    void OnMinionClicked(Minion clickedMinion)
    {
        if (clickedMinion.Owner == Board.Entities.Player && Board.State.SelectedMinion == null && Board.State.IsPlayerTurn)
            EmitSignal(SignalName.MinionSelected, clickedMinion);
    }

    void OnMinionSelected(Minion selectedMinion)
    {
        Board.State.SelectedMinion = selectedMinion;
    }

    static void SpawnRandomMinion(Vector2I cell) // This method is only for debugging
    {
        MinionData[] templates = [Minions.FireKnight, Minions.WaterKnight, Minions.PlantKnight];
        MinionData randomTemplate = templates.GetRandomElement();

        if (Board.State.GetCellData(cell).Minion == null)
            Board.State.AddMinion(new(randomTemplate, cell, Board.Entities.Player));
    }
}
