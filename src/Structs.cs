using Godot;

namespace Game;

public enum VoxelID : byte
{
    VOID = 0,
    HARDSTONE,
    STONE,
    DIRT,
    GRASS,
    GRASS_SIDE,
    PLANKS,
    BRICKS,
}

public struct Voxel
{
    public byte id;
    public byte light;
}

public struct Location
{
    public Vector2I chunkPosition;
    public Vector3I voxelPosition;
}
