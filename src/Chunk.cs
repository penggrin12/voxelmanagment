using Game.Structs;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Game;

public partial class Chunk : Node3D
{
	[Export] public Vector2I ChunkPosition = Vector2I.Zero;
	public Voxel[,,] voxels;
	public List<long> navPoints = [];
	public List<(long, long)> navConnections = [];

	private MeshInstance3D meshInstance;

	[Export] Material regularMaterial;
	[Export] Material transparentMaterial;

	private static readonly List<Vector3I> VERTICES =
	[
		new(0, 0, 0), // 0	   2 +--------+ 3  a+------+b
		new(1, 0, 0), // 1	    /|       /|     |      |
		new(0, 1, 0), // 2	   / |      / |     |      |
		new(1, 1, 0), // 3	6 +--------+ 7|    c+------+d
		new(0, 0, 1), // 4	  |0 +-----|--+ 1  
		new(1, 0, 1), // 5	  | /      | /     b-a-c
		new(0, 1, 1), // 6	  |/       |/      b-c-d
		new(1, 1, 1)  // 7	4 +--------+ 5
	];

	public readonly record struct Side(int Vertex0, int Vertex1, int Vertex2, int Vertex3, Vector3I Normal);

	private static readonly Side TOP = new(Vertex0: 2, Vertex1: 3, Vertex2: 6, Vertex3: 7, Normal: new Vector3I(0, 1, 0));
	private static readonly Side BOTTOM = new(Vertex0: 4, Vertex1: 5, Vertex2: 0, Vertex3: 1, Normal: new Vector3I(0, -1, 0));
	private static readonly Side LEFT = new(Vertex0: 7, Vertex1: 3, Vertex2: 5, Vertex3: 1, Normal: new Vector3I(1, 0, 0));
	private static readonly Side RIGHT = new(Vertex0: 2, Vertex1: 6, Vertex2: 0, Vertex3: 4, Normal: new Vector3I(-1, 0, 0));
	private static readonly Side BACK = new(Vertex0: 6, Vertex1: 7, Vertex2: 4, Vertex3: 5, Normal: new Vector3I(0, 0, 1));
	private static readonly Side FRONT = new(Vertex0: 0, Vertex1: 1, Vertex2: 2, Vertex3: 3, Normal: new Vector3I(0, 0, -1));

	private static readonly Vector2I TEXTURE_ATLAS_SIZE = new(16, 16);
	public static readonly Vector2I CHUNK_SIZE = new(16, 64);

	public override void _Ready()
	{
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
		return voxels[position.X, position.Y, position.Z];
	}

	public static bool IsVoxelInBounds(Vector3I position)
	{
		return !((position.X >= CHUNK_SIZE.X) || (position.Y >= CHUNK_SIZE.Y) || (position.Z >= CHUNK_SIZE.X) || (position.X < 0) || (position.Y < 0) || (position.Z < 0));
	}

	public bool IsVoxelTranslucent(Vector3I position)
	{
		if (!IsVoxelInBounds(position)) return false;
		return IsVoxelTranslucent(GetVoxel(position).id);
	}

	public static bool IsVoxelTranslucent(byte voxelID)
	{
		return voxelID <= 0 || VoxelData.DATA[voxelID].Translucent;
	}

	public bool IsVoxelSolid(Vector3I position)
	{
		if (!IsVoxelInBounds(position)) return false;
		return IsVoxelSolid(GetVoxel(position).id);
	}
	public static bool IsVoxelSolid(byte voxelID)
	{
		return voxelID > 0 && VoxelData.DATA[voxelID].Solid;
	}

