using System;
using System.Collections.Generic;
using Godot;

namespace Components;

/// <summary> Virtual base class for all states. DO NOT USE BY ITSELF, instead, extend this class on another script. </summary>
[GlobalClass]
public abstract partial class State : Node
{
    public enum States
    {
        Move,
        Crawl,
        Slide,
        Cinematic,
        Debug
	}

    [Export] public States StateName { get; private set; }
    public StateMachineModule StateMachine { get; set; }

    readonly List<Action> rollbackActions = [];

    public override void _Ready()
    {
        // Finite State Machine parent
        if (GetParent() is not StateMachineModule)
            GD.PushError("State's parent must be a StateMachineComponent.");
    }

    /// <summary>Saves and changes a value for potential rollback. The saved value will be restored
    /// automatically after <see cref="Exit"/> is called. Use this to track
    /// changes in state properties that may need to be reverted when leaving the state.</summary>
    /// <typeparam name="T">The type of the value being saved.</typeparam>
    /// <param name="setter">The delegate used to set the value when rolling back.</param>
    /// <param name="saveValue">The value to save for potential restoration.</param>
    /// <param name="newValue">The new value to asign.</param>
    public void TemporaryChange<T>(T saveValue, T newValue, Action<T> setter)
    {
        void rollback() => setter(saveValue);
        rollbackActions.Add(rollback);
        setter(newValue);
    }
    
    /// <summary>Restores all values previously saved with <see cref="TemporaryChange"/> and clears the saved list.
    /// This is called automatically when exiting the state, so manual calls are not required.</summary>
    public void RollbackData()
    {
        foreach (Action rollback in rollbackActions) rollback();
        rollbackActions.Clear();
    }

    /// <summary> Virtual function. Receives events from the "_unhandled_input()" callback. </summary>
    public virtual void HandleInput(InputEvent @event) { }

    /// <summary> Virtual function. Corresponds to the "_process()" callback. </summary>
    public virtual void Update(double delta) { }

    /// <summary> Virtual function. Corresponds to the "_physics_process()" callback. </summary>
    public virtual void PhysicsUpdate(double delta) { }

    /// <summary> Virtual function. Called by the state machine upon changing the active state. </summary>
    public virtual void Enter() { }

    /// <summary> Virtual function. Called by the state machine before changing the active state. Use this function to clean up the state and rollback backups. </summary>
    public virtual void Exit() => RollbackData();
}
