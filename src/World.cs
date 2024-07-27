using Godot;
using System.Collections.Generic;
using System.Linq;
using Game.Interfaces;
using Game.Structs;
using Game.Pathfinding;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Game;

public partial class World : Node3D
{
    [Export] public PackedScene chunkScene;
    [Export] public PackedScene playerScene;
    [Export] public PackedScene testEntityScene;
    private readonly Dictionary<Vector2I, Chunk> chunks = new();

    [Export] bool makeAllTheChunks = true;
    public bool islandMode = Settings.IslandMode; // should this be here..?
    [Export] public Gradient islandGradient;

    private readonly Queue<Vector2I> chunksToGenerate = new();
    private readonly ConcurrentQueue<Vector2I> chunksToRender = new();
    [Export] public FastNoiseLite baseNoise = new() { Seed = Random.Seed };
    [Export] public FastNoiseLite[] additiveNoises = System.Array.Empty<FastNoiseLite>();
    [Export] public FastNoiseLite[] subtractiveNoises = System.Array.Empty<FastNoiseLite>();

    [Signal] public delegate void WorldLoadingCompleteEventHandler();
    private bool worldLoadedYet = false;

    [Signal] public delegate void ChunkUpdatedEventHandler(Vector2I position);

    private readonly List<IEntity> entities = new();

    public AStar3D AStar { get; set; }

    public World()
    {
        Find.World = this;
    }

    private void VoxelsThread()
    {
        while (true)
        {
            while (chunksToGenerate.Count > 0)
            {
                Chunk chunkToGenerate = chunks[chunksToGenerate.Dequeue()];
                chunkToGenerate.FillBlank();
                chunkToGenerate.Regenerate();
            }

            Parallel.For(0, chunksToRender.Count, parallelOptions: new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (i) => {
                while (chunksToRender.TryDequeue(out Vector2I chunkPosition))
                {
                    chunks[chunkPosition].Rebuild();
                }
            });

            if (!worldLoadedYet)
            {
                CallThreadSafe(MethodName.EmitSignal, SignalName.WorldLoadingComplete);
                worldLoadedYet = true;
            }
        }
    }

    public void UpdateChunk(int x, int y) { UpdateChunk(new Vector2I(x, y)); }
    public async void UpdateChunk(Vector2I position)
    {
        chunksToRender.Enqueue(position);

        if (worldLoadedYet)
        {
            EmitSignal(SignalName.ChunkUpdated, position);
            AStar = await Pathfinder.PopulateAStar();
        }
    }

    public bool HasChunk(int x, int y) { return HasChunk(new Vector2I(x, y)); }
    public bool HasChunk(Vector2I position)
    {
        return chunks.ContainsKey(position);
    }

    public Chunk GetChunk(int x, int y) { return GetChunk(new Vector2I(x, y)); }
    public Chunk GetChunk(Vector2I position)
    {
        return chunks[position];
    }

    public Chunk[] GetAllChunks()
    {
        return chunks.Values.ToArray();
    }

    public void SetChunk(int x, int y, Chunk chunk) { SetChunk(new Vector2I(x, y), chunk); }
    public void SetChunk(Vector2I position, Chunk newChunk)
    {
        chunks[position] = newChunk;
    }

    public void MakeChunk(int x, int y) { MakeChunk(new Vector2I(x, y)); }
    private void MakeChunk(Vector2I chunkPosition)
    {
        Chunk newChunk = chunkScene.Instantiate<Chunk>();
        newChunk.ChunkPosition = chunkPosition;
        newChunk.Name = $"{chunkPosition}";
        AddChild(newChunk);
        SetChunk(newChunk.ChunkPosition, newChunk);
        chunksToGenerate.Enqueue(newChunk.ChunkPosition);
        UpdateChunk(chunkPosition);
    }

    private void MakeWorldChunks()
    {
        for (int x = Mathf.FloorToInt(-(Settings.WorldSize / 2)); x < Mathf.CeilToInt((Settings.WorldSize / 2) + 1); x++)
        {   
            for (int y = Mathf.FloorToInt(-(Settings.WorldSize / 2)); y < Mathf.CeilToInt((Settings.WorldSize / 2) + 1); y++)
            {
                Vector2I chunkPositionToMake = new(x, y);

                if (HasChunk(chunkPositionToMake)) continue;

                GD.Print($"gonna make {chunkPositionToMake}");
                MakeChunk(chunkPositionToMake);
            }
        }
    }

