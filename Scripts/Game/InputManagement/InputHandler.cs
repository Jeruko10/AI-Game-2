using Godot;
using System;
using Utility;

namespace Game;

// Singleton that handles all input reading and logic
[GlobalClass]
public partial class InputHandler : Node
{
    public static bool InteractionEnabled { get; set; }

    static InputHandler singleton;

    public override void _EnterTree() => singleton ??= this;

    public override void _ExitTree()
    {
        if (singleton == this) singleton = null;
    }

    public override void _Process(double delta)
    {
        Board.Players activePlayer = Board.State.GetActivePlayer();
        IInputProvider activeProvider = (activePlayer == Board.Players.Player1) ? Board.Player1 : Board.Player2;

        ReadInputs(activeProvider);
    }

    static void ReadInputs(IInputProvider inputProvider)
    {
        Vector2I? leftClickedCell = inputProvider.GetLeftClickedCell();
        Vector2I? rightClickedCell = inputProvider.GetRightClickedCell();
        Vector2I? hoveredCell = inputProvider.GetHoveredCell();
        bool isPassTurnClicked = inputProvider.IsTurnPassClicked();

        if (leftClickedCell != null) OnCellLeftClicked(leftClickedCell.Value);
        if (rightClickedCell != null) OnCellRightClicked(leftClickedCell.Value);
        if (hoveredCell != null) OnHoverCell(hoveredCell.Value);
        if (isPassTurnClicked) Board.State.PassTurn();
    }

    static void OnHoverCell(Vector2I cell) => Board.State.EmitSignal(BoardState.SignalName.CellHovered, cell);

    static void OnCellLeftClicked(Vector2I cell)
    {
        var data = Board.State.GetCellData(cell);
        Minion clickedMinion = data.Minion;

        bool minionIsAvailable = clickedMinion != null && Board.State.SelectedMinion == null &&
            clickedMinion.Owner == Board.State.GetActivePlayer() &&
            GridNavigation.GetReachableCells(clickedMinion).Length > 0;

        if (minionIsAvailable)
        {
            Board.State.SelectMinion(clickedMinion);
            return;
        }

        // If minion was not clicked, then a tile must have been clicked
        if (Board.State.SelectedMinion != null)
        {
            if (GridNavigation.IsReachableByMinion(Board.State.SelectedMinion, cell))
            {
                Vector2I[] minionPath = GridNavigation.GetPathToCursor(Board.State.SelectedMinion);
                Board.State.MoveMinion(Board.State.SelectedMinion, minionPath);
            }
            else
                Board.State.UnselectMinion(); // Unselect minion
        }
    }

    static void OnCellRightClicked(Vector2I cell)
    {
        if (Board.State.SelectedMinion != null)
        {
            Board.State.UnselectMinion();
            return;
        }

        SpawnRandomMinion(cell);
    }

    static void SpawnRandomMinion(Vector2I cell) // This method is only for debugging
    {
        MinionData[] templates = [Minions.FireKnight, Minions.WaterKnight, Minions.PlantKnight];
        MinionData randomTemplate = templates.GetRandomElement();
        Mana availableMana = Board.State.GetActiveRivalMana();

        if (Board.State.GetCellData(cell).Minion == null && randomTemplate.IsAffordable(availableMana))
            Board.State.PlayMinion(randomTemplate, cell);
    }
}
