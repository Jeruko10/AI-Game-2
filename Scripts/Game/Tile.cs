using System;
using Godot;

namespace Game;

[GlobalClass]
public partial class Tile : Resource
{
    [Export] public Texture2D Texture { get; private set; }
    [Export] public bool Obstructs { get; private set; }
    [Export] public bool Destructible { get; private set; }
    [Export] public int MoveCost { get; private set; }
    [Export] public int Damage { get; private set; }
}