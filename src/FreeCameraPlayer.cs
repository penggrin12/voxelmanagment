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

    private Location agentStartLocation = new() { chunkPosition = Vector2I.Zero, voxelPosition = new Vector3I(0, 45, 0) };
    private Location agentEndLocation = new() { chunkPosition = Vector2I.Zero, voxelPosition = new Vector3I(3, 42, 0) };

    public override void _Ready()
    {
        camera = GetNode<Camera3D>("./Camera3D");
    }

    public override void _Process(double delta)
    {
        Find.DebugUi.Get<Label>("Position").Text = $"{camera.Position.X + (Chunk.CHUNK_SIZE.X / 2):n3}, {camera.Position.Y:n3}, {camera.Position.Z + (Chunk.CHUNK_SIZE.X / 2):n3} ( {Mathf.Wrap(camera.Position.X + (Chunk.CHUNK_SIZE.X / 2), 0, Chunk.CHUNK_SIZE.X):n3}, {Mathf.Wrap(camera.Position.Y, 0, Chunk.CHUNK_SIZE.Y):n3}, {Mathf.Wrap(camera.Position.Z + (Chunk.CHUNK_SIZE.X / 2), 0, Chunk.CHUNK_SIZE.X):n3} )";

        Vector2I chunkPosition = new(
            Mathf.FloorToInt(camera.GlobalPosition.X / Chunk.CHUNK_SIZE.X),
            Mathf.FloorToInt(camera.GlobalPosition.Z / Chunk.CHUNK_SIZE.X)
        );

        if (!world.HasChunk(chunkPosition))
            return; // not in a chunk (loading void..?)

		Chunk inChunk = world.GetChunk(chunkPosition);
		Vector3 cameraAt = camera.GlobalPosition;
		Vector3I voxelPosition = (Vector3I)(cameraAt - new Vector3(inChunk.ChunkPosition.X * Chunk.CHUNK_SIZE.X, 0, inChunk.ChunkPosition.Y * Chunk.CHUNK_SIZE.X).Floor());

		if (!Chunk.IsVoxelInBounds(voxelPosition))
		{
            // GD.Print("not in chunk");
            return;
        }

        if (Input.IsActionPressed("debug_agent_spawnpoint"))
		{
            agentStartLocation = new() { chunkPosition = chunkPosition, voxelPosition = voxelPosition };
		}

        if (Input.IsActionPressed("debug_agent_endpoint"))
		{
            agentEndLocation = new() { chunkPosition = chunkPosition, voxelPosition = voxelPosition };
		}

        if (Settings.ShowDebugDraw)
        {
            DebugDraw.Box(
                agentStartLocation.voxelPosition
                + new Vector3I(agentStartLocation.chunkPosition.X * Chunk.CHUNK_SIZE.X, 0, agentStartLocation.chunkPosition.Y * Chunk.CHUNK_SIZE.X)
                + new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(1, 1, 1),
                Colors.Green,
                drawSolid: true
            );

            DebugDraw.Box(
                agentEndLocation.voxelPosition
                + new Vector3I(agentEndLocation.chunkPosition.X * Chunk.CHUNK_SIZE.X, 0, agentEndLocation.chunkPosition.Y * Chunk.CHUNK_SIZE.X)
                + new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(1, 1, 1),
                Colors.Blue,
                drawSolid: true
            );
        }

        if (Input.IsActionJustPressed("debug_agent"))
        {
            AStar3D aStar = new();
            List<(long, long)> connectionsMade = new();

            foreach (Chunk chunk in world.GetAllChunks())
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

            foreach (Chunk chunk in world.GetAllChunks())
            {
                Chunk otherChunk;
                if (world.HasChunk(chunk.ChunkPosition + Vector2I.Right))
                {
                    otherChunk = world.GetChunk(chunk.ChunkPosition + Vector2I.Right);
                    int x = Chunk.CHUNK_SIZE.X - 1;

                    for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                    {
                        for (int z = 0; z < Chunk.CHUNK_SIZE.X; z++)
                        {
                            Vector3I thisVoxelPosition = new(x, y, z);

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
                if (world.HasChunk(chunk.ChunkPosition + Vector2I.Left))
                {
                    otherChunk = world.GetChunk(chunk.ChunkPosition + Vector2I.Left);
                    int x = 0;

                    for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                    {
                        for (int z = 0; z < Chunk.CHUNK_SIZE.X; z++)
                        {
                            Vector3I thisVoxelPosition = new(x, y, z);

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
                if (world.HasChunk(chunk.ChunkPosition + Vector2I.Down))
                {
                    otherChunk = world.GetChunk(chunk.ChunkPosition + Vector2I.Down);
                    int z = Chunk.CHUNK_SIZE.X - 1;

                    for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                    {
                        for (int x = 0; x < Chunk.CHUNK_SIZE.X; x++)
                        {
                            Vector3I thisVoxelPosition = new(x, y, z);

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
                if (world.HasChunk(chunk.ChunkPosition + Vector2I.Up))
                {
                    otherChunk = world.GetChunk(chunk.ChunkPosition + Vector2I.Up);
                    int z = 0;

                    for (int y = 0; y < Chunk.CHUNK_SIZE.Y; y++)
                    {
                        for (int x = 0; x < Chunk.CHUNK_SIZE.X; x++)
                        {
                            Vector3I thisVoxelPosition = new(x, y, z);

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

            bool GetPath(Location from, Location to, out long[] points, out Vector3I[] voxelPositions)
            {
                if (!Chunk.IsVoxelInBounds(from.voxelPosition)) {points = null; voxelPositions = null; return false;};
                if (!Chunk.IsVoxelInBounds(to.voxelPosition)) {points = null; voxelPositions = null; return false;};

                //////// TODO: we need ref for recursion (?) but then we need to know how long `points` and `voxelPositions` gonna be where GetPath used
                // Chunk fromChunk = thisWorld.GetChunk(from.chunkPosition);
                // if ((from.voxelPosition.Y < Chunk.CHUNK_SIZE.Y) && (fromChunk.voxels[from.voxelPosition.X][from.voxelPosition.Y - 1][from.voxelPosition.Z].id <= 0))
                //     return GetPath(new Location() { chunkPosition = from.chunkPosition, voxelPosition = new Vector3I(from.voxelPosition.X, from.voxelPosition.Y - 1, from.voxelPosition.Z) }, to, ref points, ref voxelPositions);
                // Chunk toChunk = thisWorld.GetChunk(to.chunkPosition);
                // if ((to.voxelPosition.Y < Chunk.CHUNK_SIZE.Y) && (toChunk.voxels[to.voxelPosition.X][to.voxelPosition.Y - 1][to.voxelPosition.Z].id <= 0))
                //     return GetPath(from, new Location() { chunkPosition = to.chunkPosition, voxelPosition = new Vector3I(to.voxelPosition.X, to.voxelPosition.Y - 1, to.voxelPosition.Z) }, ref points, ref voxelPositions);

                long point1 = (long)DataPacking.PackData((byte)from.voxelPosition.X, (byte)from.voxelPosition.Y, (byte)from.voxelPosition.Z, (short)from.chunkPosition.X, (short)from.chunkPosition.Y);
                long point2 = (long)DataPacking.PackData((byte)to.voxelPosition.X, (byte)to.voxelPosition.Y, (byte)to.voxelPosition.Z, (short)to.chunkPosition.X, (short)to.chunkPosition.Y);

                if ((!aStar.HasPoint(point1)) || (!aStar.HasPoint(point2))) {points = null; voxelPositions = null; return false;};
                
                points = aStar.GetIdPath(point1, point2);
                voxelPositions = new Vector3I[points.Length];
                
                foreach (var (value, i) in points.Select((value, i) => ( value, i )))
                    voxelPositions[i] = (Vector3I)aStar.GetPointPosition(value);
                
                return true;
            }

            if (!GetPath(agentStartLocation, agentEndLocation, out long[] points, out Vector3I[] voxels))
            {
                GD.Print("no path");
                return;
            }

            List<Vector3> linePositions = new();

            foreach (long point in points)
            {
                DataPacking.UnpackData((ulong)point, out byte voxelX, out byte voxelY, out byte voxelZ, out short chunkX, out short chunkY);

                Vector3 position = new(
                    voxelX + chunkX * Chunk.CHUNK_SIZE.X + 0.5f,
                    voxelY + 0.5f,
                    voxelZ + chunkY * Chunk.CHUNK_SIZE.X + 0.5f
                );
                linePositions.Add(position);

                if (Settings.ShowDebugDraw)
                {
                    DebugDraw.Sphere(
                        position,
                        radius: 0.25f,
                        color: Utils.GetRandomColor(),
                        drawSolid: true,
                        duration: 30
                    );
                }
            }

            if (Settings.ShowDebugDraw)
                DebugDraw.Lines(linePositions.ToArray(), color: Colors.Black, duration: 5);
        }
    }
}   