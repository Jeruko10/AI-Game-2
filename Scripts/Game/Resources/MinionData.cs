using System;
using Godot;
using Godot.Collections;

namespace Game;

[GlobalClass]
public partial class MinionData : Resource
{
    [Export] public string Name { get; private set; } = "Nameless";
    [Export] public Texture2D Texture { get; private set; }
    [Export] public Element Element { get; private set; }
    [Export(PropertyHint.Range, positiveHint)] public Mana Cost { get; private set; } = new();
    [Export(PropertyHint.Range, positiveHint)] public int Health { get; private set; } = 100;
    [Export(PropertyHint.Range, positiveHint)] public int Damage { get; private set; } = 50;
    [Export(PropertyHint.Range, positiveHint)] public int MovePoints { get; private set; } = 5;
    [Export] public Array<Vector2I> DamageArea { get; private set; } // Define this as if the minion was facing upwards

    const string positiveHint = "0, 1, or_greater, hide_slider";

    public bool IsAffordable(Mana availableMana) => availableMana - Cost >= Mana.Zero;
}
