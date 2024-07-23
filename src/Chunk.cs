using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game;

public partial class Chunk : Node3D
{
	[Export] public Vector2I ChunkPosition = Vector2I.Zero;
	public Voxel[][][] voxels;
	public List<long> navPoints = new();
	public List<(long, long)> navConnections = new();

	private MeshInstance3D meshInstance;
	private SurfaceTool surfaceTool;

    private static readonly List<Vector3I> VERTICES = new()
	{
		new(0, 0, 0), // 0	   2 +--------+ 3  a+------+b
		new(1, 0, 0), // 1	    /|       /|     |      |
		new(0, 1, 0), // 2	   / |      / |     |      |
		new(1, 1, 0), // 3	6 +--------+ 7|    c+------+d
		new(0, 0, 1), // 4	  |0 +-----|--+ 1  
		new(1, 0, 1), // 5	  | /      | /     b-a-c
		new(0, 1, 1), // 6	  |/       |/      b-c-d
		new(1, 1, 1)  // 7	4 +--------+ 5
	};

	private struct Side
	{
		public int vertex0;
		public int vertex1;
		public int vertex2;
		public int vertex3;
		public Vector3I normal;
	}

	private static readonly Side TOP = new() { vertex0 = 2, vertex1 = 3, vertex2 = 6, vertex3 = 7, normal = new Vector3I(0, 1, 0)};
	private static readonly Side BOTTOM = new() { vertex0 = 4, vertex1 = 5, vertex2 = 0, vertex3 = 1, normal = new Vector3I(0, -1, 0)};
	private static readonly Side LEFT = new() { vertex0 = 7, vertex1 = 3, vertex2 = 5, vertex3 = 1, normal = new Vector3I(1, 0, 0)};
	private static readonly Side RIGHT = new() { vertex0 = 2, vertex1 = 6, vertex2 = 0, vertex3 = 4, normal = new Vector3I(-1, 0, 0)};
	private static readonly Side BACK = new() { vertex0 = 6, vertex1 = 7, vertex2 = 4, vertex3 = 5, normal = new Vector3I(0, 0, 1)};
	private static readonly Side FRONT = new() { vertex0 = 3, vertex1 = 2, vertex2 = 1, vertex3 = 0, normal = new Vector3I(0, 0, -1)};

	private static readonly Vector2I TEXTURE_ATLAS_SIZE = new(16, 16);
	public static readonly Vector2I CHUNK_SIZE = new(16, 64);

	public override void _Ready()
	{
		surfaceTool = new();
		meshInstance = GetNode<MeshInstance3D>("Mesh");
	}

	public static Vector2I IndexToVector(byte index)
	{
		
		return new Vector2I((byte)(index % TEXTURE_ATLAS_SIZE.X), (byte)(index / TEXTURE_ATLAS_SIZE.Y));
	}

	public static byte VectorToIndex(Vector2I vector)
	{
		return (byte)(vector.X + (TEXTURE_ATLAS_SIZE.Y * vector.Y));
	}

	public Voxel GetVoxel(Vector3I position)
	{
		return voxels[position.X][position.Y][position.Z];
	}

	public static bool IsVoxelInBounds(Vector3I position)
	{
		return !((position.X >= CHUNK_SIZE.X) || (position.Y >= CHUNK_SIZE.Y) || (position.Z >= CHUNK_SIZE.X) || (position.X < 0) || (position.Y < 0) || (position.Z < 0));
	}

	public bool IsVoxelSolid(Vector3I position)
	{	
		return IsVoxelInBounds(position) && GetVoxel(position).id > 0;
	}

