using Game.Interfaces;
using Godot;
using System;

namespace Game;

public partial class TopDownPlayer : Node3D, IPlayer
{
	private World world;

	[Export] private Camera3D camera;
	[Export] private RayCast3D ray;

	public void SetWorld(World world)
	{
		this.world = world;
	}

	public Node3D AsNode3D()
    {
        return this;
    }

	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
		if (ray.IsColliding())
		{
			camera.GlobalPosition = ray.GetCollisionPoint();
			camera.GlobalPosition -= camera.GlobalTransform.Basis.Z * 4f;
		}
	}
}
