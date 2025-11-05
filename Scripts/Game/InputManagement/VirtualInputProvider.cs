using Godot;
using System.Collections.Generic;

namespace Game;

public partial class VirtualInputProvider : Node, IInputProvider
{
    readonly Queue<Vector2I> queuedLeftClicks = new();
    readonly Queue<Vector2I> queuedRightClicks = new();
    bool passTurnNextFrame = false;
    Vector2I? hoveredCell = null;

    public Vector2I? GetLeftClickedCell()
    {
        if (queuedLeftClicks.Count == 0) return null;
        return queuedLeftClicks.Dequeue();
    }

    public Vector2I? GetRightClickedCell()
    {
        if (queuedRightClicks.Count == 0) return null;
        return queuedRightClicks.Dequeue();
    }

    public Vector2I? GetHoveredCell() => hoveredCell;

    public bool IsTurnPassPressed()
    {
        if (passTurnNextFrame)
        {
            passTurnNextFrame = false;
            return true;
        }
        return false;
    }

    public void SimulateLeftClick(Vector2I cell) => queuedLeftClicks.Enqueue(cell);
    public void SimulateRightClick(Vector2I cell) => queuedRightClicks.Enqueue(cell);
    public void SimulateHover(Vector2I? cell) => hoveredCell = cell;
    public void SimulatePassTurn() => passTurnNextFrame = true;
}
