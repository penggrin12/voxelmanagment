using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;

namespace Game;

public partial class FreeCameraPlayer : BasePlayer
{
    private Camera3D camera;
    // private Vector3 drawBoxAt = Vector3.Zero;

    private Location agentStartLocation = new();
    private Location agentEndLocation = new();

    public override void _Ready()
    {
        camera = GetNode<Camera3D>("./Camera3D");
    }

    public override void _Process(double delta)
    {
        HandleUpdateRenderDistance(camera.Position);

        Vector2I chunkPosition = new(
            Mathf.FloorToInt(camera.GlobalPosition.X / Chunk.CHUNK_SIZE.X),
            Mathf.FloorToInt(camera.GlobalPosition.Z / Chunk.CHUNK_SIZE.X)
        );
        Vector3I voxelPosition = new(
            Mathf.Wrap((int)camera.GlobalPosition.X, 0, Chunk.CHUNK_SIZE.X),
            Mathf.Wrap((int)camera.GlobalPosition.Y, 0, Chunk.CHUNK_SIZE.Y),
            Mathf.Wrap((int)camera.GlobalPosition.Z, 0, Chunk.CHUNK_SIZE.X)
        );

        if (Input.IsActionPressed("debug_agent_spawnpoint"))
		{
            agentStartLocation = new() { chunkPosition = chunkPosition, voxelPosition = voxelPosition };
		}

        if (Input.IsActionPressed("debug_agent_endpoint"))
		{
            agentEndLocation = new() { chunkPosition = chunkPosition, voxelPosition = voxelPosition };
		}

        DebugDraw.Box(
            (agentStartLocation.voxelPosition * new Vector3I(agentStartLocation.chunkPosition.X + 1, 1, agentStartLocation.chunkPosition.Y + 1)) + new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(1, 1, 1),
            Colors.Green
        );

        DebugDraw.Box(
            (agentEndLocation.voxelPosition * new Vector3I(agentEndLocation.chunkPosition.X + 1, 1, agentEndLocation.chunkPosition.Y + 1)) + new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(1, 1, 1),
            Colors.Blue
        );

        if (Input.IsActionJustPressed("debug_agent"))
        {
            AStar3D aStar = new();

            Chunk thisChunk = Find.World.GetChunk(Vector2I.Zero);

            for (byte x = 0; x < Chunk.CHUNK_SIZE.X; x++)
            {
                for (byte z = 0; z < Chunk.CHUNK_SIZE.X; z++)
                {
                    for (byte y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                    {
                        if (y + 1 >= Chunk.CHUNK_SIZE.Y) continue; // TODO too high dont allow
                        if (y - 1 < 0) continue; // too low dont allow

                        if (thisChunk.voxels[x][y + 1][z].id > 0) continue; // voxel above is air
                        if (thisChunk.voxels[x][y][z].id > 0) continue; // this voxel is air
                        if (thisChunk.voxels[x][y - 1][z].id <= 0) continue; // voxel below is solid

                        Vector3I pos = new(x, y, z);
                        aStar.AddPoint((long)DataPacking.PackData(x, y, z, (short)thisChunk.ChunkPosition.X, (short)thisChunk.ChunkPosition.Y), pos, 1);

                        // DebugDraw.Sphere(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), 0.15f, Utils.GetRandomColor(), 30, true);
                    }
                }
            }

            GD.Print($"points: {aStar.GetPointCount()}");

            foreach (long pointId in aStar.GetPointIds())
            {
                Vector3 pos = aStar.GetPointPosition(pointId);
                // DebugDraw.Point(pos + new Vector3(0.5f, 0.5f, 0.5f), color: Colors.Black, duration: 30);

                DataPacking.UnpackData((ulong)pointId, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);
                Chunk thissChunk = Find.World.GetChunk(chunkX, chunkY);

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
                            if (!Chunk.IsVoxelInBounds(dest)) continue;
                            if ((dest.Y <= 0) || (dest.Y > Chunk.CHUNK_SIZE.Y)) continue;

                            long destPacked = (long)DataPacking.PackData((byte)dest.X, (byte)dest.Y, (byte)dest.Z, (short)thissChunk.ChunkPosition.X, (short)thissChunk.ChunkPosition.Y);

                            if (aStar.ArePointsConnected(pointId, destPacked)) continue;

                            if (thissChunk.voxels[dest.X][dest.Y][dest.Z].id > 0) continue;
                            if (thissChunk.voxels[dest.X][dest.Y - 1][dest.Z].id <= 0) continue;
                            if (thissChunk.voxels[dest.X][dest.Y + 1][dest.Z].id > 0) continue;

                            aStar.ConnectPoints(pointId, destPacked);

                            // DebugDraw.Line(
                            //     new Vector3(voxelOrigin.X + 0.5f, voxelOrigin.Y + 0.5f, voxelOrigin.Z + 0.5f),
                            //     new Vector3(voxelOrigin.X + 0.5f + x, voxelOrigin.Y + 0.5f + y, voxelOrigin.Z + 0.5f + z),
                            //     color: Colors.Red,
                            //     duration: 30
                            // );
                        }
                    }
                }
            }

