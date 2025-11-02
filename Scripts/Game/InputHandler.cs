using Godot;
using System;

namespace Game;

[GlobalClass]
public partial class InputHandler : Node
{
    public override void _Ready()
    {
      Board.Grid.CellLeftClicked += OnCellLeftClicked;
      Board.Grid.CellRightClicked += OnCellRightClicked;
    }

    void OnCellLeftClicked(Vector2I cell)
    {
    }

    void OnCellRightClicked(Vector2I cell)
  {
      // Debug only, this is temporal
      if (Board.State.GetCellData(cell).Minion == null)
        Board.State.AddMinion(new(Minions.FireKnight, cell));
    }
}
