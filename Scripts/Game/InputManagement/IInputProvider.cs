using Godot;

namespace Game;

public interface IInputProvider
{
    Vector2I? GetLeftClickedCell();

    Vector2I? GetRightClickedCell();

    Vector2I? GetHoveredCell();

    bool IsTurnPassPressed();
}
