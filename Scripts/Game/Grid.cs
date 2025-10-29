using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class Grid : Node2D
{
    [Export] public int Rows { get; private set; } = 5;
    [Export] public int Columns { get; private set; } = 5;
    [Export] public float CellSize { get; set; } = 64f;

    [ExportGroup("Debugging")]
    [Export] private bool drawGrid = true;
    [Export] private Color gridColor = Colors.Gray;
    
    [Export(PropertyHint.Range, "0,0.001,or_greater,hide_slider")]
    private float lineWidth = 1f;
    [Export] private bool mouseHover = true;
    [Export] private Color hoverColor = new(1, 1, 1, 0.2f);

    [Signal] public delegate void CellLeftClickedEventHandler(Vector2I cell);
    [Signal] public delegate void CellRightClickedEventHandler(Vector2I cell);

    private Vector2I? hoveredCell;
    private readonly Dictionary<Vector2I, Color> coloredCells = [];
    private const bool antialiasing = true;

    public override void _Input(InputEvent @event)
    {
        Vector2I cell = WorldToGrid(GetGlobalMousePosition());
        bool insideGrid = IsInsideGrid(cell);

        if (mouseHover && @event is InputEventMouseMotion)
        {
            hoveredCell = insideGrid ? cell : null;
            QueueRedraw();
        }

        if (@event is InputEventMouseButton click && click.Pressed && insideGrid)
        {
            if (click.ButtonIndex == MouseButton.Left)
                EmitSignal(SignalName.CellLeftClicked, cell);
            else if (click.ButtonIndex == MouseButton.Right)
                EmitSignal(SignalName.CellRightClicked, cell);
        }
    }

    public override void _Draw()
    {
        if (drawGrid)
        {
            for (int y = 0; y <= Rows; y++)
                DrawLine(new Vector2(0, y * CellSize), new Vector2(Columns * CellSize, y * CellSize), gridColor, lineWidth, antialiasing);

            for (int x = 0; x <= Columns; x++)
                DrawLine(new Vector2(x * CellSize, 0), new Vector2(x * CellSize, Rows * CellSize), gridColor, lineWidth, antialiasing);
        }

        foreach (var kv in coloredCells)
            DrawCellOverlay(kv.Key, kv.Value);

        if (mouseHover && hoveredCell != null)
            DrawCellOverlay(hoveredCell.Value, hoverColor);
    }

    /// <summary> Resizes the grid to the specified number of rows and columns. </summary>
    public void ResizeGrid(int rows, int columns)
    {
        Rows = rows;
        Columns = columns;
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
        return cellPosition.Y * Columns + cellPosition.X;
    }

    /// <summary> Gets the cell position from a flat array index. </summary>
    public Vector2I GetCellPosition(int index)
    {
        if (index < 0 || index >= Rows * Columns)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of bounds for the grid.");
        return new Vector2I(index % Columns, index / Columns);
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
        return cell.X >= 0 && cell.Y >= 0 && cell.X < Columns && cell.Y < Rows;
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
        if (row < 0 || row >= Rows) return [];
        return Enumerable.Range(0, Columns).Select(x => new Vector2I(x, row)).ToArray();
    }

    /// <summary> Gets an array of cells in a specific column. </summary>
    public Vector2I[] GetColumn(int column)
    {
        if (column < 0 || column >= Columns) return [];
        return Enumerable.Range(0, Rows).Select(y => new Vector2I(column, y)).ToArray();
    }

    /// <summary> Returns an array of all cells in the grid. </summary>
    public Vector2I[] GetAllCells()
    {
        List<Vector2I> cells = [];

        for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Columns; x++)
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

    /// <summary> Returns the shortest path between two cells using BFS. </summary>
    public Vector2I[] GetShortestPathBFS(Vector2I start, Vector2I goal, HashSet<Vector2I> blockedCells, bool allowDiagonals = false)
    {
        if (start == goal) return [start];

        if (!IsInsideGrid(start) || !IsInsideGrid(goal) || blockedCells.Contains(start) || blockedCells.Contains(goal))
            return [];

        Queue<Vector2I> frontier = new();
        Dictionary<Vector2I, Vector2I> cameFrom = [];

        frontier.Enqueue(start);
        cameFrom[start] = start;

        Vector2I[] directions = allowDiagonals ?
            [new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(-1, -1), new(1, -1), new(-1, 1)] :
            [new(1, 0), new(-1, 0), new(0, 1), new(0, -1)];

        while (frontier.Count > 0)
        {
            Vector2I current = frontier.Dequeue();

            foreach (Vector2I dir in directions)
            {
                Vector2I next = current + dir;

                if (!IsInsideGrid(next) || blockedCells.Contains(next) || cameFrom.ContainsKey(next)) continue;

                frontier.Enqueue(next);
                cameFrom[next] = current;

                if (next == goal) break;
            }
        }

        if (!cameFrom.ContainsKey(goal)) return [];

        List<Vector2I> path = [];
        Vector2I step = goal;

        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }

        path.Add(start);
        path.Reverse();
        return path.ToArray();
    }

    /// <summary> Returns the shortest path between two cells using A*. </summary>
    public Vector2I[] GetShortestPathAStar(Vector2I start, Vector2I goal, HashSet<Vector2I> blockedCells, bool allowDiagonals = false)
    {
        // TODO
        return null;
    }

    /// <summary> Checks if a path exists between two cells, considering blocked cells. </summary>
    public bool IsReachable(Vector2I start, Vector2I goal, HashSet<Vector2I> blockedCells, bool allowDiagonals = false)
    {
        return GetShortestPathBFS(start, goal, blockedCells, allowDiagonals).Length > 0;
    }

    /// <summary> Colors a specific cell with the given color and alpha. </summary>
    public void ColorCell(Vector2I cell, Color color, float alpha = 0.3f)
    {
        coloredCells[cell] = new Color(color.R, color.G, color.B, alpha);
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

    /// <summary> Draws an overlay for a specific cell with the given color. </summary>
    private void DrawCellOverlay(Vector2I cell, Color color)
    {
        Vector2 pos = new(cell.X * CellSize, cell.Y * CellSize);
        DrawRect(new Rect2(pos, new Vector2(CellSize, CellSize)), color);
    }
}