    public void AddEntity(IEntity entity)
    {
        entities.Add(entity);
        GetNode<Node>("Entities").AddChild(entity.AsNode3D());
    }

    private void GenerateEntities()
    {
        // TODO: make it past this basic testing

        int gonnaSpawnTestEntities = 16;
        for (int i = 0; i < gonnaSpawnTestEntities; i++)
        {
            Location spawnAt = new() {
                chunkPosition = new(
                    Random.Next(-(Settings.WorldSize / 2), (Settings.WorldSize / 2) + 1),
                    Random.Next(-(Settings.WorldSize / 2), (Settings.WorldSize / 2) + 1)
                ),
                voxelPosition = new(
                    Random.Next(0, Chunk.CHUNK_SIZE.X - 1),
                    Chunk.CHUNK_SIZE.Y - 1, // we gonna walk towards bottom
                    Random.Next(0, Chunk.CHUNK_SIZE.X - 1)
                )
            };

            Chunk chunk = GetChunk(spawnAt.chunkPosition);

            int airs = 0;
            while (spawnAt.voxelPosition.Y > 0)
            {
                Voxel voxel = chunk.GetVoxel(spawnAt.voxelPosition);
                if (voxel.id <= 0)
                    airs++;
                else if (airs >= 2)
                    break;
                else
                    airs = 0;

                spawnAt.voxelPosition.Y--;
            }

            spawnAt.voxelPosition.Y++;

            if (spawnAt.voxelPosition.Y <= 0)
            {
                GD.PushWarning($"couldnt find valid spot to spawn entity at {spawnAt}");
                continue;
            }

            IEntity entity = testEntityScene.Instantiate<IEntity>();
            entity.AsNode3D().Position = spawnAt.GetGlobalPosition() + new Vector3(0.5f, 0f, 0.5f);
            AddEntity(entity);
        }
    }

	public override void _Ready()
	{
        foreach (FastNoiseLite additiveNoise in additiveNoises)
            additiveNoise.Seed = Random.Seed;

        foreach (FastNoiseLite subtractiveNoise in subtractiveNoises)
            subtractiveNoise.Seed = Random.Seed;

        MakeChunk(Vector2I.Zero);

        if (makeAllTheChunks)
            MakeWorldChunks();

        Task voxelThread = new(VoxelsThread);
        voxelThread.ContinueWith((Task task) => { throw task.Exception.InnerException; }, TaskContinuationOptions.OnlyOnFaulted);
        voxelThread.Start();

        // TODO: after implementing proper player, load it after WorldLoadingComplete
        IPlayer player = playerScene.Instantiate<IPlayer>();
        player.AsNode3D().Position = new Vector3(
            Chunk.CHUNK_SIZE.X / 2,
            Chunk.CHUNK_SIZE.Y,
            Chunk.CHUNK_SIZE.X / 2
        );

        Find.Player = player;

        AddEntity(player);

        WorldLoadingComplete += async () => {AStar = await Pathfinder.PopulateAStar(); GenerateEntities();};
	}

    public override void _Process(double delta)
    {
        if (Settings.ShowDebugDraw)
        {
            foreach (Chunk chunk in GetAllChunks())
            {
                DebugDraw.Box(new Vector3(chunk.ChunkPosition.X * (Chunk.CHUNK_SIZE.X) + (Chunk.CHUNK_SIZE.X / 2), Chunk.CHUNK_SIZE.Y / 2, chunk.ChunkPosition.Y * (Chunk.CHUNK_SIZE.X) + (Chunk.CHUNK_SIZE.X / 2)), new Vector3(Chunk.CHUNK_SIZE.X, Chunk.CHUNK_SIZE.Y, Chunk.CHUNK_SIZE.X));
            }
        }

        if (!Find.DebugUi.Enabled) return;

        Find.DebugUi.Get<Label>("Memory").Text = $"MEM: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1000 / 1000:n0} mB / {Performance.GetMonitor(Performance.Monitor.MemoryStaticMax) / 1000 / 1000:n0} mB";
    }
}
