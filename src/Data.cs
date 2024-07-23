using System.Collections.Generic;
using Godot;

namespace Game;

public static class VoxelData
{
    public enum ID : byte
    {
        VOID = 0,
        HARDSTONE,
        STONE,
        DIRT,
        GRASS,
        GRASS_SIDE,
        PLANKS,
        BRICKS,
        METAL_ORE,
        COAL_ORE,
    }

    public struct Data
    {
        public byte strongness;
    }

    public static readonly Dictionary<ID, Data> data = new() {
        { ID.VOID,          new() { strongness = byte.MinValue } },
        { ID.HARDSTONE,     new() { strongness = byte.MaxValue } },
        { ID.STONE,         new() { strongness = 30 } },
        { ID.DIRT,          new() { strongness = 5 } },
        { ID.GRASS,         new() { strongness = 4 } },
        { ID.GRASS_SIDE,    new() { strongness = 4 } },
        { ID.PLANKS,        new() { strongness = 20 } },
        { ID.BRICKS,        new() { strongness = 40} },
        { ID.METAL_ORE,     new() { strongness = 100 } },
        { ID.COAL_ORE,      new() { strongness = 70 } },
    };
}

public static class OresData
{
    public enum ID : byte
    {
        NONE = 0,
        METAL,
        COAL,
    }

    public struct Data
    {
        public bool breakable;
        public VoxelData.ID voxelID;
        public Vector2I YRange;
        public Dictionary<int, float> weights;
    }

    public static readonly Dictionary<ID, Data> data = new() {
        { ID.NONE, new() }, // i don wanna mark this as nullable
        { ID.METAL, new() { breakable = true, voxelID = VoxelData.ID.METAL_ORE, YRange = new(1, 27), weights = new() { {3, 0.50f}, {4, 0.45f}, {5, 0.30f}, {6, 0.15f}, {7, 0.05f} } } },
        { ID.COAL, new() { breakable = true, voxelID = VoxelData.ID.COAL_ORE, YRange = new(1, 27), weights = new() { {23, 0.50f}, {24, 0.45f}, {25, 0.30f}, {26, 0.15f}, {27, 0.05f} } } },
    };
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
