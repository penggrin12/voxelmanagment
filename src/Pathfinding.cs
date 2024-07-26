using Game.Structs;
using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Threading.Tasks;

namespace Game.Pathfinding;

public static class Pathfinder
{
    public static async Task<AStar3D> PopulateAStar()
    {
        var task = new Task<AStar3D>(() => { return _PopulateAStar(); });
        task.ContinueWith((task) => { throw task.Exception.InnerException; }, TaskContinuationOptions.OnlyOnFaulted);
        task.Start();

        return await task;
    }

    private static AStar3D _PopulateAStar()
    {
        AStar3D aStar = new();
        List<(long, long)> connectionsMade = new();

        // adding points and connections of each chunk
        foreach (Chunk chunk in Find.World.GetAllChunks())
        {
            foreach (long point in chunk.navPoints)
            {
                DataPacking.UnpackData((ulong)point, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);
                aStar.AddPoint(point, new Location() { chunkPosition = new(chunkX, chunkY), voxelPosition = new(voxelX, voxelY, voxelZ) }.GetGlobalPosition());
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
        if (!Settings.ShowDebugDraw) return aStar;
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

        return aStar;
    }

    public static (bool, Location[]) GetPath(AStar3D aStar, Location a, Location b)
    {
        if ((!Chunk.IsVoxelInBounds(a.voxelPosition)) || (!Chunk.IsVoxelInBounds(b.voxelPosition))) return (false, null);

        long point1 = (long)DataPacking.PackData((byte)a.voxelPosition.X, (byte)a.voxelPosition.Y, (byte)a.voxelPosition.Z, (short)a.chunkPosition.X, (short)a.chunkPosition.Y);
        long point2 = (long)DataPacking.PackData((byte)b.voxelPosition.X, (byte)b.voxelPosition.Y, (byte)b.voxelPosition.Z, (short)b.chunkPosition.X, (short)b.chunkPosition.Y);

        // TODO: add some kind of option to disable fuzzy pathfind
        float maxDist = 2.5f; // TODO: move this somewhere more appropriate

        if (!aStar.HasPoint(point1))
        {
            long closest = aStar.GetClosestPoint(a.GetGlobalPosition());
            DataPacking.UnpackData((ulong)closest, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);

            Location newA = new() { chunkPosition = new(chunkX, chunkY), voxelPosition = new(voxelX, voxelY, voxelZ) };
            float distToActualPoint = newA.GetGlobalPosition().DistanceTo(a.GetGlobalPosition());

            if (newA.GetGlobalPosition().DistanceTo(a.GetGlobalPosition()) > maxDist)
                return (false, null);

            GD.Print($"point1 a bit too far [{distToActualPoint}], but its fine: {newA}, {newA.GetGlobalPosition()}");
            if (Settings.ShowDebugDraw) DebugDraw.Point(newA.GetGlobalPosition() + new Vector3(0.5f, 0.5f, 0.5f), color: Colors.Cyan, duration: 15);

            return GetPath(aStar, newA, b);
        }

        if (!aStar.HasPoint(point2))
        {
            long closest = aStar.GetClosestPoint(b.GetGlobalPosition());
            DataPacking.UnpackData((ulong)closest, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);

            Location newB = new() { chunkPosition = new(chunkX, chunkY), voxelPosition = new(voxelX, voxelY, voxelZ) };
            float distToActualPoint = newB.GetGlobalPosition().DistanceTo(b.GetGlobalPosition());

            if (distToActualPoint > maxDist) return (false, null);

            GD.Print($"point2 a bit too far [{distToActualPoint}], but its fine: {newB}, {newB.GetGlobalPosition()}");
            if (Settings.ShowDebugDraw) DebugDraw.Point(newB.GetGlobalPosition() + new Vector3(0.5f, 0.5f, 0.5f), color: Colors.Crimson, duration: 15);

            return GetPath(aStar, a, newB);
        }

        // this always gives false for some reason
        // if (!aStar.ArePointsConnected(point1, point2)) return (false, null);

        long[] points = aStar.GetIdPath(point1, point2);

        Location[] locations = new Location[points.Length];
        foreach (var (value, i) in points.Select((value, i) => ( value, i )))
        {
            DataPacking.UnpackData((ulong)value, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);
            locations[i] = new Location() { chunkPosition = new(chunkX, chunkY), voxelPosition = new(voxelX, voxelY, voxelZ) };
        }

        return (true, locations);
    }
}