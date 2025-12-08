using Components;
using Godot;
using System;

namespace Game;

// Minions singleton used to store templates of custom Minions
[GlobalClass]
public partial class Minions : Node
{
	[ExportSubgroup("Fire")]
	[Export] MinionData fireKnightDatalv1;
	[Export] MinionData fireKnightDatalv2;
	[Export] MinionData fireKnightDatalv3;

	[ExportSubgroup("Water")]
	[Export] MinionData waterKnightDatalv1;
	[Export] MinionData waterKnightDatalv2;
	[Export] MinionData waterKnightDatalv3;

	[ExportSubgroup("Plant")]
	[Export] MinionData plantKnightDatalv1;
	[Export] MinionData plantKnightDatalv2;
	[Export] MinionData plantKnightDatalv3;

	public static MinionData FireKnightLv1 => singleton.fireKnightDatalv1;
	public static MinionData WaterKnightLv1 => singleton.waterKnightDatalv1;
	public static MinionData PlantKnightLv1 => singleton.plantKnightDatalv1;

	public static MinionData FireKnightLv2 => singleton.fireKnightDatalv2;
	public static MinionData WaterKnightLv2 => singleton.waterKnightDatalv2;
	public static MinionData PlantKnightLv2 => singleton.plantKnightDatalv2;

	public static MinionData FireKnightLv3 => singleton.fireKnightDatalv3;
	public static MinionData WaterKnightLv3 => singleton.waterKnightDatalv3;
	public static MinionData PlantKnightLv3 => singleton.plantKnightDatalv3;

	public static MinionData[] AllMinionDatas =>
    [
        FireKnightLv1,
		WaterKnightLv1,
		PlantKnightLv1,
		FireKnightLv2,
		WaterKnightLv2,
		PlantKnightLv2,
		FireKnightLv3,
		WaterKnightLv3,
        PlantKnightLv3
	];

	static Minions singleton;

	public override void _EnterTree() => singleton ??= this;

    public override void _ExitTree()
    {
        if (singleton == this) singleton = null;
    }
}
