using Game;
using Godot;
using System;

namespace Game;

public partial class ActualStateLabel : Label
{
    public override void _Process(double delta)
    {
        Text = $"Estado Global: {(Board.Player2 as BotInputProvider).RootState.GetDeepestActiveState().Name}";
    }
}
