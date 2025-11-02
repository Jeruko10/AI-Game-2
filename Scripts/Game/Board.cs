using Components;
using Godot;
using System;

namespace Game;

// Board singleton
[GlobalClass]
public partial class Board : Node
{
	[Export] Grid2D gridReference;
	[Export] BoardState stateReference;

	public static Board Singleton { get; private set; }
	public static Grid2D Grid { get; private set; }
	public static BoardState State { get; private set; }

	public override void _EnterTree() => StoreStaticData();

	public override void _ExitTree()
	{
		if (Singleton == this) Singleton = null;
	}

	public override void _Ready()
	{

		AudioManager.SetOriginParent(Singleton);
		AudioManager.CreateGroup("music");
		AudioManager.CreateGroup("sounds");
	}

	void StoreStaticData()
	{
		Singleton ??= this;
		Grid ??= gridReference;
		State ??= stateReference;
	}
}
