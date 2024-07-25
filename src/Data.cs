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
        PLANKS,
        BRICKS,
        METAL_ORE,
        COAL_ORE,
        SAND,
        WATER,
    }

    public struct Data
    {
        public byte strongness;
        public byte[] texture_lookup; // front, back,    right, left,    bottom, top
    }

    public static bool IsTransparent(byte voxelID)
    {
        return voxelID switch
        {
            (byte)ID.VOID => true,
            (byte)ID.WATER => true,
            _ => false,
        };
    }

    public static readonly Dictionary<byte, Data> DATA = new() {
        { (byte)ID.VOID,          new() { strongness = 0  , texture_lookup = new byte[6] { 0  , 0  , 0  , 0  , 0  , 0   } } },
        { (byte)ID.HARDSTONE,     new() { strongness = 255, texture_lookup = new byte[6] { 0  , 0  , 0  , 0  , 0  , 0   } } },
        { (byte)ID.STONE,         new() { strongness = 30 , texture_lookup = new byte[6] { 1  , 1  , 1  , 1  , 1  , 1   } } },
        { (byte)ID.DIRT,          new() { strongness = 5  , texture_lookup = new byte[6] { 2  , 2  , 2  , 2  , 2  , 2   } } },
        { (byte)ID.GRASS,         new() { strongness = 4  , texture_lookup = new byte[6] { 4  , 4  , 4  , 4  , 2  , 3   } } },
        { (byte)ID.PLANKS,        new() { strongness = 20 , texture_lookup = new byte[6] { 5  , 5  , 5  , 5  , 5  , 5   } } },
        { (byte)ID.BRICKS,        new() { strongness = 40 , texture_lookup = new byte[6] { 6  , 6  , 6  , 6  , 6  , 6   } } },
        { (byte)ID.METAL_ORE,     new() { strongness = 100, texture_lookup = new byte[6] { 7  , 7  , 7  , 7  , 7  , 7   } } },
        { (byte)ID.COAL_ORE,      new() { strongness = 70 , texture_lookup = new byte[6] { 8  , 8  , 8  , 8  , 8  , 8   } } },
        { (byte)ID.SAND,          new() { strongness = 70 , texture_lookup = new byte[6] { 9  , 9  , 9  , 9  , 9  , 9   } } },
        { (byte)ID.WATER,         new() { strongness = 70 , texture_lookup = new byte[6] { 10 , 10 , 10 , 10 , 10 , 10  } } },
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
