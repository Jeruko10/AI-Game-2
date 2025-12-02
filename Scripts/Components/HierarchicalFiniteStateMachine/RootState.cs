using Godot;

namespace Components;

/// <summary>
/// Root node for a Hierarchical Finite State Machine.
/// The root is itself a State, but with a top-level entry point.
/// </summary>
public partial class RootState : State
{
    /// <summary>Initial top-level state.</summary>
    [Export] public string InitialState;

    public override void _Ready()
    {
        RootMachine = this;

        base._Ready();

        if (string.IsNullOrEmpty(InitialState))
        {
            GD.PushError("InitialState is not defined.");
            return;
        }

        TransitionTo(InitialState);
    }

    public override void _Process(double delta)
    {
        Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        PhysicsUpdate(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        HandleInput(@event);
    }
}
