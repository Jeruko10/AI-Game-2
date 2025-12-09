using Components;
using Godot;
using System.Collections.Generic;

namespace Game;

/// Minion damages the best target close to him, no moving involved. When finished he may want to transition to AttackMove state.
public partial class PunchState : State, IMinionState
{
    public bool TryChangeState(Minion minion, List<Waypoint> waypoints)
    {
        BoardState boardState = Board.State;
        Grid2D grid = Board.Grid;

        bool enemyFound = false;

        foreach (var cell in grid.GetAdjacents(minion.Position, includeDiagonals: false))
        {
            var data = boardState.GetCellData(cell);
            if (data.Minion != null && data.Minion.Owner != minion.Owner)
            {
                enemyFound = true;
                break;
            }
        }

        if (!enemyFound)
        {
            TransitionToSibling("AttackMoveState");
            return true;
        }

        return false;
    }

    public Vector2I[] GetStrategy(Minion minion, List<Waypoint> waypoints)
    {
        List<Vector2I> clickedCells = [];
        Vector2I[] directions = [Vector2I.Up, Vector2I.Down, Vector2I.Right, Vector2I.Left];
        Vector2I bestDirection = Vector2I.Zero;
        int bestDamage = int.MinValue;

        foreach (Vector2I direction in directions)
        {
            int score = GetAttackScore(minion, direction);

            if (score > bestDamage)
            {
                bestDamage = score;
                bestDirection = direction;
            }
        }

        if (bestDirection != Vector2I.Zero)
        {
            clickedCells.Add(minion.Position); //Click the minion 2 times
            clickedCells.Add(minion.Position);

            clickedCells.Add(minion.Position + bestDirection); //Attack
        }

        return [.. clickedCells];
    }

    static int GetAttackScore(Minion minion, Vector2I direction)
    {
        int score = 0;
        Vector2I[] damageArea = GridNavigation.RotatedDamageArea(minion.DamageArea, direction);

        foreach (Vector2I cell in damageArea)
        {
            Vector2I worldCell = cell + minion.Position;

            if (Board.Grid.IsInsideGrid(worldCell))
            {
                var cellData = Board.State.GetCellData(worldCell);
                Minion victim = cellData.Minion;

                if (victim == null) continue;

                int sign = victim.Owner == Board.State.GetActivePlayer() ? -1 : 1; // If minion is friendly, points will be negative, otherwise positive
                int damage = Board.State.GetAttackDamage(minion, victim);

                score += damage * sign;
            }
        }

        return score;
    }
}