            bool GetPath(Location from, Location to, out long[] points, out Vector3I[] voxelPositions)
            {
                if (!Chunk.IsVoxelInBounds(from.voxelPosition)) {points = null; voxelPositions = null; return false;};
                if (!Chunk.IsVoxelInBounds(to.voxelPosition)) {points = null; voxelPositions = null; return false;};

                long point1 = (long)DataPacking.PackData((byte)from.voxelPosition.X, (byte)from.voxelPosition.Y, (byte)from.voxelPosition.Z, (short)from.chunkPosition.X, (short)from.chunkPosition.Y);
                long point2 = (long)DataPacking.PackData((byte)to.voxelPosition.X, (byte)to.voxelPosition.Y, (byte)to.voxelPosition.Z, (short)to.chunkPosition.X, (short)to.chunkPosition.Y);

                if ((!aStar.HasPoint(point1)) || (!aStar.HasPoint(point2))) {points = null; voxelPositions = null; return false;};
                
                points = aStar.GetIdPath(point1, point2);
                voxelPositions = new Vector3I[points.Length];
                
                foreach  (var (value, i) in points.Select((value, i) => ( value, i )))
                    voxelPositions[i] = (Vector3I)aStar.GetPointPosition(value);
                
                return true;
            }

            // foreach (long point in aStar.GetPointIds())
            // {
            //     DataPacking.UnpackData((ulong)point, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);
            //     foreach (long pointTo in aStar.GetPointConnections(point))
            //     {
            //         DataPacking.UnpackData((ulong)pointTo, out byte voxel2X, out byte voxel2Y, out byte voxel2Z, out short chunk2X, out short chunk2Y);
            //         DebugDraw.Line(
            //             new Vector3(voxelX + 0.5f, voxelY + 0.5f, voxelZ + 0.5f),
            //             new Vector3(0.5f + voxel2X, 0.5f + voxel2Y, 0.5f + voxel2Z),
            //             color: Colors.Purple,
            //             duration: 30
            //         );
            //     }
            // }

            // foreach (long point in IsPathPossible(agentStartLocation, agentEndLocation))
            // {
            //     Vector3I pos = (Vector3I)aStar.GetPointPosition(point);
            //     // DebugDraw.Sphere(new Vector3(pos.X + 0.5f, pos.Y + 0.5f, pos.Z + 0.5f), 0.15f, Utils.GetRandomColor(), 30, true);
            //     // DebugDraw.Point(point, color: Colors.Blue, duration: 30);
            // }

            bool pathSuccess = GetPath(agentStartLocation, agentEndLocation, out long[] points, out Vector3I[] voxels);

            List<Vector3> lines = new();

            foreach (Vector3I item in voxels)
            {
                lines.Add(new Vector3(item.X + 0.5f, item.Y + 0.5f, item.Z + 0.5f));
            }

            DebugDraw.Lines(lines.ToArray(), color: Colors.Black, duration: 30);
        }
    }
}   