	public bool IsVoxelSolidForThisVoxel(Vector3I position, byte askerVoxelID)
	{
		if (!IsVoxelInBounds(position))
		{
			if ((position.Y >= CHUNK_SIZE.Y) || (position.Y < 0)) return false;

			Location at = new() { chunkPosition = ChunkPosition, voxelPosition = position };

			if (at.voxelPosition.X >= CHUNK_SIZE.X) {at.voxelPosition.X = 0; at.chunkPosition += Vector2I.Right;}
			if (at.voxelPosition.Z >= CHUNK_SIZE.X) {at.voxelPosition.Z = 0; at.chunkPosition += Vector2I.Down;}

			if (at.voxelPosition.X < 0) {at.voxelPosition.X = CHUNK_SIZE.X - 1; at.chunkPosition += Vector2I.Left;}
			if (at.voxelPosition.Z < 0) {at.voxelPosition.Z = CHUNK_SIZE.X - 1; at.chunkPosition += Vector2I.Up;}

			if (!Find.World.HasChunk(at.chunkPosition))
				return false;

			return Find.World.GetChunk(at.chunkPosition).IsVoxelSolidForThisVoxel(at.voxelPosition, askerVoxelID);
		}

		if (!IsVoxelInBounds(position)) return false;

		byte voxelID = GetVoxel(position).id;
		return VoxelData.DATA[voxelID].Solid || (askerVoxelID == voxelID);
	}

