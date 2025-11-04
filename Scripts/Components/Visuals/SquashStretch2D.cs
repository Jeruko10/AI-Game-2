using System;
using System.ComponentModel.DataAnnotations;
using Godot;
using Utility;

namespace Components;

[GlobalClass]
/// <summary>Applies squash and stretch scaling dynamically based on motion and impacts.</summary>
public partial class SquashStretch2D : Node2D
{
	/// <summary>Amount of deformation applied during stretching. Goes from 0 to 1.</summary>
	[Export(PropertyHint.Range, factorHint)] public float SquashFactor { get; set; } = 0.3f;

	/// <summary>Upper velocity threshold for maximum stretch.</summary>
	[Export] public float MaxValueThreshold { get; set; } = 10f;

	/// <summary>Lower velocity threshold for maximum stretch.</summary>
	[Export] public float MinValueThreshold { get; set; } = -10f;

	/// <summary>Range where movement is ignored for scaling.</summary>
	[Export(PropertyHint.Range, positveHint)] public float DeadZone { get; set; } = 5f;

	/// <summary>Strength of deformation caused by impacts. Goes from 0 to 1.</summary>
	[Export(PropertyHint.Range, factorHint)] public float ImpactFactor { get; set; } = 0.6f;

	/// <summary>Speed at which impact effects fade out.</summary>
	[Export(PropertyHint.Range, positveHint)] public float ImpactDecay { get; set; } = 10f;

	/// <summary>Whether scaling is active; resets scale when disabled.</summary>
	public bool Enabled
	{
		get => enabled;
		set
		{
			if (enabled == value) return;
			enabled = value;
			Scale = Vector2.One;
		}
	}

	const string positveHint = "0,1, or_greater, hide_slider", factorHint = "0,1";
	Func<float> GetModulator = () => 0f;
	float lastValue = 0f;
	bool enabled = true;
	Vector2 impactScaleFactor = Vector2.Zero, manualImpactWeight = Vector2.Zero;

	public override void _Process(double delta)
	{
		if (Enabled) UpdateDynamicScaling();
	}

	/// <summary>Assigns a function that provides the input value for scaling.</summary>
	public void SetSquashModulator(Func<float> modulatorGetter) => GetModulator = modulatorGetter;

	/// <summary>Applies an impact-based deformation on the X or Y axis.</summary>
	public void ApplyImpact(float weight, bool vertical = false)
	{
		float clamped = Mathf.Clamp(weight, 0f, 1f);

		if (vertical) manualImpactWeight.Y = clamped;
		else manualImpactWeight.X = clamped;
	}

	void UpdateDynamicScaling()
	{
		float value = GetModulator();
		float deltaVy = value - lastValue;
		lastValue = value;

		Vector2 impactWeight;

		if (manualImpactWeight == Vector2.Zero)
			impactWeight = new Vector2(Mathf.Abs(deltaVy) / MaxValueThreshold, 0f);
		else
		{
			impactWeight = manualImpactWeight;
			manualImpactWeight = Vector2.Zero;
		}

		bool newImpactOnX = impactWeight.X > 0.1f, newImpactOnY = impactWeight.Y > 0.1f;
		float decayWeight = Logic.ComputeLerpWeight(ImpactDecay, (float)GetProcessDeltaTime());

		if (newImpactOnX) impactScaleFactor.X = Mathf.Clamp(impactWeight.X, 0f, 1f);
		else impactScaleFactor.X = Mathf.Lerp(impactScaleFactor.X, 0f, decayWeight);

		if (newImpactOnY) impactScaleFactor.Y = Mathf.Clamp(impactWeight.Y, 0f, 1f);
		else impactScaleFactor.Y = Mathf.Lerp(impactScaleFactor.Y, 0f, decayWeight);
		
		ApplyScaling(CalculateScaleFactor(value));
	}

	float CalculateScaleFactor(float value)
	{
		if (Mathf.Abs(value) < DeadZone) return 0f;
		value = Mathf.Clamp(value, MinValueThreshold, MaxValueThreshold);

		if (value < 0)
			return Mathf.InverseLerp(-DeadZone, MinValueThreshold, value);
		else
			return Mathf.InverseLerp(DeadZone, MaxValueThreshold, value);
	}

	void ApplyScaling(float weight)
	{
		Vector2 maxStretchScale = new(1f - SquashFactor, 1f + SquashFactor);
		Vector2 baseScale = Vector2.One.Lerp(maxStretchScale, weight);

		Vector2 impactScaleHorizontal = new(
			1f + ImpactFactor * impactScaleFactor.X,
			1f - ImpactFactor * impactScaleFactor.X);

		Vector2 impactScaleVertical = new(
			1f - ImpactFactor * impactScaleFactor.Y,
			1f + ImpactFactor * impactScaleFactor.Y);

		Vector2 finalScale = baseScale * impactScaleHorizontal * impactScaleVertical;
		Scale = finalScale;
	}
}
