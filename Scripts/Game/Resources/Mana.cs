using System;
using Godot;

namespace Game;

[GlobalClass]
public partial class Mana : Resource
{
    [Export(PropertyHint.Range, manaHint)] public int FireMana { get; private set; }
    [Export(PropertyHint.Range, manaHint)] public int WaterMana { get; private set; }
    [Export(PropertyHint.Range, manaHint)] public int PlantMana { get; private set; }
    public static Mana Zero => new(0, 0, 0);
    public static Mana One => new(1, 1, 1);

    private const string manaHint = "0,0,or_greater,suffix:mana";

    public Mana() { }

    public Mana(int fire, int water, int plant)
    {
        FireMana = fire;
        WaterMana = water;
        PlantMana = plant;
    }

    public void Spend(Mana cost)
    {
        FireMana = Math.Max(0, FireMana - cost.FireMana);
        WaterMana = Math.Max(0, WaterMana - cost.WaterMana);
        PlantMana = Math.Max(0, PlantMana - cost.PlantMana);
    }

    public void Obtain(Mana cost)
    {
        FireMana += cost.FireMana;
        WaterMana += cost.WaterMana;
        PlantMana += cost.PlantMana;
    }

    public static Mana operator +(Mana a, Mana b)
    {
        return new Mana(
            a.FireMana + b.FireMana,
            a.WaterMana + b.WaterMana,
            a.PlantMana + b.PlantMana
        );
    }

    public static Mana operator -(Mana a, Mana b)
    {
        return new Mana(
            a.FireMana - b.FireMana,
            a.WaterMana - b.WaterMana,
            a.PlantMana - b.PlantMana
        );
    }

    public static Mana operator *(Mana a, int scalar)
    {
        return new Mana(
            a.FireMana * scalar,
            a.WaterMana * scalar,
            a.PlantMana * scalar
        );
    }

    public static Mana operator /(Mana a, int divisor)
    {
        return new Mana(
            a.FireMana / divisor,
            a.WaterMana / divisor,
            a.PlantMana / divisor
        );
    }

    public static bool operator >=(Mana a, Mana b)
    {
        return a.FireMana >= b.FireMana &&
               a.WaterMana >= b.WaterMana &&
               a.PlantMana >= b.PlantMana;
    }

    public static bool operator <=(Mana a, Mana b)
    {
        return a.FireMana <= b.FireMana &&
               a.WaterMana <= b.WaterMana &&
               a.PlantMana <= b.PlantMana;
    }

    public static bool operator ==(Mana a, Mana b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        return a.FireMana == b.FireMana &&
               a.WaterMana == b.WaterMana &&
               a.PlantMana == b.PlantMana;
    }

    public static bool operator !=(Mana a, Mana b) => !(a == b);

    public override bool Equals(object obj) => obj is Mana m && this == m;

    public override int GetHashCode() => HashCode.Combine(FireMana, WaterMana, PlantMana);

    public override string ToString() => $"Fire: {FireMana}, Water: {WaterMana}, Plant: {PlantMana}";

    public Mana Clone() => new(FireMana, WaterMana, PlantMana);
}
