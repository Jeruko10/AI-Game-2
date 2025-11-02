using System;
using Godot;
using Godot.Collections;

namespace Game;

[GlobalClass]
public partial class MinionData : Resource
{
    public enum Elements { Water, Fire, Plant }

    [Export] public Texture2D Texture { get; private set; }
    [Export] public int Health { get; private set; } = 100;
    [Export] public int Damage { get; private set; } = 50;
    [Export] public int MovePoints { get; private set; } = 5;
    [Export] public Array<Vector2I> DamageArea { get; private set; } // Define this as if the minion was facing upwards
    [Export] public Elements Element { get; private set; }
}
