using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Game.Structs;

namespace Game;

public static class Node3DExtensions
{
	public static Location GetLocation(this Node3D node3D)
	{
		Vector2I chunkPosition = new(
			Mathf.FloorToInt(node3D.GlobalPosition.X / Chunk.CHUNK_SIZE.X),
			Mathf.FloorToInt(node3D.GlobalPosition.Z / Chunk.CHUNK_SIZE.X)
		);
		Vector3I voxelPosition = (Vector3I)(node3D.GlobalPosition - new Vector3(chunkPosition.X * Chunk.CHUNK_SIZE.X, 0, chunkPosition.Y * Chunk.CHUNK_SIZE.X).Floor());

		return new Location() { chunkPosition = chunkPosition, voxelPosition = voxelPosition};
	}
}

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