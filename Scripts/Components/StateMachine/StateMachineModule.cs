using System;
using System.Collections.Generic;
using Godot;

namespace Components;

/// <summary> Generic finite state machine. Handles State type childs so that only one of them is running at a time. </summary>
[GlobalClass]
public partial class StateMachineModule : Node
{
    /// <summary> State in which the State Machine begins. If no _initialState passed, default will be first _state. </summary>
    [Export] State initialState;

    readonly List<State> childrenStates = [];
    State currentState;

    public override async void _Ready()
    {
        await ToSignal(Owner, "ready");

        // Script will only run if there is at least one State 
        foreach (Node child in GetChildren())
        {
            if (child is State stateChild)
            {
                initialState ??= stateChild;
                stateChild.StateMachine = this;
                childrenStates.Add(stateChild);
            }
        }

        if (initialState is not null) // There is at least one State child
        {
            currentState = initialState;
            currentState.Enter();
        }
    }

    /// <summary> Transitions to a new state. </summary>
    public void Transition(State.States targetStateName)
    {
        State newState = GetState(targetStateName);

        if (newState == currentState)
        {
            GD.PushWarning($"Trying to transition into an already active state: {currentState.StateName}.");
            return;
        }

        currentState.Exit();
        currentState.RollbackData();
        currentState = newState;
        currentState.Enter();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        currentState?.HandleInput(@event);
    }

    public override void _Process(double delta)
    {
        currentState?.Update((float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        currentState?.PhysicsUpdate((float)delta);
    }

    public State.States GetCurrentStateIndex() => currentState.StateName;

    /// <summary> Returns the first children state found with the given name. </summary>
    State GetState(State.States stateName)
    {
        foreach (State state in childrenStates)
            if (state.StateName == stateName) return state;

        GD.PushError($"State {stateName} not found among this State Machine children.");
        return null;
    }
}
