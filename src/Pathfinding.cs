using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Game.Pathfinding;

public static class Pathfinder
{
    private static void PopulateAStar(ref AStar3D aStar)
    {
        List<(long, long)> connectionsMade = new();

        // adding points and connections of each chunk
        foreach (Chunk chunk in Find.World.GetAllChunks())
        {
            foreach (long point in chunk.navPoints)
            {
                DataPacking.UnpackData((ulong)point, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);
                aStar.AddPoint(point, new Vector3(voxelX * (chunkX + 1), voxelY, voxelZ * (chunkY + 1)));
            }

            foreach ((long point1, long point2) in chunk.navConnections)
            {
                aStar.ConnectPoints(point1, point2);
                if (Settings.ShowEvenMoreDebugDraw)
                    connectionsMade.Add((point1, point2));
            }
        }

        // sewing borders between chunks with connections
        // TODO: do better
        foreach (Chunk chunk in Find.World.GetAllChunks())
        {
            Chunk otherChunk;
            if (Find.World.HasChunk(chunk.ChunkPosition + Vector2I.Right))
            {
                otherChunk = Find.World.GetChunk(chunk.ChunkPosition + Vector2I.Right);
                int x = Chunk.CHUNK_SIZE.X - 1;

                for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE.X; z++)
                    {
                        long point1 = (long)DataPacking.PackData((byte)x, (byte)y, (byte)z, (short)chunk.ChunkPosition.X, (short)chunk.ChunkPosition.Y);

                        if (!aStar.HasPoint(point1)) continue;
                        long pointA; if (aStar.HasPoint(pointA = (long)DataPacking.PackData((byte)0, (byte)(y - 1), (byte)z, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointA); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointA));}
                        long pointB; if (aStar.HasPoint(pointB = (long)DataPacking.PackData((byte)0, (byte)y, (byte)z, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointB); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointB));}
                        long pointC; if (aStar.HasPoint(pointC = (long)DataPacking.PackData((byte)0, (byte)(y + 1), (byte)z, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointC); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointC));}
                    }
                }
            }
            if (Find.World.HasChunk(chunk.ChunkPosition + Vector2I.Left))
            {
                otherChunk = Find.World.GetChunk(chunk.ChunkPosition + Vector2I.Left);
                int x = 0;

                for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE.X; z++)
                    {
                        long point1 = (long)DataPacking.PackData((byte)x, (byte)y, (byte)z, (short)chunk.ChunkPosition.X, (short)chunk.ChunkPosition.Y);

                        if (!aStar.HasPoint(point1)) continue;
                        long pointA; if (aStar.HasPoint(pointA = (long)DataPacking.PackData((byte)(Chunk.CHUNK_SIZE.X - 1), (byte)(y - 1), (byte)z, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointA); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointA));}
                        long pointB; if (aStar.HasPoint(pointB = (long)DataPacking.PackData((byte)(Chunk.CHUNK_SIZE.X - 1), (byte)y, (byte)z, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointB); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointB));}
                        long pointC; if (aStar.HasPoint(pointC = (long)DataPacking.PackData((byte)(Chunk.CHUNK_SIZE.X - 1), (byte)(y + 1), (byte)z, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointC); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointC));}
                    }
                }
            }
            if (Find.World.HasChunk(chunk.ChunkPosition + Vector2I.Down))
            {
                otherChunk = Find.World.GetChunk(chunk.ChunkPosition + Vector2I.Down);
                int z = Chunk.CHUNK_SIZE.X - 1;

                for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                {
                    for (int x = 0; x < Chunk.CHUNK_SIZE.X; x++)
                    {
                        long point1 = (long)DataPacking.PackData((byte)x, (byte)y, (byte)z, (short)chunk.ChunkPosition.X, (short)chunk.ChunkPosition.Y);

                        if (!aStar.HasPoint(point1)) continue;
                        long pointA; if (aStar.HasPoint(pointA = (long)DataPacking.PackData((byte)x, (byte)(y - 1), (byte)0, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointA); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointA));}
                        long pointB; if (aStar.HasPoint(pointB = (long)DataPacking.PackData((byte)x, (byte)y, (byte)0, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointB); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointB));}
                        long pointC; if (aStar.HasPoint(pointC = (long)DataPacking.PackData((byte)x, (byte)(y + 1), (byte)0, (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointC); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointC));}
                    }
                }
            }
            if (Find.World.HasChunk(chunk.ChunkPosition + Vector2I.Up))
            {
                otherChunk = Find.World.GetChunk(chunk.ChunkPosition + Vector2I.Up);
                int z = 0;

                for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                {
                    for (int x = 0; x < Chunk.CHUNK_SIZE.X; x++)
                    {
                        long point1 = (long)DataPacking.PackData((byte)x, (byte)y, (byte)z, (short)chunk.ChunkPosition.X, (short)chunk.ChunkPosition.Y);

                        if (!aStar.HasPoint(point1)) continue;
                        long pointA; if (aStar.HasPoint(pointA = (long)DataPacking.PackData((byte)x, (byte)(y - 1), (byte)(Chunk.CHUNK_SIZE.X - 1), (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointA); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointA));}
                        long pointB; if (aStar.HasPoint(pointB = (long)DataPacking.PackData((byte)x, (byte)y, (byte)(Chunk.CHUNK_SIZE.X - 1), (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointB); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointB));}
                        long pointC; if (aStar.HasPoint(pointC = (long)DataPacking.PackData((byte)x, (byte)(y + 1), (byte)(Chunk.CHUNK_SIZE.X - 1), (short)otherChunk.ChunkPosition.X, (short)otherChunk.ChunkPosition.Y)))
                            {aStar.ConnectPoints(point1, pointC); if (Settings.ShowEvenMoreDebugDraw) connectionsMade.Add((point1, pointC));}
                    }
                }
            }
        }

        // debug draw for each connection
        if (!Settings.ShowDebugDraw) return;
        foreach ((long, long) connection in connectionsMade)
        {
            DataPacking.UnpackData((ulong)connection.Item1, out byte voxel1X, out byte voxel1Y, out byte voxel1Z, out short chunk1X, out short chunk1Y);
            DataPacking.UnpackData((ulong)connection.Item2, out byte voxel2X, out byte voxel2Y, out byte voxel2Z, out short chunk2X, out short chunk2Y);

            DebugDraw.Line(
                new Vector3(voxel1X + chunk1X * Chunk.CHUNK_SIZE.X + 0.5f, voxel1Y + 0.5f, voxel1Z + chunk1Y * Chunk.CHUNK_SIZE.X + 0.5f),
                new Vector3(voxel2X + chunk2X * Chunk.CHUNK_SIZE.X + 0.5f, voxel2Y + 0.5f, voxel2Z + chunk2Y * Chunk.CHUNK_SIZE.X + 0.5f),
                color: Colors.Red,
                duration: 15
            );
        }
    }

    public static (bool, Location[]) GetPath(Location a, Location b)
    {
        if ((!Chunk.IsVoxelInBounds(a.voxelPosition)) || (!Chunk.IsVoxelInBounds(b.voxelPosition))) return (false, null);

        // TODO: we only really need to repopulate it when a chunk changes
        AStar3D aStar = new();
        PopulateAStar(ref aStar);

        //////// TODO: we need ref for recursion (?) but then we need to know how long `points` and `voxelPositions` gonna be where GetPath used
        // Chunk fromChunk = thisWorld.GetChunk(from.chunkPosition);
        // if ((from.voxelPosition.Y < Chunk.CHUNK_SIZE.Y) && (fromChunk.voxels[from.voxelPosition.X][from.voxelPosition.Y - 1][from.voxelPosition.Z].id <= 0))
        //     return GetPath(new Location() { chunkPosition = from.chunkPosition, voxelPosition = new Vector3I(from.voxelPosition.X, from.voxelPosition.Y - 1, from.voxelPosition.Z) }, to, ref points, ref voxelPositions);
        // Chunk toChunk = thisWorld.GetChunk(to.chunkPosition);
        // if ((to.voxelPosition.Y < Chunk.CHUNK_SIZE.Y) && (toChunk.voxels[to.voxelPosition.X][to.voxelPosition.Y - 1][to.voxelPosition.Z].id <= 0))
        //     return GetPath(from, new Location() { chunkPosition = to.chunkPosition, voxelPosition = new Vector3I(to.voxelPosition.X, to.voxelPosition.Y - 1, to.voxelPosition.Z) }, ref points, ref voxelPositions);

        long point1 = (long)DataPacking.PackData((byte)a.voxelPosition.X, (byte)a.voxelPosition.Y, (byte)a.voxelPosition.Z, (short)a.chunkPosition.X, (short)a.chunkPosition.Y);
        long point2 = (long)DataPacking.PackData((byte)b.voxelPosition.X, (byte)b.voxelPosition.Y, (byte)b.voxelPosition.Z, (short)b.chunkPosition.X, (short)b.chunkPosition.Y);

        if ((!aStar.HasPoint(point1)) || (!aStar.HasPoint(point2))) return (false, null);
        // if (!aStar.ArePointsConnected(point1, point2)) return (false, null);

        long[] points = aStar.GetIdPath(point1, point2);

        Location[] locations = new Location[points.Length];
        foreach (var (value, i) in points.Select((value, i) => ( value, i )))
        {
            DataPacking.UnpackData((ulong)value, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);
            locations[i] = new Location() { chunkPosition = new(chunkX, chunkY), voxelPosition = new(voxelX, voxelY, voxelZ) };
        }

        if (Settings.ShowDebugDraw)
        {
            List<Vector3> linePositions = new(points.Length);
            foreach (long point in points)
            {
                DataPacking.UnpackData((ulong)point, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);
    
                Vector3 position = new(
                    voxelX + chunkX * Chunk.CHUNK_SIZE.X + 0.5f,
                    voxelY + 0.5f,
                    voxelZ + chunkY * Chunk.CHUNK_SIZE.X + 0.5f
                );
                linePositions.Add(position);
    
                DebugDraw.Sphere(
                    position,
                    radius: 0.25f,
                    color: Utils.GetRandomColor(),
                    drawSolid: true,
                    duration: 30
                );
            }

            DebugDraw.Lines(linePositions.ToArray(), color: Colors.Black, duration: 5);
        }

        return (true, locations);
    }
}