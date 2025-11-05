using Godot;
using System;
using Utility;

namespace Game;

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
        if (!InteractionEnabled) return;

        Board.Players activePlayer = Board.State.GetActivePlayer();
        IInputProvider activeProvider = activePlayer == Board.Players.Player1 ? Board.Player1 : Board.Player2;

        ReadInputs(activeProvider);
    }

    static void ReadInputs(IInputProvider inputProvider)
    {
        Vector2I? leftClickedCell = inputProvider.GetLeftClickedCell();
        Vector2I? rightClickedCell = inputProvider.GetRightClickedCell();
        Vector2I? hoveredCell = inputProvider.GetHoveredCell();
        bool isPassTurnClicked = inputProvider.IsTurnPassPressed();

        if (leftClickedCell != null)
            OnCellLeftClicked(leftClickedCell.Value);

        if (rightClickedCell != null)
            OnCellRightClicked(rightClickedCell.Value);

        if (isPassTurnClicked)
            Board.State.PassTurn();

        OnHoverCell(hoveredCell);
    }

    static void OnHoverCell(Vector2I? cell) => Board.State.SelectedCell = cell;

    static void OnCellLeftClicked(Vector2I clickedCell)
    {
        var data = Board.State.GetCellData(clickedCell);
        Minion clickedMinion = data.Minion;
        Minion selectedMinion = Board.State.SelectedMinion;
        bool minionSelectable = clickedMinion != null && selectedMinion == null && clickedMinion.Owner == Board.State.GetActivePlayer();
        bool minionCanMove = clickedMinion != null && GridNavigation.GetReachableCells(clickedMinion).Length > 0;

        // Selecting a minion for movement
        if (minionSelectable && minionCanMove && !clickedMinion.Exhausted)
        {
            Board.State.SelectMinion(clickedMinion);
            return;
        }

        // Selecting a minion for attacking (cannot move)
        if (minionSelectable && !clickedMinion.Exhausted)
        {
            Board.State.SelectMinion(clickedMinion);
            Board.State.SetAttackMode(true);
        }

        // Handle interactions when a minion is already selected
        if (selectedMinion == null) return;

        if (clickedCell == selectedMinion.Position)
        {
            // Toggle attack mode or unselect
            if (Board.State.AttackMode && !minionCanMove) Board.State.UnselectMinion();
            else Board.State.SetAttackMode(!Board.State.AttackMode);

            return;
        }

        if (Board.State.AttackMode)
        {
            // Perform attack
            Vector2I attackDir = Board.Grid.GetCardinal(selectedMinion.Position, Board.State.SelectedCell.Value);
            Board.State.AttackWithMinion(selectedMinion, attackDir);
            return;
        }

        // Perform movement
        if (GridNavigation.IsReachableByMinion(selectedMinion, clickedCell))
        {
            Vector2I[] minionPath = GridNavigation.GetPathForMinion(selectedMinion, clickedCell);
            Board.State.MoveMinion(selectedMinion, minionPath);
        }
        else Board.State.UnselectMinion();
    }

    static void OnCellRightClicked(Vector2I clickedCell)
    {
        if (Board.State.SelectedMinion != null)
        {
            Board.State.UnselectMinion();
            return;
        }

        SpawnRandomMinion(clickedCell);
    }

    static void SpawnRandomMinion(Vector2I cell)
    {
        MinionData[] templates =
        {
            Minions.FireKnight,
            Minions.WaterKnight,
            Minions.PlantKnight
        };

        MinionData randomTemplate = templates.GetRandomElement();
        Mana availableMana = Board.State.GetActiveRivalMana();

        if (Board.State.GetCellData(cell).Minion == null &&
            randomTemplate.IsAffordable(availableMana))
        {
            Board.State.PlayMinion(randomTemplate, cell);
        }
    }
}
