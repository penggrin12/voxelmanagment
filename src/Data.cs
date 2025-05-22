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

    public readonly record struct Data(byte Strongness, byte[] Texture_lookup, bool Translucent, bool Solid);
    public static readonly Data[] DATA = [
        new(Strongness: 0  , Texture_lookup: [0 , 0 , 0 , 0 , 0 , 0 ], Translucent: true,  Solid: false),
        new(Strongness: 255, Texture_lookup: [0 , 0 , 0 , 0 , 0 , 0 ], Translucent: false, Solid: true ),
        new(Strongness: 30 , Texture_lookup: [1 , 1 , 1 , 1 , 1 , 1 ], Translucent: false, Solid: true ),
        new(Strongness: 5  , Texture_lookup: [2 , 2 , 2 , 2 , 2 , 2 ], Translucent: false, Solid: true ),
        new(Strongness: 4  , Texture_lookup: [4 , 4 , 4 , 4 , 2 , 3 ], Translucent: false, Solid: true ),
        new(Strongness: 20 , Texture_lookup: [5 , 5 , 5 , 5 , 5 , 5 ], Translucent: false, Solid: true ),
        new(Strongness: 40 , Texture_lookup: [6 , 6 , 6 , 6 , 6 , 6 ], Translucent: false, Solid: true ),
        new(Strongness: 100, Texture_lookup: [7 , 7 , 7 , 7 , 7 , 7 ], Translucent: false, Solid: true ),
        new(Strongness: 70 , Texture_lookup: [8 , 8 , 8 , 8 , 8 , 8 ], Translucent: false, Solid: true ),
        new(Strongness: 70 , Texture_lookup: [9 , 9 , 9 , 9 , 9 , 9 ], Translucent: false, Solid: true ),
        new(Strongness: 70 , Texture_lookup: [10, 10, 10, 10, 10, 10], Translucent: true,  Solid: false),
    ];
}

public static class OresData
{
    public enum ID : byte
    {
        NONE = 0,
        METAL,
        COAL,
    }

    public readonly record struct Data(bool Breakable, VoxelData.ID VoxelID, Vector2I YRange, Dictionary<int, float> Weights);
    public static readonly Dictionary<ID, Data> data = new() {
        { ID.NONE, new() }, // i don wanna mark this as nullable
        { ID.METAL, new(Breakable: true, VoxelID: VoxelData.ID.METAL_ORE, YRange: new(1, 27), Weights: new() { { 3, 0.50f }, { 4, 0.45f }, { 5, 0.30f }, { 6, 0.15f }, { 7, 0.05f } }) },
        { ID.COAL, new(Breakable: true, VoxelID: VoxelData.ID.COAL_ORE, YRange: new(1, 27), Weights: new() { { 23, 0.50f }, { 24, 0.45f }, { 25, 0.30f }, { 26, 0.15f }, { 27, 0.05f } }) },
    };
}
