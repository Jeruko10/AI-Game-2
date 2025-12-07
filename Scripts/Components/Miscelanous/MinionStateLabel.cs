using Game;
using Godot;
using System;

public partial class MinionStateLabel : Label
{
    public override void _Process(double delta)
    {
        Text = $"Actual IA State: {(Board.Player2 as BotInputProvider).minionState.GetType().Name}";
    }
}
