using Godot;

namespace Game;

[GlobalClass]
public partial class MouseInputProvider : Node, IInputProvider
{
    public Vector2I? GetLeftClickedCell() => Input.IsActionJustPressed("leftClick") ? GetHoveredCell() : null;

    public Vector2I? GetRightClickedCell() => Input.IsActionJustPressed("rightClick") ? GetHoveredCell() : null;

    public Vector2I? GetHoveredCell() => Board.Grid.GetHoveredCell();

    public bool IsTurnPassPressed() => Input.IsActionJustPressed("passTurn");
}
