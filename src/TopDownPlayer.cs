using Game.Interfaces;
using Godot;

namespace Game.Entities;

public partial class TopDownPlayer : Node3D, IPlayer
{
	[Export] private Camera3D camera;
	[Export] private RayCast3D ray;

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

    public object GetDebugThingie()
    {
        throw new System.NotImplementedException();
    }
}
