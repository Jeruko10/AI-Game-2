using Godot;
using System;
using System.Collections.Generic;
using Utility;

namespace Game;

[GlobalClass]
public partial class BoardDisplay : Node2D
{
	[Export] Vector2 tileTextureScale = Vector2.One;

	[ExportSubgroup("Fort Sprites")]
	[Export] Texture2D emptyFortTexture;
	[Export] Texture2D fireFortTexture;
	[Export] Texture2D waterFortTexture;
	[Export] Texture2D plantFortTexture;

	readonly Dictionary<Vector2I, Sprite2D> tileSprites = [];
    readonly Dictionary<Minion, Sprite2D> minionSprites = [];
    readonly Dictionary<Fort, Sprite2D> fortSprites = [];
	Node2D tilesGroup, fortsGroup, minionsGroup;

	public override void _Ready()
	{
		CreateChildGroups();

		Board.State.MinionAdded += OnMinionAdded;
		Board.State.FortAdded += OnFortAdded;
		Board.State.TileAdded += OnTileAdded;

		Board.Grid.GlobalPosition = GlobalPosition;
	}

    public override void _Process(double delta)
    {
		UpdateSprites();
    }

    void OnTileAdded(Tile tile, Vector2I cell)
    {
		Sprite2D tileSprite = CreateSprite(cell, tile.Texture, "Tile");
		tileSprites.Add(cell, tileSprite);
		tilesGroup.AddChild(tileSprite);
    }

    void OnFortAdded(Fort fort)
    {
		Sprite2D fortSprite = CreateSprite(fort.Position, emptyFortTexture, "Fort");
		fortSprites.Add(fort, fortSprite);
		fortsGroup.AddChild(fortSprite);
    }

    void OnMinionAdded(Minion minion)
    {
		Sprite2D minionSprite = CreateSprite(minion.Position, minion.Texture, "Minion", false);
		minionSprites.Add(minion, minionSprite);
		minionsGroup.AddChild(minionSprite);
    }

	void UpdateSprites()
	{
		foreach (Vector2I cell in Board.Grid.GetAllCells())
        {
			var data = Board.State.GetCellData(cell);

			if (data.Tile != null)
			{
				Sprite2D tileSprite = tileSprites[cell];
				tileSprite.Texture = data.Tile.Texture;
			}
			if (data.Minion != null)
			{
				minionSprites[data.Minion].Position = CellToPos(data.Minion.Position);
			}
			if (data.Fort != null)
			{
				Sprite2D fortSprite = fortSprites[data.Fort];
				fortSprites[data.Fort].Position = CellToPos(data.Fort.Position);

				if (data.Fort.Element == MinionData.Elements.Fire) fortSprite.Texture = fireFortTexture;
				else if (data.Fort.Element == MinionData.Elements.Water) fortSprite.Texture = waterFortTexture;
				else if (data.Fort.Element == MinionData.Elements.Plant) fortSprite.Texture = plantFortTexture;
			}
        }
    }

	Sprite2D CreateSprite(Vector2I cell, Texture2D texture, string nodeName, bool showCords = true)
	{
		Vector2 cellSize = Board.Grid.CellSize * Vector2.One;
		string finalName = showCords ? $"{nodeName} {cell}" : nodeName;
		
		Sprite2D sprite = new()
		{
			Texture = texture,
			Centered = true,
			Scale = cellSize / texture.GetSize() * tileTextureScale,
			Position = CellToPos(cell),
			Name = finalName
		};

		return sprite;
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
