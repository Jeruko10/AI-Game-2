using Components;
using Godot;
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

	[ExportSubgroup("Animations")]
	[Export(PropertyHint.ColorNoAlpha)] Color enemyTurnColorFilter = Colors.Red;
	[Export] Color onSelectColor = Colors.White;
	[Export] Color movementPathColor = Colors.SkyBlue;
	[Export] Color damageZoneColor = Colors.Red;

	[ExportSubgroup("Textures")]
	[Export] float tileTextureScale = 1f;
	[Export] float minionTextureScale = 1f;
	[Export] float fortTextureScale = 1f;

	readonly Dictionary<Vector2I, TileDisplay> tileVisuals = [];
    readonly Dictionary<Minion, MinionDisplay> minionVisuals = [];
    readonly Dictionary<Fort, FortDisplay> fortVisuals = [];
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
				tileVisuals[cell].Position = CellToPos(cell);
				tileVisuals[cell].Sprite.Texture = tile.Texture;
			}
			if (minion != null)
			{
				minionVisuals[minion].Position = CellToPos(minion.Position);
				minionVisuals[minion].Sprite.Texture = minion.Texture;
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

		// Color movement path
		Vector2I[] pathToCursor = [];
		Minion selectedMinion = Board.State.SelectedMinion;

		if (selectedMinion != null) pathToCursor = InputHandler.GetPathToCursor(selectedMinion.Position);

		foreach (Vector2I cell in pathToCursor)
			Board.Grid.ColorCell(cell, movementPathColor);

		// Color damage zone
		if (pathToCursor.Length <= 1) return;

		Vector2I pathEnd = pathToCursor[^1], pathPenultimate = pathToCursor[^2];
		Vector2I minionDir = pathEnd - pathPenultimate;
		float angle = Vector2.Up.AngleTo(minionDir);

		foreach (Vector2 cell in selectedMinion.DamageArea)
		{
			Vector2 rotatedCell = cell.Rotated(angle);
			Vector2I cellGlobal = pathEnd + (Vector2I)rotatedCell;
			Board.Grid.ColorCell(cellGlobal, damageZoneColor);
        }
	}

	void CreateTileVisual(Tile tile, Vector2I cell) => CreateVisual(cell, tileDisplayTscn, tilesGroup, tileVisuals, tileTextureScale, $"Tile {cell}");

	void CreateFortVisual(Fort fort) => CreateVisual(fort, fortDisplayTscn, fortsGroup, fortVisuals, fortTextureScale, "Fort");

	void CreateMinionVisual(Minion minion) => CreateVisual(minion, minionDisplayTscn, minionsGroup, minionVisuals, minionTextureScale, "Minion");

	Vector2 CellToPos(Vector2I cell) => Board.Grid.GridToWorld(cell) - GlobalPosition + Board.Grid.CellSize * Vector2.One / 2;

    static void AdjustSpriteToGrid(Sprite2D sprite)
	{
		Vector2 cellSize = Board.Grid.CellSize * Vector2.One;
		sprite.Centered = true;
		sprite.Scale = cellSize / sprite.Texture.GetSize();
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

	void OnMinionSelected(Minion minion)
	{
		MinionDisplay minionDisplay = minionVisuals[minion];

		minionDisplay.SquashAnimator.ApplyImpact(1f);
		ColorOverlayModule.Fade fade = new(1f, 0f, 0f, 0.5f);
		_ = minionDisplay.FlashEffect.PlayEffect(fade, onSelectColor);
	}
	
	void OnTurnEnded()
    {
		if (Board.State.IsPlayerTurn)
		{
			Modulate = Colors.White;
		}
		else
        {
			Modulate = enemyTurnColorFilter;
        }
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
}
