using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Interfaces;
using Game.Pathfinding;
using Game.Structs;
using Godot;

namespace Game.Entities;

public partial class TestControllableEntity : CharacterBody3D, IPathfinding, IPlayerControllableEntity
{
	private Vector3 movingTo;

    public override void _Ready()
    {
        movingTo = GlobalPosition;
    }

    public override void _Process(double delta)
    {
        GlobalPosition = GlobalPosition.MoveToward(movingTo, (float)delta);
    }

    public void OnPlayerSelect()
	{
		MeshInstance3D meshInstance = GetNode<MeshInstance3D>("Mesh");
		meshInstance.Mesh = (Mesh)meshInstance.Mesh.Duplicate();
		StandardMaterial3D material = (StandardMaterial3D)meshInstance.Mesh.SurfaceGetMaterial(0).Duplicate();
		material.AlbedoColor = Colors.AliceBlue;
		meshInstance.Mesh.SurfaceSetMaterial(0, material);
	}

    public async void PlayerCommandMove(Location to)
    {
        (bool, Location[]) path = Pathfinder.GetPath(Find.World.AStar, this.GetLocation(), to);

		if (!path.Item1) {GD.Print($"no path for {GetType()}"); return;}

		if (Settings.ShowDebugDraw)
        {
            List<Vector3> linePositions = new(path.Item2.Length);
            Color thisColor = Utils.GetRandomColor();
            foreach (Location location in path.Item2)
            {
                Vector3 position = location.GetGlobalPosition() + new Vector3(0.5f, 0.5f, 0.5f);
                linePositions.Add(position);
    
                DebugDraw.Sphere(
                    position,
                    radius: 0.20f,
                    color: thisColor,
                    drawSolid: true,
                    duration: 15
                );
            }

            DebugDraw.Lines(linePositions.ToArray(), color: Colors.Black, duration: 15);
        }

		foreach (Location location in path.Item2)
		{
			movingTo = location.GetGlobalPosition() + new Vector3(0.5f, 0, 0.5f);
			await Task.Delay(1000);
		}
    }
}
