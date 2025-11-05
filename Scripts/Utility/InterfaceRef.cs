using System;
using Game;
using Godot;
using Utility;

namespace Utility;

public abstract partial class InterfaceRef<T> : Resource where T : class
{
    [Export]
    NodePath interfaceReference;

    T Value;
    Node GetRefNode(NodePath path)
    {
        SceneTree tree = Engine.GetMainLoop() as SceneTree;
        Window root = tree.Root;
        Node node = root.GetNode(interfaceReference);
        return node;
    }


    public T Get()
    {
        if (Value != null)
        {
            return Value;
        }
        Node node = GetRefNode(interfaceReference);
        Value = node as T;
        return Value;
    }
}
