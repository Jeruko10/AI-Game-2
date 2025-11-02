using System;
using Godot;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class TurnManager : Node
{
	[Export(PropertyHint.ColorNoAlpha)] Color enemyTurnColorFilter = Colors.White;

	public int TurnIndex { get; private set; } = 1;
	public bool IsPlayerTurn { get => TurnIndex % 2 != 0; } // Odd turn inedex => Player's turn

	BoardDisplay boardDisplay;

    public override void _Ready()
    {
		boardDisplay = GetTree().Root.GetChildrenOfType<BoardDisplay>(true).First();
    }

	public void PassTurn()
	{
		TurnIndex++;
		if (IsPlayerTurn) boardDisplay.Modulate = Colors.White;
		else boardDisplay.Modulate = enemyTurnColorFilter;
	}
}
