using Godot;

namespace Game
{
    public partial class MouseInputProvider : Node, IInputProvider
    {
        public Vector2I? GetLeftClickedCell()
        {
            if (Input.IsActionJustPressed("leftClick"))
                return Board.Grid.WorldToGrid(GetViewport().GetMousePosition());
            return null;
        }

        public Vector2I? GetRightClickedCell()
        {
            if (Input.IsActionJustPressed("rightClick"))
                return Board.Grid.WorldToGrid(GetViewport().GetMousePosition());
            return null;
        }

        public Vector2I? GetHoveredCell() => Board.Grid.GetHoveredCell();

        public bool IsTurnPassClicked() => Input.IsActionJustPressed("passTurn");
    }
}
