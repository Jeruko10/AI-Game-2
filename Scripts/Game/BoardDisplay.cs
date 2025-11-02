using Godot;
using System.Collections.Generic;
using System.Linq;
using Utility;

namespace Game;

[GlobalClass]
public partial class BoardDisplay : Node2D
{
	[ExportSubgroup("Gizmos")]
	[Export] Color MovementPathColor { get; set; } = Colors.SkyBlue;

	[ExportSubgroup("Squash Animation")]
	[Export] public float SquashAnimationSpeed { get; set; } = 0.7f;
	[Export] public float SquashFactor { get; set; } = 0.2f;
	[Export] public float SquashImpactDecay { get; set; } = 25f;
	[Export] public float SpawnSquashImpact { get; set; } = 0.7f;
	[Export] public float OnSelectSquashImpact { get; set; } = 0.7f;

	[ExportSubgroup("Textures")]
	[Export] float tileTextureScale = 1f;
	[Export] float minionTextureScale = 1f;
	[Export] float fortTextureScale = 1f;
	[Export] Texture2D fortTexture;

	readonly Dictionary<Vector2I, Node2D> tileVisuals = [];
    readonly Dictionary<Minion, Node2D> minionVisuals = [];
    readonly Dictionary<Fort, Node2D> fortVisuals = [];
	Node2D tilesGroup, fortsGroup, minionsGroup;

	public override void _Ready()
	{
		CreateChildGroups();

		Board.State.MinionAdded += CreateMinionVisual;
		Board.State.FortAdded += CreateFortVisual;
		Board.State.TileAdded += CreateTileVisual;

		Board.Grid.GlobalPosition = GlobalPosition;
	}

	public override void _Process(double delta)
	{
		UpdateSprites();
		UpdateGizmos();
	}

	public void OnMinionSelected(Minion minion)
    {
		Node2D visual = minionVisuals[minion];
		SquashAnimator animator = visual.GetChildrenOfType<SquashAnimator>().First();

		animator.ApplyImpact(OnSelectSquashImpact);
    }
	
	void UpdateSprites()
	{
		foreach (Vector2I cell in Board.Grid.GetAllCells())
		{
			var cellData = Board.State.GetCellData(cell);
			Fort fort = cellData.Fort;
			Tile tile = cellData.Tile;
			Minion minion = cellData.Minion;

			if (tile != null)
			{
				if (tileVisuals[cell] is not Sprite2D tileSprite) return;
				tileSprite.Texture = tile.Texture;
			}
			if (minion != null)
			{
				minionVisuals[minion].Position = CellToPos(minion.Position);
			}
			if (fort != null)
			{
				fortVisuals[fort].Position = CellToPos(fort.Position);
				fortVisuals[fort].Modulate = (fort.Element == null) ? Colors.White : fort.Element.Color;
			}
		}
	}
	
	void UpdateGizmos()
	{
		Board.Grid.ClearAll();

		Vector2I[] pathToCursor = [];
		Minion selected = Board.State.SelectedMinion;
		
		if (selected != null) pathToCursor = InputHandler.GetPathToCursor(selected.Position);

		foreach (Vector2I cell in pathToCursor)
			Board.Grid.ColorCell(cell, MovementPathColor);
    }

    void CreateTileVisual(Tile tile, Vector2I cell)
    {
		Sprite2D tileSprite = CreateSprite(cell, tile.Texture, "Tile");
		tileSprite.Scale *= tileTextureScale;
		tileVisuals.Add(cell, tileSprite);
		tilesGroup.AddChild(tileSprite);
    }

    void CreateFortVisual(Fort fort)
    {
		Sprite2D fortSprite = CreateSprite(fort.Position, fortTexture, "Fort");
		fortSprite.Scale *= fortTextureScale;
		fortVisuals.Add(fort, fortSprite);
		fortsGroup.AddChild(fortSprite);
    }

    void CreateMinionVisual(Minion minion)
	{
		Sprite2D minionSprite = CreateSprite(minion.Position, minion.Texture, "MinionSprite", false);
		SquashAnimator minionAnimation = CreateSquashAnimation();
		Node2D minionVisual = new() { Name = $"Minion {minionVisuals.Count + 1}" };

		minionsGroup.AddChild(minionVisual);
		minionVisual.AddChild(minionAnimation);
		minionAnimation.AddChild(minionSprite);

		minionVisual.Scale *= minionTextureScale;
		minionVisual.Position = minionSprite.Position;
		minionSprite.Position = Vector2.Zero;
		minionVisuals.Add(minion, minionVisual);
		minionAnimation.ApplyImpact(SpawnSquashImpact);
    }

	Sprite2D CreateSprite(Vector2I cell, Texture2D texture, string nodeName, bool showCords = true)
	{
		Vector2 cellSize = Board.Grid.CellSize * Vector2.One;
		string finalName = showCords ? $"{nodeName} {cell}" : nodeName;

		Sprite2D sprite = new()
		{
			Texture = texture,
			Centered = true,
			Scale = cellSize / texture.GetSize(),
			Position = CellToPos(cell),
			Name = finalName
		};

		return sprite;
	}
	
	public SquashAnimator CreateSquashAnimation()
	{
		SquashAnimator animator = new()
		{
			ImpactFactor = 1f,
			MaxValueThreshold = 1.2f,
			MinValueThreshold = -1.2f,
			DeadZone = 0f,
			SquashFactor = SquashFactor,
			AnimationSpeed = SquashAnimationSpeed,
			ImpactDecay = SquashImpactDecay,
			Name = "Animator"
		};

		return animator;
	}

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

	Vector2 CellToPos(Vector2I cell) => Board.Grid.GridToWorld(cell) - GlobalPosition + Board.Grid.CellSize * Vector2.One / 2;
}
