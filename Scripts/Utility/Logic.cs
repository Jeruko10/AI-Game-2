using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Utility;

public static class Logic
{
    static readonly Random RandomInstance = new();

    /// <summary>
    /// Returns a random element from the specified generic list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The non-null, non-empty list from which to retrieve a random element.</param>
    /// <returns>A randomly selected element from the list.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the list is null or contains no elements.
    /// </exception>
    public static T GetRandomElement<T>(this IList<T> list)
    {
        if (list == null || list.Count == 0)
            throw new InvalidOperationException("Cannot get random element from an empty or null list.");

        int index = RandomInstance.Next(0, list.Count);
        return list[index];
    }

    /// <summary>Returns a random sign, either -1 or 1, optionally including 0.</summary>
    /// <param name="includeZero">If true, the result can also be 0; otherwise only -1 or 1.</param>
    /// <returns>An integer value of -1, 0, or 1 depending on the <paramref name="includeZero"/> flag.</returns>
    public static int RandomSign(bool includeZero = false)
    {
        int[] options = includeZero ? [-1, 0, 1] : [-1, 1];
        return options.GetRandomElement();
    }

    /// <summary>
    /// Calculates the interpolation weight for exponential smoothing.
    /// </summary>
    /// <param name="smoothingSpeed">Smoothing speed (rate) in units per second; higher values converge faster.</param>
    /// <param name="delta">Frame delta time in seconds.</param>
    /// <returns>Interpolation weight in the range [0, 1), where values closer to 1 indicate stronger immediate response.</returns>
    public static float ComputeLerpWeight(float smoothingSpeed, float delta)
    {
        float weight = (float)(1f - Math.Exp(-smoothingSpeed * delta));
        return weight;
    }

    /// <summary>Wraps a value into the specified range [min, max), cycling back to the start when exceeding the bounds.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive lower bound of the range.</param>
    /// <param name="max">The exclusive upper bound of the range.</param>
    /// <returns>The wrapped value constrained within [min, max).</returns>
    public static float LoopRange(float value, float min, float max)
    {
        float range = max - min;
        float relative = value - min;
        float wrapped = (relative % range + range) % range;
        return min + wrapped;
    }

    /// <summary>Wraps a value into the specified range [min, max], cycling back to the start when exceeding the bounds.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="min">The inclusive lower bound of the range.</param>
    /// <param name="max">The inclusive upper bound of the range.</param>
    /// <returns>The wrapped integer constrained within [min, max].</returns>
    public static int LoopRange(int value, int min, int max)
    {
        int range = max - min + 1; // Include upper bound
        int relative = value - min;
        int wrapped = ((relative % range) + range) % range;
        return min + wrapped;
    }
    
    ///<summary>Asynchronously waits for a specified amount of seconds using <see cref="Task.Delay"/>.</summary>
    ///<param name="seconds">The number of seconds to delay the execution of the next operation.</param>
	public static async Task Delay(float seconds) => await Task.Delay((int)MathF.Round(seconds * 1000));
}
