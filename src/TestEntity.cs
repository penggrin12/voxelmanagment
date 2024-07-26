using System.Collections.Generic;
using Game.Interfaces;
using Game.Pathfinding;
using Game.Structs;
using Godot;

namespace Game.Entities;

public partial class TestEntity : CharacterBody3D, IEntity, IPathfinding
{
    public AStar3D AStar { get; set; } = new();

    public override async void _Ready()
	{
		// rebuild nav for me each chunk update (this should maybe be shared...?)
        // TODO: causes huuuuuugeee memory consumption, make it shared
		Find.World.ChunkUpdated += async (Vector2I chunkPosition) => { await (this as IPathfinding).OnChunkUpdate(); };
		await (this as IPathfinding).OnChunkUpdate();
	}

    public override async void _Process(double delta)
	{
		Location playerAt = (Location)Find.Player.GetDebugThingie();

		if (!Input.IsActionJustPressed("debug_agent")) return;

		Vector2I chunkPosition = new(
            Mathf.FloorToInt(GlobalPosition.X / Chunk.CHUNK_SIZE.X),
            Mathf.FloorToInt(GlobalPosition.Z / Chunk.CHUNK_SIZE.X)
        );

        if (!Find.World.HasChunk(chunkPosition))
            return;

		Vector3I voxelPosition = (Vector3I)(GlobalPosition - new Vector3(chunkPosition.X * Chunk.CHUNK_SIZE.X, 0, chunkPosition.Y * Chunk.CHUNK_SIZE.X).Floor());

		(bool, Location[]) path = await Pathfinder.GetPath(AStar, new Location() { chunkPosition = chunkPosition, voxelPosition = voxelPosition }, playerAt);

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
