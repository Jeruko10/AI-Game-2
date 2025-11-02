using Godot;
using System;

namespace Game;

public partial class Fort(Vector2I position) : Resource
{
    public Element Element { get; set; } = null;
    public Vector2I Position
    {
        get => pos;
        set
        {
            if (Board.Grid.IsInsideGrid(value)) pos = value;
            else GD.PushWarning("Trying to position fort outside of grid boundaries.");
        }
    }

    Vector2I pos = position;
}
