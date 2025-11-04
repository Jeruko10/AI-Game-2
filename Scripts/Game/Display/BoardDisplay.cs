using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class BoardDisplay : Node2D
{
	[ExportSubgroup("Display Scenes")]
	[Export] PackedScene minionDisplayTscn;
	[Export] PackedScene fortDisplayTscn;
	[Export] PackedScene tileDisplayTscn;

	[ExportSubgroup("Mana Counters")]
	[Export] RichTextLabel fireManaCounter;
	[Export] RichTextLabel waterManaCounter;
	[Export] RichTextLabel plantManaCounter;

	[ExportSubgroup("Animations")]
	[Export] Control turnInformer;
	[Export] float turnInformerAnimationSpeed = 1f;
	[Export] float minionMoveAnimationSpeed = 1f;
	[Export] float minionDeathAnimationSpeed = 1f;
	[Export] ScreenFlashModule screenFlash;

	[ExportSubgroup("Colors")]
	[Export] float colorFilterIntensity = 0.3f;
	[Export(PropertyHint.ColorNoAlpha)] Color player1Color = Colors.SkyBlue;
	[Export(PropertyHint.ColorNoAlpha)] Color player2Color = Colors.Red;
	[Export(PropertyHint.ColorNoAlpha)] Color onMinionRestoredColor = Colors.Lime;
	[Export(PropertyHint.ColorNoAlpha)] Color onMinionSelectedColor = Colors.White;
	[Export(PropertyHint.ColorNoAlpha)] Color onMinionDamagedColor = Colors.Red;
	[Export] Color movementRangeColor = Colors.Purple;
	[Export] Color movementPathColor = Colors.Purple;
	[Export] Color damageZoneColor = Colors.Purple;

	[ExportSubgroup("Textures")]
	[Export] float tileTextureScale = 1f;
	[Export] float minionTextureScale = 1f;
	[Export] float fortTextureScale = 1f;

	readonly Dictionary<Vector2I, TileDisplay> tileVisuals = [];
    readonly Dictionary<Minion, MinionDisplay> minionVisuals = [];
    readonly Dictionary<Fort, FortDisplay> fortVisuals = [];
	Node2D tilesGroup, fortsGroup, minionsGroup;
	Vector2I? hoveredCell = null;

	public override void _Ready()
	{
		CreateChildGroups();

		Board.State.FortAdded += OnFortAdded;
		Board.State.FortDominated += OnFortDominated;
		Board.State.FortHarvested += OnFortHarvested;
		Board.State.TileAdded += OnTileAdded;
		Board.State.MinionAdded += OnMinionAdded;
		Board.State.MinionMoved += OnMinionMoved;
		Board.State.MinionRestored += OnMinionRestored;
		Board.State.MinionAttack += OnMinionAttack;
		Board.State.MinionDamaged += OnMinionDamaged;
		Board.State.MinionDeath += OnMinionDeath;
		Board.State.TurnStarted += OnTurnStarted;
		Board.State.CellHovered += OnCellHovered;

		Board.Grid.GlobalPosition = GlobalPosition;
	}

    public override void _Process(double delta)
	{
		UpdateGizmos();
		UpdateHUD();
	}
	
	void UpdateHUD()
    {
		fireManaCounter.Text = Board.State.Player1Mana.FireMana.ToString();
		waterManaCounter.Text = Board.State.Player1Mana.WaterMana.ToString();
		plantManaCounter.Text = Board.State.Player1Mana.PlantMana.ToString();
    }

	void UpdateGizmos()
	{
		Board.Grid.ClearAll();
		Minion selectedMinion = Board.State.SelectedMinion;

		if (selectedMinion == null || selectedMinion.MovePoints == 0) return;

		// Draw movement range
		Vector2I[] movementRange = GridNavigation.GetReachableCells(selectedMinion);

		foreach (Vector2I cell in movementRange)
			Board.Grid.ColorCell(cell, movementRangeColor);

		// Draw movement path
		Vector2I[] pathToCursor = GridNavigation.GetPathToCursor(selectedMinion);
		Vector2I pathEnd = (pathToCursor.Length > 0) ? pathToCursor[^1] : selectedMinion.Position;
		Vector2I pathPenultimate = (pathToCursor.Length > 1) ? pathToCursor[^2] : selectedMinion.Position;

		if (!movementRange.Contains(pathEnd)) return;

		foreach (Vector2I cell in pathToCursor)
			Board.Grid.ColorCell(cell, movementPathColor);

		// Draw damage zone
		Vector2I minionDir = pathEnd - pathPenultimate;
		Vector2I[] rotatedZone = GridNavigation.RotatedDamageArea(selectedMinion.DamageArea, minionDir);

		foreach (Vector2I offset in rotatedZone)
		{
			Vector2I cellGlobal = pathEnd + offset;
			Board.Grid.ColorCell(cellGlobal, damageZoneColor);
		}
	}

	void OnTileAdded(Tile tile, Vector2I cell)
	{
		CreateVisual(cell, tileDisplayTscn, tilesGroup, tileVisuals, tileTextureScale, $"Tile {cell}");
		
		tileVisuals[cell].Position = CellToWorld(cell);
		tileVisuals[cell].Sprite.Texture = tile.Texture;
	}

	void OnFortAdded(Fort fort)
	{
		CreateVisual(fort, fortDisplayTscn, fortsGroup, fortVisuals, fortTextureScale, "Fort");

		fortVisuals[fort].Position = CellToWorld(fort.Position);
		fortVisuals[fort].Modulate = (fort.Element == null) ? Colors.White : fort.Element.Color;
		fortVisuals[fort].OutlineModule.OutlineColor = GetPlayerColor(fort.Owner);
	}

	async void OnFortDominated(Fort fort, Minion dominator)
	{
		await ToSignal(dominator, Minion.SignalName.SelectionAvailable);

		fortVisuals[fort].Sprite.Modulate = dominator.Element.Color;
		fortVisuals[fort].OutlineModule.OutlineColor = GetPlayerColor(dominator.Owner);
		fortVisuals[fort].SquashAnimator.ApplyImpact(1f);
	}

	void OnFortHarvested(Fort fort)
	{
		GD.Print("daw");
		fortVisuals[fort].SquashAnimator.ApplyImpact(1f);
	}
	
    void OnCellHovered(Vector2I cell) => hoveredCell = cell;

	void OnMinionAdded(Minion minion)
	{
		CreateVisual(minion, minionDisplayTscn, minionsGroup, minionVisuals, minionTextureScale, "Minion");

		minionVisuals[minion].Position = CellToWorld(minion.Position);
		minionVisuals[minion].Sprite.Texture = minion.Texture;
		minionVisuals[minion].OutlineModule.OutlineColor = (minion.Owner == Board.Players.Player1) ? player1Color : player2Color;
	}

    async void OnMinionDeath(Minion minion)
	{
		if (!minion.Selectable) await ToSignal(minion, Minion.SignalName.SelectionAvailable);

		// Death animation
		MinionDisplay minionDisplay = minionVisuals[minion];
		Tween deathTween = CreateTween();
		float startRotation = minionDisplay.RotationDegrees;
		Vector2 startScale = minionDisplay.Scale;

		deathTween.TweenDelegate(v => minionDisplay.RotationDegrees = v, startRotation, 180f, 1 / minionDeathAnimationSpeed);
		deathTween.TweenDelegate<Vector2>(v => minionDisplay.Scale = v, startScale, Vector2.Zero, 1 / minionDeathAnimationSpeed);

		await ToSignal(deathTween, Tween.SignalName.Finished);
		minionVisuals[minion].QueueFree();
    }

    async void OnMinionDamaged(Minion minion, int damageReceived)
	{
		if (!minion.Selectable) await ToSignal(minion, Minion.SignalName.SelectionAvailable);

		// On damaged animation
		MinionDisplay minionDisplay = minionVisuals[minion];
		ColorOverlayModule.Fade fade = new(0.8f, 0f, 0f, 0.3f);

		await minionDisplay.FlashEffect.PlayEffect(fade, onMinionDamagedColor);
    }

    async void OnMinionAttack(Minion minion, Vector2I direction)
	{
        // Attack animation
    }

	async void OnMinionSelected(Minion minion)
	{
		// Select animation
		MinionDisplay minionDisplay = minionVisuals[minion];
		ColorOverlayModule.Fade fade = new(1f, 0f, 0f, 0.5f);

		minionDisplay.SquashAnimator.ApplyImpact(1f);
		await minionDisplay.FlashEffect.PlayEffect(fade, onMinionSelectedColor);
	}

	async void OnMinionRestored(Minion minion)
	{
		// Restore animation
		MinionDisplay minionDisplay = minionVisuals[minion];
		ColorOverlayModule.Fade fade = new(1f, 0.2f, 0.1f, 0.4f);

		minionDisplay.SquashAnimator.ApplyImpact(1f);
		await minionDisplay.FlashEffect.PlayEffect(fade, onMinionRestoredColor);
	}

	async void OnMinionMoved(Minion minion, Godot.Collections.Array<Vector2I> path)
	{
		// Moving animation
		Tween moveTween;

		foreach (Vector2I cell in path[1..]) // Skip first cell
		{
			MinionDisplay visual = minionVisuals[minion];
			Vector2 startingPos = visual.Position;

			moveTween = CreateTween();
			moveTween.TweenDelegate<Vector2>(v => visual.Position = v, startingPos, CellToWorld(cell), 1 / minionMoveAnimationSpeed);
			await ToSignal(moveTween, Tween.SignalName.Finished);
		}
		
		minion.Selectable = true;
	}

	async void OnTurnStarted(Board.Players turnOwner)
	{
		Tween bannerTween;

		Modulate = (turnOwner == Board.Players.Player1) ? player1Color.Lightened(1 - colorFilterIntensity) : player2Color.Lightened(1 - colorFilterIntensity);
		await screenFlash.PlayFlash(0f, 0f, 0.5f, 0.5f, GetPlayerColor(turnOwner));

		turnInformer.Show();
		RichTextLabel label = turnInformer.GetChildrenOfType<RichTextLabel>().First();

		label.Text = $"{turnOwner}'s Turn";
		bannerTween = CreateTween();
		bannerTween.TweenDelegate(v => turnInformer.Position = new(v, turnInformer.Position.Y), -2577f, 2557f, 1 / turnInformerAnimationSpeed)
		.SetEase(Tween.EaseType.OutIn).SetTrans(Tween.TransitionType.Elastic);

		await ToSignal(bannerTween, Tween.SignalName.Finished);

		InputHandler.InteractionEnabled = true;
	}

	Vector2 CellToWorld(Vector2I cell) => Board.Grid.GridToWorld(cell) - GlobalPosition + Board.Grid.CellSize * Vector2.One / 2;

	void CreateChildGroups()
	{
		tilesGroup = new();
		minionsGroup = new();
		fortsGroup = new();

		tilesGroup.Name = "Tiles";
		minionsGroup.Name = "Minions";
		fortsGroup.Name = "Forts";

		AddChild(tilesGroup);
		AddChild(fortsGroup);
		AddChild(minionsGroup);
	}

	static void CreateVisual<TData, TDisplay>(TData data, PackedScene prefab, Node group, Dictionary<TData, TDisplay> visualsDict, float scale, string baseName) where TDisplay : Node2D
	{
		TDisplay displayInst = prefab.Instantiate<TDisplay>(); // Instantiate the prefab and add it to the corresponding group
		group.AddChild(displayInst);

		visualsDict.Add(data, displayInst); // Register in the dictionary
		displayInst.Name = $"{baseName} {visualsDict.Count}";

		var sprites = displayInst.GetChildrenOfType<Sprite2D>(true);

		if (sprites.Any()) AdjustSpriteToGrid(sprites.First());
		displayInst.Scale *= scale;
	}

	static void AdjustSpriteToGrid(Sprite2D sprite)
	{
		Vector2 cellSize = Board.Grid.CellSize * Vector2.One;
		sprite.Centered = true;
		sprite.Scale = cellSize / sprite.Texture.GetSize();
	}
	
	Color GetPlayerColor(Board.Players? player)
    {
		if (player == null) return Colors.White;
		if (player == Board.Players.Player1) return player1Color;
		if (player == Board.Players.Player2) return player2Color;
		return Colors.Black;
    }
}
