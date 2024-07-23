using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Game;

public static class IEnumerableExtensions
{
    // <https://stackoverflow.com/a/11930875>
    public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
    {
        float totalWeight = sequence.Sum(weightSelector);
        float itemWeightIndex =  (float)new System.Random().NextDouble() * totalWeight;
        float currentWeightIndex = 0;

        foreach(var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
        {
            currentWeightIndex += item.Weight;

            // If we've hit or passed the weight we are after for this item then it's the one we want....
            if(currentWeightIndex > itemWeightIndex)
                return item.Value;
        }
        return default;
    }
}

public static class Utils
{
    public static Color GetRandomColor() { return new Color(GD.Randf(), GD.Randf(), GD.Randf()); }
}