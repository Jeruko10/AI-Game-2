using Components;
using Godot;
using System;

namespace Game;

// Tiles singleton used to store templates of custom Tiles
[GlobalClass]
public partial class Tiles : Node
{
	[Export] Tile groundTile;
	[Export] Tile wallTile;
	[Export] Tile fireTile;
	[Export] Tile waterTile;

	public static Tile Ground { get; private set; }
	public static Tile Wall { get; private set; }
	public static Tile Fire { get; private set; }
	public static Tile Water { get; private set; }
    static Tiles singleton;

	public override void _EnterTree() => StoreStaticData();

    public override void _ExitTree()
    {
        if (singleton == this) singleton = null;
    }
    
    void StoreStaticData()
    {
        singleton ??= this;
		Ground ??= groundTile;
		Wall ??= wallTile;
		Fire ??= fireTile;
		Water ??= waterTile;
	}
}
