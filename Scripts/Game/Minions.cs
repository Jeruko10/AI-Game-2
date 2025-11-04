using Components;
using Godot;
using System;

namespace Game;

// Minions singleton used to store templates of custom Minions
[GlobalClass]
public partial class Minions : Node
{
	[ExportSubgroup("Fire")]
	[Export] MinionData fireKnightData;

	[ExportSubgroup("Water")]
	[Export] MinionData waterKnightData;

	[ExportSubgroup("Plant")]
	[Export] MinionData plantKnightData;

	public static MinionData FireKnight { get; private set; }
	public static MinionData WaterKnight { get; private set; }
	public static MinionData PlantKnight { get; private set; }
	static Minions singleton;

	public override void _EnterTree() => StoreStaticData();

    public override void _ExitTree()
    {
        if (singleton == this) singleton = null;
    }
    
    void StoreStaticData()
    {
        singleton ??= this;
		FireKnight ??= fireKnightData;
		WaterKnight ??= waterKnightData;
		PlantKnight ??= plantKnightData;
	}
}