	private void RebuildNav()
	{
		for (byte x = 0; x < CHUNK_SIZE.X; x++)
		{
			for (byte z = 0; z < CHUNK_SIZE.X; z++)
			{
				for (byte y = 0; y < CHUNK_SIZE.Y; y++)
				{
					if (y + 1 >= CHUNK_SIZE.Y) continue; // TODO too high dont allow
					if (y - 1 < 0) continue; // too low dont allow

					if (voxels[x][y + 1][z].id > 0) continue; // voxel above is air
					if (voxels[x][y][z].id > 0) continue; // this voxel is air
					if (voxels[x][y - 1][z].id <= 0) continue; // voxel below is solid

					navPoints.Add((long)DataPacking.PackData(x, y, z, (short)ChunkPosition.X, (short)ChunkPosition.Y));
				}
			}
		}

		GD.Print($"made nav points on {ChunkPosition}: {navPoints.Count}");

		foreach (long pointId in navPoints)
		{
			DataPacking.UnpackData((ulong)pointId, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);

			Vector3I voxelOrigin = new(voxelX, voxelY, voxelZ);

			for (int x = -1; x <= 1; x++)
			{
				for (int z = -1; z <= 1; z++)
				{
					for (int y = -1; y <= 1; y++)
					{
						if (x == 0 && y == 0 && z == 0) continue;
						Vector3I dest = new(voxelOrigin.X + x, voxelOrigin.Y + y, voxelOrigin.Z + z);
						if (new Vector3(dest.X, voxelOrigin.Y, dest.Z).DistanceTo(voxelOrigin) > 1f) continue; // not allowing diagonals. TODO: this is horrible
						if (!IsVoxelInBounds(dest)) continue;
						if ((dest.Y <= 0) || (dest.Y > CHUNK_SIZE.Y)) continue;

						long destPacked = (long)DataPacking.PackData((byte)dest.X, (byte)dest.Y, (byte)dest.Z, (short)ChunkPosition.X, (short)ChunkPosition.Y);

						if (navConnections.Contains((pointId, destPacked))) continue;

						if (voxels[dest.X][dest.Y][dest.Z].id > 0) continue;
						if (voxels[dest.X][dest.Y - 1][dest.Z].id <= 0) continue;
						if (voxels[dest.X][dest.Y + 1][dest.Z].id > 0) continue;

						navConnections.Add((pointId, destPacked));
					}
				}
			}
		}
	}

	private void RebuildVoxel(Voxel voxel, Vector3I voxelPosition)
	{
		Vector3I realPosition = new(voxelPosition.X + (ChunkPosition.X * CHUNK_SIZE.X), voxelPosition.Y, voxelPosition.Z + (ChunkPosition.Y * CHUNK_SIZE.X));
		VoxelData.Data voxelData = VoxelData.DATA[(VoxelData.ID)voxel.id];

		if (!IsVoxelSolid(voxelPosition + FRONT.normal))
			RebuildSide(realPosition, FRONT, IndexToVector(voxelData.texture_lookup[0]), voxel.light);
		if (!IsVoxelSolid(voxelPosition + BACK.normal))
			RebuildSide(realPosition, BACK, IndexToVector(voxelData.texture_lookup[1]), voxel.light);
		
		if (!IsVoxelSolid(voxelPosition + RIGHT.normal))
			RebuildSide(realPosition, RIGHT, IndexToVector(voxelData.texture_lookup[2]), voxel.light);
		if (!IsVoxelSolid(voxelPosition + LEFT.normal))
			RebuildSide(realPosition, LEFT, IndexToVector(voxelData.texture_lookup[3]), voxel.light);

		if (!IsVoxelSolid(voxelPosition + BOTTOM.normal))
			RebuildSide(realPosition, BOTTOM, IndexToVector(voxelData.texture_lookup[4]), voxel.light);
		if (!IsVoxelSolid(voxelPosition + TOP.normal))
			RebuildSide(realPosition, TOP, IndexToVector(voxelData.texture_lookup[5]), voxel.light);
	}

