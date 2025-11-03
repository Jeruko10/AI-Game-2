using Godot;
using System;

namespace Game;

public partial class FortDisplay : Node2D
{
	[ExportSubgroup("References")]
	[Export] public Sprite2D Sprite { get; private set; }
	
	public override void _Ready()
	{
		
	}

	public override void _Process(double delta)
	{
	}
}
