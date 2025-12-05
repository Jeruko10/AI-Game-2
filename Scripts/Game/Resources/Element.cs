using System;
using Godot;

namespace Game;

[GlobalClass]
public partial class Element : Resource
{
    public enum Types { Water, Fire, Plant, None }

    [Export] public Types Tag { get; private set; }
    [Export] public Texture2D Symbol { get; private set; }
    [Export(PropertyHint.ColorNoAlpha)] public Color Color { get; private set; } = Colors.White;

    public static Types GetAdvantage(Types types)
    {
        return types switch
        {
            Types.Water => Types.Fire,
            Types.Fire  => Types.Plant,
            Types.Plant => Types.Water,
            _          => Types.None
        };
    }

    public static Types GetDisadvantage(Types types)
    {
        return types switch
        {
            Types.Water => Types.Plant,
            Types.Fire  => Types.Water,
            Types.Plant => Types.Fire,
            _          => Types.None
        };
    }
    public static Types GetTypeFromMostMana(Mana m)
    {
        int max = Math.Max(m.FireMana, Math.Max(m.WaterMana, m.PlantMana));
        if (max == 0) return Types.None;
        if (max == m.FireMana) return Types.Fire;
        if (max == m.WaterMana) return Types.Water;
        return Types.Plant;
    }
}
