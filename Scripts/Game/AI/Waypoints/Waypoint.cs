using Godot;

using Game;
public class Waypoint
{
    public enum Types
    {
        Attack,
        Capture,
        Move,
        Deploy
    }
    
    public Types Type { get; set; }
    public Element.Types ElementAffinity { get; set; }
    public Vector2I Cell { get; set; }
    public int Priority { get; set; } // priority is in terms of 10x
}
