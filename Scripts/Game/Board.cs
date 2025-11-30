using Components;
using Godot;
using System;
using Utility;

namespace Game;

// Board singleton
[GlobalClass]
public partial class Board : Node
{
	public enum Players { Player1, Player2 };
	
	[Export] Grid2D gridReference;
	[Export] BoardState stateReference;
	[Export] IInputProviderRef player1;
	[Export] IInputProviderRef player2;

	static Board singleton;
	public static Grid2D Grid => singleton.gridReference;
	public static BoardState State => singleton.stateReference;
	public static IInputProvider Player1 => singleton.player1.Get();
	public static IInputProvider Player2 => singleton.player2.Get();


	public override void _EnterTree() => singleton ??= this;

	public override void _ExitTree()
	{
		if (singleton == this) singleton = null;
	}

	public override void _Ready()
	{
		DebugDraw2D.Config.TextDefaultSize = 25;

		AudioManager.SetOriginParent(singleton);
		AudioManager.CreateGroup("music");
		AudioManager.CreateGroup("sounds");

		AddChild(Player1 as Node2D);
		AddChild(Player2 as Node2D);
	}

	public static Players GetRival(Players player) => (player == Players.Player1) ? Players.Player2 : Players.Player1;
}