	private void RebuildSide(Vector3I realPos, Side side, Vector2 textureAtlasOffset, byte light)
	{
		// a+------+b 
		//  |      | b-c-a
		//  |      | b-d-c
		// c+------+d

		Vector3I a = VERTICES[side.vertex0] + realPos;
		Vector3I b = VERTICES[side.vertex1] + realPos;
		Vector3I c = VERTICES[side.vertex2] + realPos;
		Vector3I d = VERTICES[side.vertex3] + realPos;

		Vector2 uvOffset = textureAtlasOffset / TEXTURE_ATLAS_SIZE;
		float height = 1.0f / TEXTURE_ATLAS_SIZE.Y;
		float width = 1.0f / TEXTURE_ATLAS_SIZE.X;

		Vector2 uvA = uvOffset + Vector2.Zero;
		Vector2 uvB = uvOffset + new Vector2(width, 0);
		Vector2 uvC = uvOffset + new Vector2(0, height);
		Vector2 uvD = uvOffset + new Vector2(width, height);

		// this is probably horrible
		if (side.normal == Vector3I.Up) light = 255;
		else if (side.normal == Vector3I.Back) light = 180;
		else if (side.normal == Vector3I.Right) light = 165;
		else light = 135;

		surfaceTool.SetCustom(0, new Color((float)light / byte.MaxValue, 0, 0));
		surfaceTool.AddTriangleFan(new Vector3[] { b, c, a }, new Vector2[] { uvB, uvC, uvA	});

		surfaceTool.SetCustom(0, new Color((float)light / byte.MaxValue, 0, 0));
		surfaceTool.AddTriangleFan(new Vector3[] { b, d, c }, new Vector2[] { uvB, uvD, uvC });
	}

	public void SetVoxel(Vector3I position, byte id)
	{
		voxels[position.X][position.Y][position.Z] = new Voxel() { id = id, light = byte.MaxValue };
	}

	/// <summary>
	/// Fills every voxel with 0s
	/// </summary>
	public void FillBlank()
	{
		voxels = new Voxel[CHUNK_SIZE.X][][];
		for (int x = 0; x < CHUNK_SIZE.X; x++)
		{
			voxels[x] = new Voxel[CHUNK_SIZE.Y][];
			for (int y = 0; y < CHUNK_SIZE.Y; y++)
			{
				voxels[x][y] = new Voxel[CHUNK_SIZE.X];
				for (int z = 0; z < CHUNK_SIZE.X; z++)
				{
					SetVoxel(new Vector3I(x, y, z), 0);
				}
			}
		}
	}

