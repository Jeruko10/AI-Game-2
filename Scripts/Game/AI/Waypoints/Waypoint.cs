using Godot;
using System;
using System.Collections.Generic;

using Game;
public class Waypoint
{
    public WaypointType Type { get; set; }
    public Element.Type ElementAffinity { get; set; }
    public Vector2I Cell { get; set; }
    // priority is in terms of 10x
    public int Priority { get; set; }
}
