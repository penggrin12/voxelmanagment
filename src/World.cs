using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Game;

public partial class World : Node3D
{
    [Export] public PackedScene chunkScene;
    [Export] public PackedScene playerScene;
    private Dictionary<Vector2I, Chunk> chunks = new();
    private const int RENDER_DISTANCE = 2;

    [Signal] public delegate void UpdateRenderDistanceEventHandler(Vector2 from); // useless...?

    private Queue<Vector2I> chunksToGenerate = new();
    private Queue<Vector2I> chunksToRender = new();

    public World()
    {
        Find.World = this;
    }

    private async void VoxelsThread()
    {
        while (true)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (chunksToGenerate.Count > 0)
            {
                Chunk chunkToGenerate = chunks[chunksToGenerate.Dequeue()];
                chunkToGenerate.FillBlank();
                chunkToGenerate.Regenerate();
            }

            if (chunksToRender.Count > 0)
            {
                Chunk chunkToRender = chunks[chunksToRender.Dequeue()];
                chunkToRender.Rebuild();
            }
        }
    }

    public Chunk GetChunk(int x, int y) { return GetChunk(new Vector2I(x, y)); }
    public Chunk GetChunk(Vector2I position)
    {
        return chunks[position];
    }

    public void SetChunk(int x, int y, Chunk chunk) { SetChunk(new Vector2I(x, y), chunk); }
    public void SetChunk(Vector2I position, Chunk newChunk)
    {
        chunks[position] = newChunk;
    }

    private void HandleUpdateRenderDistance(Vector2 from)
    {
        for (int x = Mathf.FloorToInt(from.X - RENDER_DISTANCE); x < Mathf.CeilToInt(from.X + RENDER_DISTANCE); x++)
        {   
            for (int y = Mathf.FloorToInt(from.Y - RENDER_DISTANCE); y < Mathf.CeilToInt(from.Y + RENDER_DISTANCE); y++)
            {
                Vector2I chunkPositionToMake = new(x, y);
                if ((from.DistanceTo(new Vector2(x, y)) <= RENDER_DISTANCE) && (!chunks.ContainsKey(chunkPositionToMake)))
                {
                    GD.Print($"gonna make {chunkPositionToMake}");
                    Chunk newChunk = chunkScene.Instantiate<Chunk>();
                    newChunk.ChunkPosition = chunkPositionToMake;
                    newChunk.Name = $"{chunkPositionToMake}";
                    AddChild(newChunk);
                    SetChunk(newChunk.ChunkPosition, newChunk);
                    
                    chunksToGenerate.Enqueue(newChunk.ChunkPosition);
                    chunksToRender.Enqueue(newChunk.ChunkPosition);
                }
            }
        }

        foreach (Vector2I chunkPosition in chunks.Keys)
        {
            chunks[chunkPosition].ChunkPosition = chunkPosition;
        }

        List<Vector2I> chunksPositions = chunks.Keys.ToList();
        foreach (Vector2I chunkPosition in chunksPositions)
        {
            if (from.DistanceTo((Vector2)chunkPosition) >= RENDER_DISTANCE * 1.5f)
            {
                GD.Print(chunkPosition);
                chunks[chunkPosition].DeRender();
                chunks[chunkPosition].QueueFree();
                GD.Print(chunks.Remove(chunkPosition)); 
            }
        } 
    }

	public override void _Ready()
	{
        UpdateRenderDistance += HandleUpdateRenderDistance;

        Thread voxelThread = new(VoxelsThread);
        voxelThread.Start();

        BasePlayer player = playerScene.Instantiate<BasePlayer>();
        player.world = this;
        player.Position = new Vector3(
            Chunk.CHUNK_SIZE.X / 2,
            Chunk.CHUNK_SIZE.Y,
            Chunk.CHUNK_SIZE.X / 2
        );

        AddChild(player);
	}

    public override void _Process(double delta)
    {
        if (!Find.DebugUi.Enabled) return;

        Find.DebugUi.Get<Label>("Memory").Text = $"MEM: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1000 / 1000:n0} mB / {Performance.GetMonitor(Performance.Monitor.MemoryStaticMax) / 1000 / 1000:n0} mB";
    }
}
