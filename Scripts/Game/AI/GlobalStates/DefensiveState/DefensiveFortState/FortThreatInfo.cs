using System;
using Game;

public class FortThreatInfo
{
    public Fort Fort { get; set; }
    public bool DirectThreat { get; set; }
    public float EnemyPressure { get; set; }
    public int Samples { get; set; }
    public int FireCount { get; set; }
    public int WaterCount { get; set; }
    public int PlantCount { get; set; }

    public bool IsThreatened => DirectThreat || EnemyPressure > 0.5f * Samples;

    public Element.Types PredominantElement()
    {
        int maxCount = Math.Max(FireCount, Math.Max(WaterCount, PlantCount));
        if (maxCount == 0) return Element.Types.None;
        if (maxCount == FireCount) return Element.Types.Fire;
        if (maxCount == WaterCount) return Element.Types.Water;
        return Element.Types.Plant;
    }

    
}