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

	public static MinionData FireKnight => singleton.fireKnightData;
	public static MinionData WaterKnight => singleton.waterKnightData;
	public static MinionData PlantKnight => singleton.plantKnightData;

	public static MinionData[] AllMinionDatas =>
    [
        FireKnight,
		WaterKnight,
		PlantKnight
	];

	static Minions singleton;

	public override void _EnterTree() => singleton ??= this;

    public override void _ExitTree()
    {
        if (singleton == this) singleton = null;
    }
}
