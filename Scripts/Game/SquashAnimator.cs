using Components;
using Godot;
using Utility;

namespace Game;

[GlobalClass]
public partial class SquashAnimator : SquashStretch2D
{
	[Export] public float AnimationSpeed { get; set; } = 1f;

	float modifier, elapsedTime;

    public override void _Ready()
	{
		base._Ready();
		SetSquashModulator(() => modifier);
    }

	public override void _Process(double delta)
	{
		elapsedTime += (float)delta;
		elapsedTime = Logic.LoopRange(elapsedTime, 0, 1000);
		modifier = Mathf.Sin(elapsedTime * Mathf.Pi * 2f * AnimationSpeed);
		base._Process(delta);
	}
}