	private void RebuildNav()
	{
		navPoints.Clear();
		navConnections.Clear();

		for (byte x = 0; x < CHUNK_SIZE.X; x++)
		{
			for (byte z = 0; z < CHUNK_SIZE.X; z++)
			{
				for (byte y = 0; y < CHUNK_SIZE.Y; y++)
				{
					if (y + 1 >= CHUNK_SIZE.Y) continue; // TODO too high don't allow
					if (y < 1) continue; // too low don't allow

					// use GetVoxel or nah?
					if (voxels[x, y + 1, z].id > 0) continue; // voxel above is air
					if (voxels[x, y, z].id > 0) continue; // this voxel is air
					if (voxels[x, y - 1, z].id <= 0) continue; // voxel below is solid

					navPoints.Add((long)DataPacking.PackData(x, y, z, (short)ChunkPosition.X, (short)ChunkPosition.Y));
				}
			}
		}

		// GD.Print($"made nav points on {ChunkPosition}: {navPoints.Count}");

		foreach (long pointId in navPoints)
		{
			DataPacking.UnpackData((ulong)pointId, out byte voxelX, out byte voxelY, out byte voxelZ, out _, out _);

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

						if (GetVoxel(dest).id > 0) continue;
						if (GetVoxel(dest + Vector3I.Down).id <= 0) continue;
						if (GetVoxel(dest + Vector3I.Up).id > 0) continue;

						navConnections.Add((pointId, destPacked));
					}
				}
			}
		}
	}

	private void RebuildVoxel(Voxel voxel, SurfaceTool surfaceTool, Vector3I voxelPosition)
	{
		Vector3I realPosition = new(voxelPosition.X + (ChunkPosition.X * CHUNK_SIZE.X), voxelPosition.Y, voxelPosition.Z + (ChunkPosition.Y * CHUNK_SIZE.X));
		VoxelData.Data voxelData = VoxelData.DATA[voxel.id];

		if (!IsVoxelSolidForThisVoxel(voxelPosition + FRONT.Normal, voxel.id))
			RebuildSide(surfaceTool, realPosition, FRONT, IndexToVector(voxelData.Texture_lookup[0]), voxel.light);
		if (!IsVoxelSolidForThisVoxel(voxelPosition + BACK.Normal, voxel.id))
			RebuildSide(surfaceTool, realPosition, BACK, IndexToVector(voxelData.Texture_lookup[1]), voxel.light);

		if (!IsVoxelSolidForThisVoxel(voxelPosition + RIGHT.Normal, voxel.id))
			RebuildSide(surfaceTool, realPosition, RIGHT, IndexToVector(voxelData.Texture_lookup[2]), voxel.light);
		if (!IsVoxelSolidForThisVoxel(voxelPosition + LEFT.Normal, voxel.id))
			RebuildSide(surfaceTool, realPosition, LEFT, IndexToVector(voxelData.Texture_lookup[3]), voxel.light);

		if (!IsVoxelSolidForThisVoxel(voxelPosition + BOTTOM.Normal, voxel.id))
			RebuildSide(surfaceTool, realPosition, BOTTOM, IndexToVector(voxelData.Texture_lookup[4]), voxel.light);
		if (!IsVoxelSolidForThisVoxel(voxelPosition + TOP.Normal, voxel.id))
			RebuildSide(surfaceTool, realPosition, TOP, IndexToVector(voxelData.Texture_lookup[5]), voxel.light);
	}

	private static void RebuildSide(SurfaceTool surfaceTool, Vector3I realPos, Side side, Vector2 textureAtlasOffset, byte light)
	{
		// a+------+b 
		//  |      | b-c-a
		//  |      | b-d-c
		// c+------+d

		Vector3I a = VERTICES[side.Vertex0] + realPos;
		Vector3I b = VERTICES[side.Vertex1] + realPos;
		Vector3I c = VERTICES[side.Vertex2] + realPos;
		Vector3I d = VERTICES[side.Vertex3] + realPos;

		Vector2 uvOffset = textureAtlasOffset / TEXTURE_ATLAS_SIZE;
		float height = 1.0f / TEXTURE_ATLAS_SIZE.Y;
		float width = 1.0f / TEXTURE_ATLAS_SIZE.X;

		Vector2 uvA = uvOffset + Vector2.Zero;
		Vector2 uvB = uvOffset + new Vector2(width, 0);
		Vector2 uvC = uvOffset + new Vector2(0, height);
		Vector2 uvD = uvOffset + new Vector2(width, height);

		// this is probably horrible
		if (side.Normal == Vector3I.Up) light = 255;
		else if (side.Normal == Vector3I.Back) light = 180;
		else if (side.Normal == Vector3I.Right) light = 165;
		else light = 135;

		surfaceTool.SetCustom(0, new Color((float)light / byte.MaxValue, 0, 0));
		surfaceTool.AddTriangleFan([b, c, a], [uvB, uvC, uvA]);

		surfaceTool.SetCustom(0, new Color((float)light / byte.MaxValue, 0, 0));
		surfaceTool.AddTriangleFan([b, d, c], [uvB, uvD, uvC]);
	}

	public void SetVoxel(Vector3I position, byte id)
	{
		voxels[position.X, position.Y, position.Z] = new Voxel() { id = id, light = byte.MaxValue };
	}

	/// <summary>
	/// Fills every voxel with void
	/// </summary>
	public void FillBlank()
	{
		voxels = new Voxel[CHUNK_SIZE.X, CHUNK_SIZE.Y, CHUNK_SIZE.X];
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

				foreach (FastNoiseLite subtractiveNoise in Find.World.subtractiveNoises)
				{
					FastNoiseLite thisNoise = subtractiveNoise;
					thisNoise.Offset = new Vector3(ChunkPosition.X * CHUNK_SIZE.X, ChunkPosition.Y * CHUNK_SIZE.X, 0);
					colHeight = Mathf.Min(colHeight, colHeight - (int)(thisNoise.GetNoise2D(x, z) * (CHUNK_SIZE.Y * 1.5)));
				}

				colHeight /= 4;
				colHeight += CHUNK_SIZE.Y / 4;

				if (Find.World.islandMode)
				{
					Location at = new() {chunkPosition = ChunkPosition, voxelPosition = new Vector3I(x, 0, z)};
					colHeight -= (int)(Find.World.islandGradient.Sample(at.GetGlobalPosition().DistanceTo(Vector3I.Zero) / (Settings.WorldSize * CHUNK_SIZE.X)).R * (CHUNK_SIZE.Y / 2));
				}

				// Find.DebugUi.Get<Label>("Test").Text = (CHUNK_SIZE.Y / 1.9).ToString();

				VoxelData.ID voxelID;

				// if (colHeight <= CHUNK_SIZE.Y / 9) voxelID = VoxelData.ID.DIRT;
				// if (colHeight <= CHUNK_SIZE.Y / 4 + (GD.Randf() * 0.4)) voxelID = VoxelData.ID.WATER;
				if (colHeight <= 19 + Random.Next(0, 1)) voxelID = VoxelData.ID.SAND;
				else if (colHeight <= 33 + Random.Next(-1, 1)) voxelID = VoxelData.ID.GRASS;
				else voxelID = VoxelData.ID.STONE;

				for (int y = 0; y < CHUNK_SIZE.Y; y++)
				{
					if (y == 0) { SetVoxel(new Vector3I(x, y, z), (byte)VoxelData.ID.HARDSTONE); continue; }
					if (y == 1 && colHeight <= 0) { SetVoxel(new Vector3I(x, y, z), (byte)voxelID); continue; }
					if (y > colHeight)
					{
						if (y <= 18) SetVoxel(new Vector3I(x, y, z), (byte)VoxelData.ID.WATER);
						continue;
					}

					if (new List<VoxelData.ID>() { VoxelData.ID.GRASS, VoxelData.ID.SAND }.Contains(voxelID))
					{
						if (y == colHeight)
							SetVoxel(new Vector3I(x, y, z), (byte)voxelID);
						else if (y < colHeight && y >= colHeight - Random.Next(3, 5))
							SetVoxel(new Vector3I(x, y, z), voxelID == VoxelData.ID.SAND ? (byte)voxelID : (byte)VoxelData.ID.DIRT);
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
			int oresToSpawn = oreData.Weights.RandomElementByWeight(e => e.Value).Key;

			// GD.Print($"gonna make {oresToSpawn} ores");
			List<Vector3I> ores = new(oresToSpawn);

			Vector3I metalAt = new(
				Random.RandRange(0, CHUNK_SIZE.X),
				Random.RandRange(oreData.YRange.X, oreData.YRange.Y),
				Random.RandRange(0, CHUNK_SIZE.X)
			);

			Vector3I[] directions = [Vector3I.Back, Vector3I.Forward, Vector3I.Down, Vector3I.Up, Vector3I.Left, Vector3I.Right];

			int tries = 0;
			while (oresToSpawn > 0)
			{
				if (tries >= 10) { /*GD.PushWarning("failed 10 attempts on placing ore");*/ break; }

				List<VoxelData.ID> allowedToOverwrite = OresData.data.Select((x) => x.Value.VoxelID).Where((x) => x != VoxelData.ID.VOID).ToList();
				allowedToOverwrite.Add(VoxelData.ID.STONE);

				if (
					ores.Contains(metalAt) ||
					!IsVoxelInBounds(metalAt) ||
					!allowedToOverwrite.Contains((VoxelData.ID)GetVoxel(metalAt).id)
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
				SetVoxel(metalPosition, (byte)oreData.VoxelID);
		}

		void MakeOreGrow(OresData.ID oreID)
		{
			OresData.Data oreData = OresData.data[oreID];
			int oresToSpawn = oreData.Weights.RandomElementByWeight(e => e.Value).Key;

			// GD.Print($"gonna make {oresToSpawn} ores");
			List<Vector3I> ores = new(oresToSpawn);

			Vector3I metalAt = new(
				Random.RandRange(0, CHUNK_SIZE.X),
				Random.RandRange(oreData.YRange.X, oreData.YRange.Y),
				Random.RandRange(0, CHUNK_SIZE.X)
			);

			Vector3I[] directions = [
				Vector3I.Back, Vector3I.Forward, Vector3I.Back + Vector3I.Down, Vector3I.Back + Vector3I.Up, Vector3I.Forward + Vector3I.Down, Vector3I.Forward + Vector3I.Up,
				Vector3I.Down, Vector3I.Up, Vector3I.Down + Vector3I.Left, Vector3I.Down + Vector3I.Right, Vector3I.Up + Vector3I.Left, Vector3I.Up + Vector3I.Right,
				Vector3I.Left, Vector3I.Right,
			];

			int tries = 0;
			while (oresToSpawn > 0)
			{
				if (tries >= 50) { /*GD.PushWarning("failed 50 attempts on placing ore");*/ break; }

				List<VoxelData.ID> allowedToOverwrite = OresData.data.Select((x) => x.Value.VoxelID).Where((x) => x != VoxelData.ID.VOID).ToList();
				allowedToOverwrite.Add(VoxelData.ID.STONE);

				if (
					ores.Contains(metalAt) ||
					!IsVoxelInBounds(metalAt) ||
					!allowedToOverwrite.Contains((VoxelData.ID)GetVoxel(metalAt).id)
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
				SetVoxel(metalPosition, (byte)oreData.VoxelID);
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

		RebuildNav();
	}

	private void FreeCollision()
	{
		if (meshInstance.HasNode("Mesh_col"))
			meshInstance.GetNode("Mesh_col").Free();
	}

	public void DeRender()
	{
		meshInstance.Mesh = null;
	}

	private SurfaceTool Populate(bool transparentPass)
	{
		SurfaceTool surfaceTool = new();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		surfaceTool.SetSmoothGroup(uint.MaxValue); // means -1

		surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RFloat);

		for (int x = 0; x < CHUNK_SIZE.X; x++)
		{
			for (int y = 0; y < CHUNK_SIZE.Y; y++)
			{
				for (int z = 0; z < CHUNK_SIZE.X; z++)
				{
					Vector3I voxelPosition = new(x, y, z);
					Voxel voxel = GetVoxel(voxelPosition);

					if (voxel.id <= 0) continue;

					bool isTransparent = VoxelData.DATA[voxel.id].Translucent;

					if (isTransparent && !transparentPass) continue;
					if (!isTransparent && transparentPass) continue;

					RebuildVoxel(voxel, surfaceTool, voxelPosition);
				}
			}
		}

		// BakeAO();

		// TODO: do we need tangents and normals...?
		// surfaceTool.GenerateNormals();
		// surfaceTool.GenerateTangents();
		return surfaceTool;
	}

	private void SetMesh(Mesh mesh)
	{
		meshInstance.Mesh = mesh;
		// meshInstance.CreateTrimeshCollision();

		// meshInstance.GetNode<StaticBody3D>("Mesh_col").SetCollisionLayerValue(2, true);
		// meshInstance.GetNode<StaticBody3D>("Mesh_col").SetCollisionMaskValue(2, true);
	}

	public void Rebuild()
	{
		// CallThreadSafe(MethodName.DeRender);

		ArrayMesh arrayMesh = new();

		arrayMesh = Populate(false).Commit(arrayMesh);
		arrayMesh = Populate(true).Commit(arrayMesh);

		// ShaderMaterial regularMaterial = new() { Shader = GD.Load<Shader>("res://assets/shaders/terrain.gdshader") };
		// regularMaterial.SetShaderParameter("tex", GD.Load<BaseMaterial3D>("res://assets/textures/terrain.png"));

		// ShaderMaterial transparentMaterial = new() { Shader = GD.Load<Shader>("res://assets/shaders/terrain.gdshader") };
		// transparentMaterial.SetShaderParameter("tex", GD.Load<BaseMaterial3D>("res://assets/textures/terrain.png"));
		// transparentMaterial.SetShaderParameter("allowTransparency", true);

		arrayMesh.SurfaceSetMaterial(0, regularMaterial);

		if (arrayMesh.GetSurfaceCount() > 1)
			arrayMesh.SurfaceSetMaterial(1, transparentMaterial);

		CallThreadSafe(MethodName.SetMesh, arrayMesh);
	}

	public void RebuildCollision()
	{
		// CallThreadSafe(MethodName.FreeCollision);
		// meshInstance.Mesh.CreateTrimeshShape();
	}
}
