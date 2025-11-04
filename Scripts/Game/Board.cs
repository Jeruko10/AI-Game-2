using Components;
using Godot;
using System;

namespace Game;

// Board singleton
[GlobalClass]
public partial class Board : Node
{
	public enum Players { Player1, Player2 };
	
	[Export] Grid2D gridReference;
	[Export] BoardState stateReference;

	static Board singleton;
	public static Grid2D Grid => singleton.gridReference;
	public static BoardState State => singleton.stateReference;
	public static IInputProvider Player1 { get; private set; } = new MouseInputProvider();
	public static IInputProvider Player2 { get; private set; } = new MouseInputProvider();

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
	}
}
