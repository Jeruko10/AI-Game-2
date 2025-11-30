using Godot;

namespace Game;

[GlobalClass]
public partial class Element : Resource
{
    public enum Type { Water, Fire, Plant, None }

    [Export] public Type Tag { get; private set; }
    [Export] public Texture2D Symbol { get; private set; }
    [Export(PropertyHint.ColorNoAlpha)] public Color Color { get; private set; } = Colors.White;

    public Type Beats()
    {
        return Tag switch
        {
            Type.Water => Type.Fire,
            Type.Fire  => Type.Plant,
            Type.Plant => Type.Water,
            _          => Type.None
        };
    }

    public Type LosesTo()
    {
        return Tag switch
        {
            Type.Water => Type.Plant,
            Type.Fire  => Type.Water,
            Type.Plant => Type.Fire,
            _          => Type.None
        };
    }
}
