using System;
using Godot;

namespace Game;

[GlobalClass]
public partial class Tile : Resource
{
    [Export] public Texture2D Texture { get; private set; }
    [Export] public bool Obstructs { get; private set; }
    [Export] public bool Destructible { get; private set; }
    [Export(PropertyHint.Range, positiveHint)] public int MoveCost { get; private set; }
    [Export(PropertyHint.Range, positiveHint)] public int Damage { get; private set; }

    const string positiveHint = "0, 1, or_greater, hide_slider";
}