using Components;
using Godot;
using System;

namespace Game;

public partial class FortDisplay : Node2D
{
	[ExportSubgroup("References")]
	[Export] public Sprite2D Sprite { get; private set; }
	[Export] Sprite2D outlineSprite;
	[Export] public GpuParticles2D Particle { get; private set; }
	[Export] public OutlineModule OutlineModule { get; private set; }
	[Export] public SquashStretch2D SquashAnimator { get; private set; }
	
	public override void _Process(double delta)
    {
    	outlineSprite.Texture = Sprite.Texture;
		outlineSprite.Transform = Sprite.Transform;
    }
}
