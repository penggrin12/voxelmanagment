using Godot;
using Game.Interfaces;
using Game.Structs;

namespace Game.Entities;

public partial class FreeCameraPlayer : Node3D, IPlayer
{
	private Camera3D camera;
	private RayCast3D ray;

	private IPlayerSelectableEntity selectedEntity;

	public object GetDebugThingie()
	{
		return null;
	}

	public override void _Ready()
	{
		camera = GetNode<Camera3D>("./Camera");
		ray = camera.GetNode<RayCast3D>("./Ray");
	}

	public override void _Process(double delta)
	{
		Find.DebugUi.Get<Label>("Position").Text = $"{camera.Position.X + (Chunk.CHUNK_SIZE.X / 2):n3}, {camera.Position.Y:n3}, {camera.Position.Z + (Chunk.CHUNK_SIZE.X / 2):n3} ( {Mathf.Wrap(camera.Position.X + (Chunk.CHUNK_SIZE.X / 2), 0, Chunk.CHUNK_SIZE.X):n3}, {Mathf.Wrap(camera.Position.Y, 0, Chunk.CHUNK_SIZE.Y):n3}, {Mathf.Wrap(camera.Position.Z + (Chunk.CHUNK_SIZE.X / 2), 0, Chunk.CHUNK_SIZE.X):n3} )";

		ray.TargetPosition = GetViewport().GetCamera3D().ProjectLocalRayNormal(GetViewport().GetMousePosition()) * 150;
		ray.ForceRaycastUpdate();

		if (!ray.IsColliding()) return;

		if (Input.IsActionJustPressed("command_entity_move"))
		{
			Node collidingWithParent = (ray.GetCollider() as Node3D).GetNode("../..");
			if (collidingWithParent is not Chunk) return;

			GD.Print("1");
			GD.Print(selectedEntity.GetType());

			if (selectedEntity is IPlayerControllableEntity)
			{
				GD.Print("2");
				Chunk chunk = collidingWithParent as Chunk;
				Vector3 collisionNormal = ray.GetCollisionNormal();
				Vector3 collisionAt = ray.GetCollisionPoint();
				Vector3I voxelPosition = (Vector3I)(collisionAt - (collisionNormal * 0.05f) - new Vector3(chunk.ChunkPosition.X * Chunk.CHUNK_SIZE.X, 0, chunk.ChunkPosition.Y * Chunk.CHUNK_SIZE.X).Floor());

				(selectedEntity as IPlayerControllableEntity).PlayerCommandMove(new Location() { chunkPosition = chunk.ChunkPosition, voxelPosition = voxelPosition });
			}
		}

		if (ray.GetCollider() is not IPlayerSelectableEntity) return;

		if (Input.IsActionJustPressed("select_entity"))
		{
			selectedEntity = ray.GetCollider() as IPlayerSelectableEntity;
			(ray.GetCollider() as IPlayerSelectableEntity).OnPlayerSelect();
		}
	}
}
