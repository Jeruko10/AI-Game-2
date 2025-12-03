using Components;
using Godot;
using Utility;

namespace Game;

public partial class MinionDisplay : Node2D
{
	[ExportSubgroup("Squash Animation")]
	[Export] public float AnimationSpeed { get; set; } = 0.7f;

	[ExportSubgroup("References")]
	[Export] public RootState RootState { get; private set; }
	[Export] public Sprite2D Sprite { get; private set; }
	[Export] Sprite2D colorOverlay;
	[Export] public SquashStretch2D SquashAnimator { get; private set; }
	[Export] public ColorOverlayModule FlashEffect { get; private set; }
	[Export] public OutlineModule OutlineModule { get; private set; }

	float modifier, elapsedTime;

    public override void _Ready()
	{
		SquashAnimator.SetSquashModulator(() => modifier);
    }

	public override void _Process(double delta)
	{
		colorOverlay.Texture = Sprite.Texture;
		colorOverlay.Transform = Sprite.Transform;

		elapsedTime += (float)delta;
		elapsedTime = Logic.LoopRange(elapsedTime, 0, 1000);
		modifier = Mathf.Sin(elapsedTime * Mathf.Pi * 2f * AnimationSpeed);
	}
}
