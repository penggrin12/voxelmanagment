using System.Collections.Generic;
using Game.Interfaces;
using Game.Pathfinding;
using Game.Structs;
using Godot;

namespace Game.Entities;

public partial class TestEntity : CharacterBody3D, IEntity, IPathfinding
{
	public override void _Process(double delta)
	{
		Location playerAt = (Location)Find.Player.GetDebugThingie();

		if (!Input.IsActionJustPressed("debug_agent")) return;

		Location imAt = this.GetLocation();

		if (!Find.World.HasChunk(imAt.chunkPosition))
			return;

		(bool, Location[]) path = Pathfinder.GetPath(Find.World.AStar, imAt, playerAt);

		if (Settings.ShowDebugDraw)
		{
			List<Vector3> linePositions = new(path.Item2.Length);
			Color thisColor = Utils.GetRandomColor();
			foreach (Location location in path.Item2)
			{
				Vector3 position = location.GetGlobalPosition() + new Vector3(0.5f, 0.5f, 0.5f);
				linePositions.Add(position);
	
				DebugDraw.Sphere(
					position,
					radius: 0.20f,
					color: thisColor,
					drawSolid: true,
					duration: 15
				);
			}

			DebugDraw.Lines(linePositions.ToArray(), color: Colors.Black, duration: 15);
		}
	}
}
