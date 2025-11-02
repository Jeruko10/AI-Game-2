using Godot;

namespace Game;

[GlobalClass]
public partial class Mana : Resource
{
    [Export(PropertyHint.Range, manaHint)] public int FireMana { get; private set; }
    [Export(PropertyHint.Range, manaHint)] public int WaterMana { get; private set; }
    [Export(PropertyHint.Range, manaHint)] public int PlantMana { get; private set; }

    const string manaHint = "0,0, or_greater, suffix:mana";
}
