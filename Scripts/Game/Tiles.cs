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

	public static Tile Ground => singleton.groundTile;
	public static Tile Wall => singleton.wallTile;
	public static Tile Fire => singleton.fireTile;
	public static Tile Water => singleton.waterTile;
    static Tiles singleton;

	public override void _EnterTree() => singleton ??= this;

    public override void _ExitTree()
    {
        if (singleton == this) singleton = null;
    }
}
