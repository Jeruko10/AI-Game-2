using Components;
using Godot;
using System;

namespace Game;

public partial class WaypointDisplay : Node2D
{
	[ExportSubgroup("References")]
	[Export] public Sprite2D Sprite { get; private set; }
	[Export] public SquashStretch2D SquashAnimator { get; private set; }
	
}
