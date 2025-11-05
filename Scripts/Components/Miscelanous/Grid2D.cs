using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Components;

[GlobalClass][Tool]
public partial class Grid2D : Node2D
{
    [Export] public Vector2I Size { get; private set; }
    [Export] public float CellSize  { get; private set; } = 64f;
    [Export] bool drawGrid = true;
    [Export] Color gridColor = Colors.Gray;
    [Export(PropertyHint.Range, "0,0.001,or_greater,hide_slider")] float lineWidth = 1f;

    readonly Dictionary<Vector2I, Color> coloredCells = [];
    const bool antialiasing = true;

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) QueueRedraw();
    }

    public override void _Draw()
    {
        if (drawGrid)
        {
            for (int y = 0; y <= Size.Y; y++)
                DrawLine(new Vector2(0, y * CellSize), new Vector2(Size.X * CellSize, y * CellSize), gridColor, lineWidth, antialiasing);

            for (int x = 0; x <= Size.X; x++)
                DrawLine(new Vector2(x * CellSize, 0), new Vector2(x * CellSize, Size.Y * CellSize), gridColor, lineWidth, antialiasing);
        }

        foreach (var kv in coloredCells)
            DrawCellOverlay(kv.Key, kv.Value);
    }

    /// <summary> Resizes the grid to the specified number of size.Y and size.X. </summary>
    public void SetSize(Vector2I size)
    {
        Size = size;
        QueueRedraw();
    }

    /// <summary> Transforms a world position to a grid cell. </summary>
    public Vector2I WorldToGrid(Vector2 position)
    {
        Vector2 local = ToLocal(position);

        return new Vector2I(
            Mathf.FloorToInt(local.X / CellSize),
            Mathf.FloorToInt(local.Y / CellSize)
        );
    }

    /// <summary> Transforms a grid cell to a world position. </summary>
    public Vector2 GridToWorld(Vector2I cell)
    {
        Vector2 local = new(cell.X * CellSize, cell.Y * CellSize);
        return ToGlobal(local);
    }

    /// <summary> Gets the index of a cell in a flat array representation of the grid. </summary>
    public int GetCellIndex(Vector2I cellPosition)
    {
        if (!IsInsideGrid(cellPosition))
            throw new ArgumentOutOfRangeException(nameof(cellPosition), "Cell position is out of bounds for the grid.");
        return cellPosition.Y * Size.X + cellPosition.X;
    }

    /// <summary> Gets the cell position from a flat array index. </summary>
    public Vector2I GetCellPosition(int index)
    {
        if (index < 0 || index >= Size.Y * Size.X)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of bounds for the grid.");
        return new Vector2I(index % Size.X, index / Size.X);
    }

    /// <summary> Calculates the distance between two grid cells. </summary>
    public int GetDistance(Vector2I cellA, Vector2I cellB, bool useChebyshevDistance = false)
    {
        int dx = Math.Abs(cellA.X - cellB.X);
        int dy = Math.Abs(cellA.Y - cellB.Y);

        return useChebyshevDistance ? Math.Max(dx, dy) : dx + dy;
    }

    /// <summary> Checks if a grid cell is within the grid bounds. </summary>
    public bool IsInsideGrid(Vector2I cell)
    {
        return cell.X >= 0 && cell.Y >= 0 && cell.X < Size.X && cell.Y < Size.Y;
    }

    /// <summary> Returns an array of adjacent cells to the specified cell. </summary> 
    public Vector2I[] GetAdjacents(Vector2I cell, bool includeDiagonals = false)
    {
        Vector2I[] directions = includeDiagonals ?
            [new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(-1, -1), new(1, -1), new(-1, 1)]
            :
            [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

        return directions.Select(dir => cell + dir).Where(IsInsideGrid).ToArray();
    }

    /// <summary> Returns an array of cells in a specific row. </summary>
    public Vector2I[] GetRow(int row)
    {
        if (row < 0 || row >= Size.Y) return [];
        return Enumerable.Range(0, Size.X).Select(x => new Vector2I(x, row)).ToArray();
    }

    /// <summary> Gets an array of cells in a specific column. </summary>
    public Vector2I[] GetColumn(int column)
    {
        if (column < 0 || column >= Size.X) return [];
        return Enumerable.Range(0, Size.Y).Select(y => new Vector2I(column, y)).ToArray();
    }

    /// <summary> Returns an array of all cells in the grid. </summary>
    public Vector2I[] GetAllCells()
    {
        List<Vector2I> cells = [];

        for (int y = 0; y < Size.Y; y++)
            for (int x = 0; x < Size.X; x++)
                cells.Add(new Vector2I(x, y));

        return cells.ToArray();
    }

    /// <summary> Returns an array of cells within a specified range from a center cell. </summary>
    public Vector2I[] GetCellsInRange(Vector2I center, int range, bool useChebyshevDistance = false)
    {
        List<Vector2I> result = [];

        for (int y = -range; y <= range; y++)
            for (int x = -range; x <= range; x++)
            {
                Vector2I offset = new(x, y);
                Vector2I candidate = center + offset;

                if (!IsInsideGrid(candidate)) continue;

                int distance = useChebyshevDistance
                    ? Mathf.Max(Mathf.Abs(x), Mathf.Abs(y))  // Chebyshev distance
                    : Mathf.Abs(x) + Mathf.Abs(y);           // Manhattan distance

                if (distance <= range) result.Add(candidate);
            }

        return result.ToArray();
    }

    /// <summary> Colors a specific cell with the given color. </summary>
    public void ColorCell(Vector2I cell, Color color)
    {
        if (!IsInsideGrid(cell)) return;
        coloredCells[cell] = color;
        QueueRedraw();
    }

    /// <summary> Erases a specific cell from the grid. </summary>
    public void ClearCell(Vector2I cell)
    {
        coloredCells.Remove(cell);
        QueueRedraw();
    }

    /// <summary> Erases all colored cells in the grid. </summary>
    public void ClearAll()
    {
        coloredCells.Clear();
        QueueRedraw();
    }

    /// <summary> Returns the cell that is currently being hovered by the mouse, if there is one. </summary>
    public Vector2I? GetHoveredCell()
    {
        Vector2I? cell = null;
        Vector2I candidate = WorldToGrid(GetGlobalMousePosition());

        if (IsInsideGrid(candidate)) cell = candidate;

        return cell;
    }

    /// <summary>Returns the predominant cardinal direction from 'from' to 'to'.
    /// Only returns (1, 0), (-1, 0), (0, 1), or (0, -1).</summary>
    public Vector2I GetCardinal(Vector2I from, Vector2I to)
    {
        Vector2I delta = to - from;

        if (delta == Vector2I.Zero) return Vector2I.Zero;
        if (Math.Abs(delta.X) > Math.Abs(delta.Y)) return new Vector2I(Math.Sign(delta.X), 0);
        else return new Vector2I(0, Math.Sign(delta.Y));
    }

    /// <summary> Draws an overlay for a specific cell with the given color. </summary>
    void DrawCellOverlay(Vector2I cell, Color color)
    {
        Vector2 pos = new(cell.X * CellSize, cell.Y * CellSize);
        DrawRect(new Rect2(pos, new Vector2(CellSize, CellSize)), color);
    }
}