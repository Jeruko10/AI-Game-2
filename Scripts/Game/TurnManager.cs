using System;
using Godot;
using System.Linq;
using Utility;
using System.Threading.Tasks;

namespace Game;

[GlobalClass]
public partial class TurnManager : Node
{
	[Signal] public delegate void TurnEndedEventHandler(Board.Rivals lastTurnOwner);
	[Signal] public delegate void TurnStartedEventHandler(Board.Rivals newTurnOwner);
	[Export] float delayBetweenTurns = 1f;
	
	bool IsPlayerTurn => turnIndex % 2 != 0; // Odd turn inedex => Player's turn
	Board.Rivals TurnOwner => IsPlayerTurn ? Board.Rivals.Player : Board.Rivals.Opponent;
	int turnIndex = 1;

	public override void _Ready()
    {
        Board.State.IsPlayerTurn = IsPlayerTurn;
    }

    public override void _Process(double delta)
    {
		if (Input.IsActionJustPressed("passTurn")) PassTurn();
    }

	async public void PassTurn()
	{
		EmitSignal(SignalName.TurnEnded, (int)TurnOwner);

		await Task.Delay((int)(delayBetweenTurns * 1000));

		turnIndex++;
		Board.State.IsPlayerTurn = IsPlayerTurn;
		
		EmitSignal(SignalName.TurnStarted, (int)TurnOwner);
	}
}