	/// <summary>
	/// Natural generation first pass
	/// </summary>
	public void Regenerate()
	{	
		for (int x = 0; x < CHUNK_SIZE.X; x++)
		{
			for (int z = 0; z < CHUNK_SIZE.X; z++)
			{
				FastNoiseLite baseNoise = Find.World.baseNoise;
				baseNoise.Offset = new Vector3(ChunkPosition.X * CHUNK_SIZE.X, ChunkPosition.Y * CHUNK_SIZE.X, 0);
				int colHeight = (int)(((baseNoise.GetNoise2D(x, z) + 1f) / 2f) * (CHUNK_SIZE.Y * 0.75));

				colHeight = (int)(colHeight * 2f);

				foreach (FastNoiseLite additiveNoise in Find.World.additiveNoises)
				{
					FastNoiseLite thisNoise = additiveNoise;
					thisNoise.Offset = new Vector3(ChunkPosition.X * CHUNK_SIZE.X, ChunkPosition.Y * CHUNK_SIZE.X, 0);
					colHeight = Mathf.Max(colHeight, colHeight + (int)(thisNoise.GetNoise2D(x, z) * (CHUNK_SIZE.Y)));
				}

				colHeight /= 4;
				colHeight += CHUNK_SIZE.Y / 4;

				VoxelData.ID voxelID;

				// if (colHeight <= CHUNK_SIZE.Y / 9) voxelID = VoxelData.ID.DIRT;
				if (colHeight <= CHUNK_SIZE.Y / 1.9 + (GD.Randf() * 1.5)) voxelID = VoxelData.ID.GRASS;
				else voxelID = VoxelData.ID.STONE;

				for (int y = 0; y < CHUNK_SIZE.Y; y++)
				{
					if (y == 0) { SetVoxel(new Vector3I(x, y, z), (byte)VoxelData.ID.HARDSTONE); continue; }
					if (y > colHeight) continue;

					if (voxelID == VoxelData.ID.GRASS)
					{
						if (y == colHeight)
							SetVoxel(new Vector3I(x, y, z), (byte)VoxelData.ID.GRASS);
						else if (y < colHeight && y >= colHeight - GD.RandRange(3, 5))
							SetVoxel(new Vector3I(x, y, z), (byte)VoxelData.ID.DIRT);
						else
							SetVoxel(new Vector3I(x, y, z), (byte)VoxelData.ID.STONE);
					}
					else
					{
						SetVoxel(new Vector3I(x, y, z), (byte)voxelID);
					}
				}
			}
		}

		void MakeOreWorm(OresData.ID oreID)
		{
			OresData.Data oreData = OresData.data[oreID];
			int oresToSpawn = oreData.weights.RandomElementByWeight(e => e.Value).Key;

			// GD.Print($"gonna make {oresToSpawn} ores");
			List<Vector3I> ores = new(oresToSpawn);

			Vector3I metalAt = new(
				Random.RandRange(0, CHUNK_SIZE.X),
				Random.RandRange(oreData.YRange.X, oreData.YRange.Y),
				Random.RandRange(0, CHUNK_SIZE.X)
			);

			Vector3I[] directions = { Vector3I.Back, Vector3I.Forward, Vector3I.Down, Vector3I.Up, Vector3I.Left, Vector3I.Right };

			int tries = 0;
			while (oresToSpawn > 0)
			{
				if (tries >= 10) { /*GD.PushWarning("failed 10 attempts on placing ore");*/ break; };

				List<VoxelData.ID> allowedToOverwrite = OresData.data.Select((x) => x.Value.voxelID).Where((x) => x != VoxelData.ID.VOID).ToList();
				allowedToOverwrite.Add(VoxelData.ID.STONE);

				if (
					ores.Contains(metalAt) ||
					!IsVoxelInBounds(metalAt) ||
					!allowedToOverwrite.Contains((VoxelData.ID)voxels[metalAt.X][metalAt.Y][metalAt.Z].id)
				)
				{
					tries++;
					continue;
				}
				else
				{
					// GD.Print($"placing metal at ({metalAt.X}, {metalAt.Y}, {metalAt.Z})");
					ores.Add(metalAt);
					oresToSpawn--;
				}

				metalAt += directions[Random.RandRange(0, directions.Length - 1)];
			}

			foreach (Vector3I metalPosition in ores)
				voxels[metalPosition.X][metalPosition.Y][metalPosition.Z] = new Voxel() { id = (byte)oreData.voxelID, light = 0 };
		}

		void MakeOreGrow(OresData.ID oreID)
		{
			OresData.Data oreData = OresData.data[oreID];
			int oresToSpawn = oreData.weights.RandomElementByWeight(e => e.Value).Key;

			// GD.Print($"gonna make {oresToSpawn} ores");
			List<Vector3I> ores = new(oresToSpawn);

			Vector3I metalAt = new(
				Random.RandRange(0, CHUNK_SIZE.X),
				Random.RandRange(oreData.YRange.X, oreData.YRange.Y),
				Random.RandRange(0, CHUNK_SIZE.X)
			);

			Vector3I[] directions = {
				Vector3I.Back, Vector3I.Forward, Vector3I.Back + Vector3I.Down, Vector3I.Back + Vector3I.Up, Vector3I.Forward + Vector3I.Down, Vector3I.Forward + Vector3I.Up,
				Vector3I.Down, Vector3I.Up, Vector3I.Down + Vector3I.Left, Vector3I.Down + Vector3I.Right, Vector3I.Up + Vector3I.Left, Vector3I.Up + Vector3I.Right,
				Vector3I.Left, Vector3I.Right,
			};

			int tries = 0;
			while (oresToSpawn > 0)
			{
				if (tries >= 50) { /*GD.PushWarning("failed 50 attempts on placing ore");*/ break; };

				List<VoxelData.ID> allowedToOverwrite = OresData.data.Select((x) => x.Value.voxelID).Where((x) => x != VoxelData.ID.VOID).ToList();
				allowedToOverwrite.Add(VoxelData.ID.STONE);

				if (
					ores.Contains(metalAt) ||
					!IsVoxelInBounds(metalAt) ||
					!allowedToOverwrite.Contains((VoxelData.ID)voxels[metalAt.X][metalAt.Y][metalAt.Z].id)
				)
				{
					tries++;
					continue;
				}
				else
				{
					// GD.Print($"placing metal at ({metalAt.X}, {metalAt.Y}, {metalAt.Z})");
					ores.Add(metalAt);
					oresToSpawn--;
				}

				metalAt = ores[Random.RandRange(ores.Count / 2, ores.Count - 1)] + directions[Random.RandRange(0, directions.Length - 1)];
			}

			foreach (Vector3I metalPosition in ores)
				Call(MethodName.SetVoxel, metalPosition, (byte)oreData.voxelID);
				// voxels[metalPosition.X][metalPosition.Y][metalPosition.Z] = new Voxel() { id = (byte)oreData.voxelID, light = 0 };
		}

		for (int i = 0; i < Random.RandRange(3, 5); i++)
			MakeOreWorm(OresData.ID.METAL);
		
		for (int i = 0; i < Random.RandRange(5, 8); i++)
			MakeOreGrow(OresData.ID.COAL);

		/////// hollowing out stone at and below 27 for debug
		// for (int x = 0; x < CHUNK_SIZE.X; x++)
		// {
		// 	for (int z = 0; z < CHUNK_SIZE.X; z++)
		// 	{
		// 		for (int y = 0; y <= 27; y++)
		// 		{
		// 			Voxel voxel = voxels[x][y][z];
		// 			if (voxel.id != (byte)VoxelData.ID.STONE) continue;

		// 			voxels[x][y][z] = new Voxel() { id = 0, light = 0 };
		// 		}
		// 	}
		// }
	}

