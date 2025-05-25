using Godot;
using System.Collections.Generic;
using Game.Interfaces;
using Game.Structs;
using Game.Pathfinding;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Game;

public partial class World : Node3D
{
	[Export] public PackedScene chunkScene;
	[Export] public PackedScene playerScene;
	[Export] public PackedScene testEntityScene;
	private readonly Dictionary<Vector2I, Chunk> chunks = [];

	[Export] bool makeAllTheChunks = true;
	public bool islandMode = Settings.IslandMode; // should this be here..?
	[Export] public Gradient islandGradient;

	private readonly Queue<Vector2I> chunksToGenerate = new();
	private readonly ConcurrentQueue<Vector2I> chunksToRender = new();
	[Export] public FastNoiseLite baseNoise = new() { Seed = Random.Seed };
	[Export] public FastNoiseLite[] additiveNoises = [];
	[Export] public FastNoiseLite[] subtractiveNoises = [];

	[Signal] public delegate void WorldChunksGenerationCompleteEventHandler();
	[Signal] public delegate void WorldLoadingCompleteEventHandler();
	private bool worldLoadedYet = false;

	[Signal] public delegate void ChunkUpdatedEventHandler(Vector2I position);

	private readonly List<IEntity> entities = [];

	public AStar3D AStar { get; set; }

	public World()
	{
		Find.World = this;
	}

