using Godot;

namespace Game.Structs;

public struct Voxel
{
    public byte id;
    public byte light;
}

public struct Location
{
    public Vector2I chunkPosition;
    public Vector3I voxelPosition;

    public override readonly string ToString() { return $"[{voxelPosition} in {chunkPosition}]"; }
    public readonly Vector3I GetGlobalPosition() { return new(voxelPosition.X + (chunkPosition.X * Chunk.CHUNK_SIZE.X), voxelPosition.Y, voxelPosition.Z + (chunkPosition.Y * Chunk.CHUNK_SIZE.X)); }
    public static Location FromGlobalPosition(Vector3I position)
    {
        return new Location()
        {
            chunkPosition = new Vector2I(position.X / Chunk.CHUNK_SIZE.X, position.Z / Chunk.CHUNK_SIZE.X),
            voxelPosition = new Vector3I(position.X % Chunk.CHUNK_SIZE.X, position.Y, position.Z % Chunk.CHUNK_SIZE.X)
        };
    }
}