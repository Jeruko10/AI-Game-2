using System;
using Godot;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class TurnManager : Node
{
	[Signal] public delegate void TurnEndedEventHandler();
	
	bool IsPlayerTurn { get => turnIndex % 2 != 0; } // Odd turn inedex => Player's turn
	int turnIndex = 0;
	BoardDisplay boardDisplay;

    public override void _Ready()
	{
		PassTurn();
		boardDisplay = GetTree().Root.GetChildrenOfType<BoardDisplay>(true).First();
    }

	public void PassTurn()
	{
		turnIndex++;
		Board.State.IsPlayerTurn = IsPlayerTurn;
		EmitSignal(SignalName.TurnEnded);

		if (IsPlayerTurn)
		{
		}
		else
		{
			Board.State.SelectedMinion = null;
		}
	}
}
