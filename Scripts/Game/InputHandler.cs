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

    void OnCellLeftClicked(Vector2I cell)
    {
        var data = Board.State.GetCellData(cell);
        Minion clickedMinion = data.Minion;

        bool minionIsAvailable = clickedMinion != null && clickedMinion.MovePoints != 0 &&
            clickedMinion.Owner == Board.Rivals.Player && Board.State.SelectedMinion == null &&
            Board.State.IsPlayerTurn && GridNavigation.GetReachableCells(clickedMinion).Length > 0;

        if (minionIsAvailable)
        {
            EmitSignal(SignalName.MinionSelected, clickedMinion);
            return;
        }

        // If minion was not clicked, then a tile must have been clicked
        if (Board.State.SelectedMinion != null)
        {
            if (GridNavigation.IsReachableByMinion(Board.State.SelectedMinion, cell))
            {
                Vector2I[] minionPath = GridNavigation.GetPathToCursor(Board.State.SelectedMinion);
                
                Board.State.MoveMinion(Board.State.SelectedMinion, minionPath);
            }
            else
                Board.State.SelectedMinion = null; // Unselect minion
        }
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

    void OnMinionSelected(Minion selectedMinion)
    {
        Board.State.SelectedMinion = selectedMinion;
    }

    static void SpawnRandomMinion(Vector2I cell) // This method is only for debugging
    {
        MinionData[] templates = [Minions.FireKnight, Minions.WaterKnight, Minions.PlantKnight];
        MinionData randomTemplate = templates.GetRandomElement();
        Mana availableMana = Board.State.GetActiveRivalMana();

        if (Board.State.GetCellData(cell).Minion == null && randomTemplate.IsAffordable(availableMana))
            Board.State.PlayMinion(randomTemplate, cell);
    }
}
