using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Utility;

public static class GodotExtensions
{
    ///<summary>Defers the execution of the specified action until the current frame has finished processing.</summary>
    ///<param name="action">The <see cref="Action"/> to be invoked later using Godot's deferred call system.</param>
    public static void CallDeferred(Action action) => Callable.From(action).CallDeferred();

    /// <summary>
    /// Adds a new parent node above the specified <paramref name="root"/> node.
    /// Performs the reparenting deferred to avoid scene tree modification errors.
    /// </summary>
    /// <param name="root">The node that will receive the new parent.</param>
    /// <param name="newParent">The node to insert as the new parent above <paramref name="root"/>.</param>
    /// <param name="inheritTransform">
    /// If true, the transform of <paramref name="root"/> is transferred to <paramref name="newParent"/> 
    /// and <paramref name="root"/> transform is reset.
    /// </param>
    public static void AddParent(this Node root, Node newParent, bool inheritTransform = true)
    {
        Node oldParent = root.GetParent();

        if (oldParent == null)
        {
            GD.PushError("Can't AddParent to a node without previous parent.");
            return;
        }

        if (inheritTransform)
        {
            if (root is Node2D root2D && newParent is Node2D newParent2D)
            {
                newParent2D.Transform = root2D.Transform;
                root2D.Transform = Transform2D.Identity;
            }
            else if (root is Node3D root3D && newParent is Node3D newParent3D)
            {
                newParent3D.Transform = root3D.Transform;
                root3D.Transform = Transform3D.Identity;
            }
        }

        CallDeferred(() => oldParent.AddChild(newParent));
        CallDeferred(() => root.Reparent(newParent, false));
    }

    ///<summary>Returns all child nodes of a specific type from the given node.</summary>
    ///<typeparam name="T">The type of nodes to search for.</typeparam>
    ///<param name="node">The parent node whose children will be searched.</param>
    ///<param name="recursive">
    /// If true, the search includes all descendants recursively; 
    /// if false, only direct children are checked.
    ///</param>
    ///<returns>
    /// An enumerable collection of nodes of type <typeparamref name="T"/> found among the children (and optionally descendants) of the given node.
    ///</returns>
    public static IEnumerable<T> GetChildrenOfType<T>(this Node node, bool recursive = false)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is T match) yield return match;
            if (!recursive) continue;

            foreach (T descendant in child.GetChildrenOfType<T>(true))
                yield return descendant;
        }
    }

    /// <summary>Creates a method tween that calls the given setter delegate over time, interpolating between two Variant values.</summary>
    /// <typeparam name="T">The type of the value being interpolated.</typeparam>
    /// <param name="tween">The Tween instance this extension operates on.</param>
    /// <param name="setter">The delegate method to be called with the interpolated value each frame.</param>
    /// <param name="from">The initial value of the interpolation.</param>
    /// <param name="to">The final value of the interpolation.</param>
    /// <param name="duration">The total duration of the tween in seconds.</param>
    /// <returns>The created <see cref="MethodTweener"/> used to further configure the tween.</returns>
    public static MethodTweener TweenDelegate<T>(this Tween tween, Action<T> setter, Variant from, Variant to, float duration)
        => tween.TweenMethod(Callable.From(setter), from, to, duration);

    /// <summary>Creates a method tween that calls the given setter delegate over time, interpolating between two float values.</summary>
    /// <param name="tween">The Tween instance this extension operates on.</param>
    /// <param name="setter">The delegate method to be called with the interpolated value each frame.</param>
    /// <param name="from">The initial value of the interpolation.</param>
    /// <param name="to">The final value of the interpolation.</param>
    /// <param name="duration">The total duration of the tween in seconds.</param>
    /// <returns>The created <see cref="MethodTweener"/> used to further configure the tween.</returns>
    public static MethodTweener TweenDelegate(this Tween tween, Action<float> setter, float from, float to, float duration)
        => tween.TweenMethod(Callable.From(setter), from, to, duration);

    /// <summary>Creates and emits a temporary burst of GPU particles by duplicating the source emitter.
    /// The spawned particle system will automatically free itself after its lifetime expires.</summary>
    /// <param name="particles">The source <see cref="GpuParticles2D"/> to duplicate.</param>
    /// <param name="makeUniqueMaterial">If true, duplicates the ProcessMaterial to avoid shared references between instances.</param>
    /// <returns>The spawned <see cref="GpuParticles2D"/> instance that performs the burst.</returns>
    public static GpuParticles2D SpawnBurst(this GpuParticles2D particles, bool makeUniqueMaterial = true)
    {
        GpuParticles2D spawn = particles.Duplicate() as GpuParticles2D;

        if (makeUniqueMaterial && spawn.ProcessMaterial != null) // Make process material unique
            spawn.ProcessMaterial = spawn.ProcessMaterial.Duplicate() as Material;

        particles.GetParent().AddChild(spawn, false, Node.InternalMode.Front);
        spawn.GlobalPosition = particles.GlobalPosition;
        spawn.Emitting = spawn.OneShot = true;
        spawn.Name = "Spawned Particles";

        particles.GetTree().CreateTimer(spawn.Lifetime).Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(spawn))
                spawn.QueueFree();
        };

        return spawn;
    }

    ///<summary>Asynchronously waits until a given condition becomes true, checking once per frame.
    /// This is <see cref="Engine.TimeScale"/> dependant.</summary>
    ///<param name="tree">The current <see cref="SceneTree"/> used to yield each frame.</param>
    ///<param name="condition">A function that returns true when the wait should end.</param>
    public static async Task DelayUntil(this SceneTree tree, Func<bool> condition)
    {
        while (!condition())
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    ///<summary>Asynchronously waits for a specified amount of real-time seconds using frame-based updates.
    /// This is <see cref="Engine.TimeScale"/> dependant.</summary>
    ///<param name="tree">The current <see cref="SceneTree"/> used to yield each frame.</param>
    ///<param name="seconds">The number of seconds to wait before continuing execution.</param>
    public static async Task Delay(this SceneTree tree, float seconds)
    {
        double elapsed = 0.0;
        while (elapsed < seconds)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            elapsed += tree.Root.GetProcessDeltaTime();
        }
    }
}