	private void VoxelsThread()
	{
		while (true)
		{
			Thread.Sleep(1);

			// Stopwatch stopwatch = new();

			while (chunksToGenerate.Count > 0)
			{
				// stopwatch.Reset();
				// stopwatch.Start();
				Chunk chunkToGenerate = chunks[chunksToGenerate.Dequeue()];
				chunkToGenerate.FillBlank();
				chunkToGenerate.Regenerate();

				// stopwatch.Stop();
				// GD.Print($"took time to generate [{chunkToGenerate.ChunkPosition}]: {stopwatch.Elapsed.Milliseconds} ms.");
			}

			if (!worldLoadedYet)
				CallThreadSafe(GodotObject.MethodName.EmitSignal, SignalName.WorldChunksGenerationComplete);

			if (chunksToRender.IsEmpty) continue;

			// stopwatch.Start();

			// int m = 0;

			while (chunksToRender.TryDequeue(out Vector2I chunkPosition))
			{
				// Stopwatch inner_stopwatch = new();
				// inner_stopwatch.Start();

				chunks[chunkPosition].Rebuild();
				chunks[chunkPosition].RebuildCollision();

				// inner_stopwatch.Stop();
				// GD.Print($"took time to rebuild [{chunkPosition}]: {inner_stopwatch.Elapsed.Milliseconds} ms.");
				// m += inner_stopwatch.Elapsed.Milliseconds;
			}

			// stopwatch.Stop();
			// GD.Print($"took time to rebuild {chunksToRender.Count} chunks: {m} ms.");

			if (!worldLoadedYet)
			{
				CallThreadSafe(GodotObject.MethodName.EmitSignal, SignalName.WorldLoadingComplete);
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
		return [.. chunks.Values];
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

	public Voxel GetVoxel(Vector3I position) { return GetVoxel(Location.FromGlobalPosition(position)); }
	public Voxel GetVoxel(Location location)
	{
		return GetChunk(location.chunkPosition).GetVoxel(location.voxelPosition);
	}

	public void SetVoxel(Vector3I position, byte id) { SetVoxel(Location.FromGlobalPosition(position), id); }
	public void SetVoxel(Location location, byte id)
	{
		GetChunk(location.chunkPosition).SetVoxel(location.voxelPosition, id);
	}

	// TODO
	public List<Aabb> GetCubes(Aabb aABB) {
		List<Aabb> aABBs = [];
		int x0 = (int)aABB.Position.X;
		int x1 = (int)(aABB.Size.X + 1.0F);
		int y0 = (int)aABB.Position.Y;
		int y1 = (int)(aABB.Size.Y + 1.0F);
		int z0 = (int)aABB.Position.Z;
		int z1 = (int)(aABB.Size.Z + 1.0F);
		if (x0 < 0) {
			x0 = 0;
		}

		if (y0 < 0) {
			y0 = 0;
		}

		if (z0 < 0) {
			z0 = 0;
		}

		// if (x1 > this.width) {
		//     x1 = this.width;
		// }

		// if (y1 > this.depth) {
		//     y1 = this.depth;
		// }

		// if (z1 > this.height) {
		//     z1 = this.height;
		// }

		for(int x = x0; x < x1; ++x) {
			for(int y = y0; y < y1; ++y) {
				for(int z = z0; z < z1; ++z) {
					Voxel voxel = GetVoxel(new Vector3I(x, y, z));
					if (voxel.id == (byte)VoxelData.ID.VOID) continue;
					// if (voxel is not null) {
					Aabb aabb = Voxel.GetAABB(x, y, z);
					aABBs.Add(aabb);
					// }
				}
			}
		}

		return aABBs;
	}

	private void MakeWorldChunks()
	{
		for (int x = -(Settings.WorldSize / 2); x < (Settings.WorldSize / 2) + 1; x++)
		{
			for (int y = -(Settings.WorldSize / 2); y < (Settings.WorldSize / 2) + 1; y++)
			{
				Vector2I chunkPositionToMake = new(x, y);

				if (HasChunk(chunkPositionToMake)) continue;

				GD.Print($"gonna make {chunkPositionToMake}");
				MakeChunk(chunkPositionToMake);
			}
		}
	}

	public void RebuildChunkLazy(Chunk chunk) { RebuildChunkLazy(chunk.ChunkPosition); }
	public void RebuildChunkLazy(Vector2I chunkPos)
	{
		chunksToRender.Enqueue(chunkPos);
	}

	public async Task RebuildChunkAndNeighboursLazy(Chunk chunk) { await RebuildChunkAndNeighboursLazy(chunk.ChunkPosition); }
	public async Task RebuildChunkAndNeighboursLazy(Vector2I chunkPos)
	{
		for (int x = -1; x < 2; x++)
		{
			for (int y = -1; y < 2; y++)
			{
				RebuildChunkLazy(chunkPos);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task MakeStructures()
	{
		for (int i = 0; i < 100; i++)
		{
			var chunkPos = new Vector2I(
				GD.RandRange(Mathf.FloorToInt(-(Settings.WorldSize / 2)), Mathf.FloorToInt(Settings.WorldSize / 2)),
				GD.RandRange(Mathf.FloorToInt(-(Settings.WorldSize / 2)), Mathf.FloorToInt(Settings.WorldSize / 2))
			);
			var chunk = GetChunk(chunkPos);

			chunk.SetVoxel(new Vector3I(0, 0, 0), 0);
			// chunksToRender.Enqueue(chunkPos);
			await RebuildChunkAndNeighboursLazy(chunkPos);
		}
		GD.Print("done making structures");
	}

	public void AddEntity(IEntity entity)
	{
		entities.Add(entity);
		GetNode<Node>("Entities").AddChild(entity.AsNode3D());
	}

	private void GenerateEntities()
	{
		// TODO: make it past this basic testing

		const int GONNA_SPAWN_ENTITIES = 16;
		for (int i = 0; i < GONNA_SPAWN_ENTITIES; i++)
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
				GD.PushWarning($"couldn't find valid spot to spawn entity at {spawnAt}");
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

		WorldChunksGenerationComplete += async () => { await MakeStructures(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); AStar = await Pathfinder.PopulateAStar(); GenerateEntities(); };

		Task voxelThread = new(VoxelsThread);
		voxelThread.ContinueWith((Task task) => throw task.Exception.InnerException, TaskContinuationOptions.OnlyOnFaulted);
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

		// WorldLoadingComplete += async () => {};
	}

	public override void _Process(double delta)
	{
		if (Settings.ShowDebugDraw)
		{
			foreach (Chunk chunk in GetAllChunks())
			{
				DebugDraw.Box(new Vector3((chunk.ChunkPosition.X * Chunk.CHUNK_SIZE.X) + (Chunk.CHUNK_SIZE.X / 2), Chunk.CHUNK_SIZE.Y / 2, (chunk.ChunkPosition.Y * Chunk.CHUNK_SIZE.X) + (Chunk.CHUNK_SIZE.X / 2)), new Vector3(Chunk.CHUNK_SIZE.X, Chunk.CHUNK_SIZE.Y, Chunk.CHUNK_SIZE.X));
			}
		}

		if (!Find.DebugUi.Enabled) return;

		Find.DebugUi.Get<Label>("Memory").Text = $"MEM: {Performance.GetMonitor(Performance.Monitor.MemoryStatic) / 1000 / 1000:n0} mB / {Performance.GetMonitor(Performance.Monitor.MemoryStaticMax) / 1000 / 1000:n0} mB";
	}
}
