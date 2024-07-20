using Godot;

namespace Game;

public enum VoxelIDs : uint
{
    VOID = 0,
    STONE = 1,
}

public struct Voxel
{
    public byte id;
    public byte light;
}
