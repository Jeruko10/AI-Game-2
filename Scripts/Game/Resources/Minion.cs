using System;
using System.Linq;
using Godot;

namespace Game;

public partial class Minion(MinionData data, Vector2I position, Board.Entities owner) : Resource
{
    public Texture2D Texture { get; set; } = data.Texture;
    public Board.Entities Owner { get; set; } = owner;
    public int MaxHealth { get; } = data.Health;
    public int Health { get; set; } = data.Health;
    public int MaxMovePoints { get; } = data.MovePoints;
    public int MovePoints { get; set; } = data.MovePoints;
    public Vector2I[] DamageArea { get; } = data.DamageArea.ToArray();
    public int Damage { get; } = data.Damage;
    public Element Element { get; } = data.Element;
    public Vector2I Position
    {
        get => pos;
        set
        {
            if (Board.Grid.IsInsideGrid(value)) pos = value;
            else GD.PushWarning("Trying to position minion outside of grid boundaries.");
        }
    }

    Vector2I pos = position;
}
