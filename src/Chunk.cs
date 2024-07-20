using Godot;
using System.Collections.Generic;

namespace Game;

public partial class Chunk : Node3D
{
	[Export] public Vector2I ChunkPosition = Vector2I.Zero;
	public Voxel[][][] voxels;

	private MeshInstance3D meshInstance;
	private SurfaceTool surfaceTool;

    private static readonly byte[] TEXTURE_LOOKUP = new byte[]
	{
		0, // not rendered
		6, 3, 0
	};

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
		// Rebuild();
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

	public bool IsVoxelInChunk(Vector3I position)
	{	
		return IsVoxelInBounds(position) && GetVoxel(position).id > 0;
	}

	private void RebuildVoxel(Voxel voxel, Vector3I voxelPosition)
	{
		Vector3I realPosition = new(voxelPosition.X + (ChunkPosition.X * CHUNK_SIZE.X), voxelPosition.Y, voxelPosition.Z + (ChunkPosition.Y * CHUNK_SIZE.X));
		Vector2I textureAtlasOffset = IndexToVector(TEXTURE_LOOKUP[voxel.id]);

		// GD.Print(textureAtlasOffset);

		if (!IsVoxelInChunk(voxelPosition + FRONT.normal))
			RebuildSide(realPosition, FRONT, textureAtlasOffset, voxel.light);
		if (!IsVoxelInChunk(voxelPosition + BACK.normal))
			RebuildSide(realPosition, BACK, textureAtlasOffset, voxel.light);
		
		if (!IsVoxelInChunk(voxelPosition + RIGHT.normal))
			RebuildSide(realPosition, RIGHT, textureAtlasOffset, voxel.light);
		if (!IsVoxelInChunk(voxelPosition + LEFT.normal))
			RebuildSide(realPosition, LEFT, textureAtlasOffset, voxel.light);

		if (!IsVoxelInChunk(voxelPosition + BOTTOM.normal))
			RebuildSide(realPosition, BOTTOM, textureAtlasOffset, voxel.light);
		if (!IsVoxelInChunk(voxelPosition + TOP.normal))
			RebuildSide(realPosition, TOP, textureAtlasOffset, voxel.light);
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

		// float light = GD.Randf();
		// light = ((Vector3)side.normal).Dot(Vector3.Up) > 0.5 ? (byte)255 : (byte)140;

		if (side.normal == Vector3I.Up) light = 255;
		else if (side.normal == Vector3I.Back) light = 180;
		else if (side.normal == Vector3I.Right) light = 165;
		else light = 135;

		// for (int i = 0; i < 3; i++)
		surfaceTool.SetCustom(0, new Color((float)light / byte.MaxValue, 0, 0));
		surfaceTool.AddTriangleFan(new Vector3[] { b, c, a }, new Vector2[] { uvB, uvC, uvA	});	
		// for (int i = 0; i < 3; i++)
		surfaceTool.SetCustom(0, new Color((float)light / byte.MaxValue, 0, 0));
		surfaceTool.AddTriangleFan(new Vector3[] { b, d, c }, new Vector2[] { uvB, uvD, uvC });

		// GetTree().Quit();
		// return;
	}

	// private int BoolToInt(bool x)
	// {
	// 	return x ? 1 : 0;
	// }

	// private int VertexAO(bool side1, bool side2, bool corner) {
	// 	if(side1 && side2) {
	// 		return 0;
	// 	}
	// 	return 3 - (BoolToInt(side1) + BoolToInt(side2) + BoolToInt(corner));
	// }

	// public void BakeAO()
	// {
	// 	for (int x = 0; x < CHUNK_SIZE.X; x++)
	// 	{
	// 		for (int y = 0; y < CHUNK_SIZE.Y; y++)
	// 		{
	// 			for (int z = 0; z < CHUNK_SIZE.X; z++)
	// 			{
	// 			}
	// 		}
	// 	}
	// }

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
        FastNoiseLite noise = new()
        {
            Seed = 356,
            Offset = new Vector3(ChunkPosition.X * CHUNK_SIZE.X, ChunkPosition.Y * CHUNK_SIZE.X, 0)
        };

        // GD.Print(noise.Offset);

		for (int x = 0; x < CHUNK_SIZE.X; x++)
		{
			for (int z = 0; z < CHUNK_SIZE.X; z++)
			{
				int colHeight = (int)(((noise.GetNoise2D(x, z) + 1f) / 2f) * CHUNK_SIZE.Y);

				for (int y = 0; y < CHUNK_SIZE.Y; y++)
				{
					if (colHeight >= y)
						SetVoxel(new Vector3I(x, y, z), 1);
						// voxels[x][y][z] = new Voxel() {id = 1, position = new Vector3I(x, y, z)};
				}
			}
		}
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
		surfaceTool.SetSmoothGroup(uint.MaxValue); // uint.MaxValue means -1

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
		// surfaceTool.GenerateTangents();
		surfaceTool.Commit(arrayMesh);
		meshInstance.Mesh = arrayMesh;
		meshInstance.CreateTrimeshCollision();

		meshInstance.GetNode<StaticBody3D>("Mesh_col").SetCollisionLayerValue(2, true);
		meshInstance.GetNode<StaticBody3D>("Mesh_col").SetCollisionMaskValue(2, true);
	}
}