using Game;
using Godot;
using System.Collections.Generic;
using Utility;

namespace Components;

[GlobalClass]
public partial class State : Node
{
    [Export] public string StateName { get; private set; }

    public State ParentState { get; internal set; }
    public State ActiveChild { get; private set; }

    readonly List<State> childStates = [];

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is State subState)
            {
                subState.ParentState = this;
                childStates.Add(subState);
            }
        }
    }

    public virtual void Enter() { }

    public virtual void Exit() { }

    public override void _Process(double delta) => ActiveChild?.Update(delta);

    public override void _PhysicsProcess(double delta) => ActiveChild?.PhysicsUpdate(delta);

    public override void _UnhandledInput(InputEvent e) => ActiveChild?.HandleInput(e);

    public virtual void Update(double delta) { }

    public virtual void PhysicsUpdate(double delta) { }

    public virtual void HandleInput(InputEvent e) { }

    /// <summary> Gets the root state of this state hierarchy.</summary>
    public State GetRootState()
    {
        State current = this;
        while (current.ParentState != null) current = current.ParentState;
        return current;
    }

    /// <summary>Gets the deepest active leaf state.</summary>
    public State GetDeepestActiveState()
    {
        State current = this;
        while (current.ActiveChild != null) current = current.ActiveChild;
        return current;
    }

    /// <summary>Transitions from this State to one of its direct child states.</summary>
    public void TransitionToChild(string childName)
    {
        foreach (State child in childStates)
        {
            if (child.StateName == childName)
            {
                SwitchToChild(child);
                return;
            }
        }

        GD.PushError($"Child state '{childName}' not found under '{StateName}'.");
    }

    /// <summary>Transitions to a sibling state (same parent).</summary>
    public void TransitionToSibling(string siblingName)
    {
        if (ParentState == null)
        {
            GD.PushError($"State '{StateName}' has no parent; cannot transition to siblings.");
            return;
        }

        foreach (State sibling in ParentState.childStates)
        {
            if (sibling.StateName == siblingName)
            {
                ParentState.SwitchToChild(sibling);
                return;
            }
        }

        GD.PushError($"Sibling '{siblingName}' not found under parent '{ParentState.StateName}'.");
    }

    /// <summary>Transitions from the current state to its parent state.</summary>
    public void TransitionToParent()
    {
        if (ParentState == null)
        {
            GD.PushWarning($"State '{StateName}' has no parent to transition to.");
            return;
        }

        DeactivateDescendants();
        ParentState.ActiveChild = null;
        ParentState.Enter();
    }

    void SwitchToChild(State target)
    {
        DeactivateDescendants();
        ActiveChild = target;
        target.Enter();
    }

    void DeactivateDescendants()
    {
        if (ActiveChild != null)
        {
            ActiveChild.DeactivateDescendants();
            ActiveChild.Exit();
        }

        ActiveChild = null;
    }
}
