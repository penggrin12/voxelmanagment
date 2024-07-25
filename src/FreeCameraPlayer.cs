using Godot;
using Game.Interfaces;
using Game.Pathfinding;
using Game.Structs;

namespace Game;

public partial class FreeCameraPlayer : CharacterBody3D, IPlayer
{
    private Camera3D camera;
    private World world;

    private Location agentStartLocation = new() { chunkPosition = Vector2I.Zero, voxelPosition = new Vector3I(0, 45, 0) };
    private Location agentEndLocation = new() { chunkPosition = Vector2I.Zero, voxelPosition = new Vector3I(3, 42, 0) };

    public void SetWorld(World world)
    {
        this.world = world;
    }

    public Node3D AsNode3D()
    {
        return this;
    }

    public override void _Ready()
    {
        camera = GetNode<Camera3D>("./Camera3D");
    }

    public override void _Process(double delta)
    {
        Find.DebugUi.Get<Label>("Position").Text = $"{camera.Position.X + (Chunk.CHUNK_SIZE.X / 2):n3}, {camera.Position.Y:n3}, {camera.Position.Z + (Chunk.CHUNK_SIZE.X / 2):n3} ( {Mathf.Wrap(camera.Position.X + (Chunk.CHUNK_SIZE.X / 2), 0, Chunk.CHUNK_SIZE.X):n3}, {Mathf.Wrap(camera.Position.Y, 0, Chunk.CHUNK_SIZE.Y):n3}, {Mathf.Wrap(camera.Position.Z + (Chunk.CHUNK_SIZE.X / 2), 0, Chunk.CHUNK_SIZE.X):n3} )";

        Vector2I chunkPosition = new(
            Mathf.FloorToInt(camera.GlobalPosition.X / Chunk.CHUNK_SIZE.X),
            Mathf.FloorToInt(camera.GlobalPosition.Z / Chunk.CHUNK_SIZE.X)
        );

        if (!world.HasChunk(chunkPosition))
            return; // not in a chunk (loading void..?)

		Chunk inChunk = world.GetChunk(chunkPosition);
		Vector3 cameraAt = camera.GlobalPosition;
		Vector3I voxelPosition = (Vector3I)(cameraAt - new Vector3(inChunk.ChunkPosition.X * Chunk.CHUNK_SIZE.X, 0, inChunk.ChunkPosition.Y * Chunk.CHUNK_SIZE.X).Floor());

		if (!Chunk.IsVoxelInBounds(voxelPosition))
		{
            // GD.Print("not in chunk");
            return;
        }

        if (Input.IsActionPressed("debug_agent_spawnpoint"))
		{
            agentStartLocation = new() { chunkPosition = chunkPosition, voxelPosition = voxelPosition };
		}

        if (Input.IsActionPressed("debug_agent_endpoint"))
		{
            agentEndLocation = new() { chunkPosition = chunkPosition, voxelPosition = voxelPosition };
		}

        if (Settings.ShowDebugDraw)
        {
            DebugDraw.Box(
                agentStartLocation.voxelPosition
                + new Vector3I(agentStartLocation.chunkPosition.X * Chunk.CHUNK_SIZE.X, 0, agentStartLocation.chunkPosition.Y * Chunk.CHUNK_SIZE.X)
                + new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(1, 1, 1),
                Colors.Green,
                drawSolid: true
            );

            DebugDraw.Box(
                agentEndLocation.voxelPosition
                + new Vector3I(agentEndLocation.chunkPosition.X * Chunk.CHUNK_SIZE.X, 0, agentEndLocation.chunkPosition.Y * Chunk.CHUNK_SIZE.X)
                + new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(1, 1, 1),
                Colors.Blue,
                drawSolid: true
            );
        }

        if (Input.IsActionJustPressed("debug_agent"))
        {
            (bool, Location[]) path = Pathfinder.GetPath(agentStartLocation, agentEndLocation);
            if (!path.Item1) {GD.PushWarning("no path found"); return;}

            GD.Print($"path with {path.Item2.Length} steps found");
        }
    }
}
