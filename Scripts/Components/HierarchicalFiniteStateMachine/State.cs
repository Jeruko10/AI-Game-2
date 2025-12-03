using Godot;
using System.Collections.Generic;

namespace Components;

/// <summary>
/// Hierarchical State (HFSM). Any State can contain child States,
/// automatically becoming a composite/parent state.
/// </summary>
[GlobalClass]
public partial class State : Node
{
    /// <summary>State identifier.</summary>
    [Export] public string StateName { get; private set; }

    /// <summary>Reference to the parent StateMachine (root).</summary>
    public RootState RootMachine { get; internal set; }

    /// <summary>Parent composite State (null if this is top-level).</summary>
    public State ParentState { get; internal set; }

    /// <summary>Currently active child state (only relevant if this has children).</summary>
    public State ActiveChild { get; private set; }

    private readonly List<State> childStates = [];

    public override void _Ready()
    {
        // Discover child states inside this node
        foreach (Node child in GetChildren())
        {
            if (child is State subState)
            {
                subState.ParentState = this;
                subState.RootMachine = RootMachine;
                childStates.Add(subState);
            }
        }
    }

    /// <summary>Called when this state becomes active.</summary>
    public virtual void Enter() { }

    /// <summary>Called before this state stops being active.</summary>
    public virtual void Exit() { }

    /// <summary>Get all cells the minion would click while being in this state.</summary>
    public virtual Vector2I[] GetStateStrategy() { return []; }

    /// <summary>Called every frame.</summary>
    public virtual void Update(double delta) => ActiveChild?.Update(delta);

    /// <summary>Called every physics tick.</summary>
    public virtual void PhysicsUpdate(double delta) => ActiveChild?.PhysicsUpdate(delta);

    /// <summary>Called for input events.</summary>
    public virtual void HandleInput(InputEvent @event) => ActiveChild?.HandleInput(@event);

    /// <summary> Gets the deepest active child state in the hierarchy.</summary>
    public State GetActiveLeafState()
    {
        State state = this;

        while (state.ActiveChild != null)
            state = state.ActiveChild;

        return state;
    }

    /// <summary>
    /// Transitions to a child state by name. 
    /// If this State has no children, this does nothing.
    /// </summary>
    public void TransitionTo(string childName)
    {
        foreach (var child in childStates)
        {
            if (child.StateName == childName)
            {
                SwitchToChild(child);
                return;
            }
        }

        GD.PushError($"Child state '{childName}' not found under '{StateName}'.");
    }

    void SwitchToChild(State newChild)
    {
        if (ActiveChild != null)
        {
            ActiveChild.Exit();
            ActiveChild.ActiveChild = null;
        }

        ActiveChild = newChild;
        newChild.Enter();
    }
}