	private void FreeCollision()
	{
		if (meshInstance.HasNode("Mesh_col"))
			meshInstance.GetNode("Mesh_col").Free();
	}

	public void DeRender()
	{
		FreeCollision();
		meshInstance.Mesh = null;
	}

	public void Rebuild()
	{
		DeRender();

		ArrayMesh arrayMesh = new();	

		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		surfaceTool.SetSmoothGroup(uint.MaxValue); // means -1

		surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RFloat);

		for (int x = 0; x < CHUNK_SIZE.X; x++)
		{
			for (int y = 0; y < CHUNK_SIZE.Y; y++)
			{
				for (int z = 0; z < CHUNK_SIZE.X; z++)
				{
					Voxel voxel = voxels[x][y][z];

					if (voxel.id > 0)
						RebuildVoxel(voxel, new Vector3I(x, y, z));
				}
			}
		}

		// BakeAO();

		surfaceTool.GenerateNormals();
		// do we need tangents...?
		surfaceTool.Commit(arrayMesh);
		meshInstance.Mesh = arrayMesh;
		meshInstance.CreateTrimeshCollision();

		meshInstance.GetNode<StaticBody3D>("Mesh_col").SetCollisionLayerValue(2, true);
		meshInstance.GetNode<StaticBody3D>("Mesh_col").SetCollisionMaskValue(2, true);

		RebuildNav();
	}
}
