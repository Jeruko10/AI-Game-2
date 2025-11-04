using Godot;

namespace Game;

[GlobalClass]
public partial class Element : Resource
{
    public enum Type { Water, Fire, Plant }
    [Export] public Type Tag { get; private set; }
    [Export(PropertyHint.ColorNoAlpha)] public Color Color { get; private set; } = Colors.White;
}
