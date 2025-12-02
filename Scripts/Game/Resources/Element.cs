using Godot;

namespace Game;

[GlobalClass]
public partial class Element : Resource
{
    public enum Types { Water, Fire, Plant, None }

    [Export] public Types Tag { get; private set; }
    [Export] public Texture2D Symbol { get; private set; }
    [Export(PropertyHint.ColorNoAlpha)] public Color Color { get; private set; } = Colors.White;

    public Types GetAdvantage()
    {
        return Tag switch
        {
            Types.Water => Types.Fire,
            Types.Fire  => Types.Plant,
            Types.Plant => Types.Water,
            _          => Types.None
        };
    }

    public Types GetDisadvantage()
    {
        return Tag switch
        {
            Types.Water => Types.Plant,
            Types.Fire  => Types.Water,
            Types.Plant => Types.Fire,
            _          => Types.None
        };
    }
}
