using Godot;
using System;

namespace Game;

public partial class Player : BasePlayer
{
	[ExportGroup("Links")]
	[Export] private Node3D head;
	[Export] private CollisionShape3D collision;
	[Export] private RayCast3D ray;
	[Export] private PackedScene voxelBreakDecoration;

	[ExportGroup("Camera")]
	[Export] private float cameraSensitivity = 0.05f;

	[ExportGroup("Movement")]
	[Export] private bool allowBunnyHoping = false;
	[Export] private bool autoBunnyHoping = false;
	[Export] private float maxVelocityAir = 0.6f;
	[Export] private float maxVelocityGround = 6.0f;
	[Export] private float maxAcceleration = 60f;
	[Export] private float gravity = 25f;
	[Export] private float stopSpeed = 8f;
	[Export] private float jumpImpulse = 5;
	private float _jumpImpulse;
	
	private Vector3 moveInput = Vector3.Zero;
	private float friction = 4f;
	private bool wishJump = false;

	public override void _Ready()
	{
		_jumpImpulse = MathF.Sqrt(jumpImpulse * gravity * 0.85f);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Process(double delta)
	{
		HandleInput();
		HandleUpdateRenderDistance(Position);
		WalkMove((float)delta);
	
		if (!Find.DebugUi.Enabled) return;

        Find.DebugUi.Get<Label>("Position").Text = $"{Position.X:n3}, {Position.Y:n3}, {Position.Z:n3} ( {Mathf.Wrap(Position.X, 0, Chunk.CHUNK_SIZE.X):n3}, {Mathf.Wrap(Position.Y, 0, Chunk.CHUNK_SIZE.Y):n3}, {Mathf.Wrap(Position.Z, 0, Chunk.CHUNK_SIZE.X):n3} )";
	}

    public override void _PhysicsProcess(double delta)
    {
        collision.GlobalRotation = Vector3.Zero;
    }

    private void CameraRotation(Vector2 relative)
	{
		RotateY(Mathf.DegToRad(-relative.X * cameraSensitivity));
		head.RotateX(Mathf.DegToRad(-relative.Y * cameraSensitivity));

		head.Rotation = new Vector3(Mathf.Clamp(head.Rotation.X, -1.5f, 1.5f), head.Rotation.Y, head.Rotation.Z);
	}

	private void HandleInput()
	{
		moveInput = Vector3.Zero;
		Vector2 inputVector = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		moveInput += (GlobalTransform.Basis.X * inputVector.X) + (GlobalTransform.Basis.Z * inputVector.Y);
		wishJump = allowBunnyHoping ? Input.IsActionPressed("jump") : Input.IsActionJustPressed("jump");

		if (!(Input.IsActionJustPressed("break") || Input.IsActionJustPressed("place"))) return;
		if (!ray.IsColliding()) return;

		Node3D collidingWith = (Node3D)ray.GetCollider();

		if (collidingWith == null) return; // shouldnt happen..?

		Node collidingWithParent = collidingWith.GetNode("../..");

		if (collidingWithParent is not Chunk) return;

		Chunk chunk = collidingWithParent as Chunk;
		Vector3 collisionNormal = ray.GetCollisionNormal();
		Vector3 collisionAt = ray.GetCollisionPoint();
		Vector3I voxelPosition = (Vector3I)(collisionAt - collisionNormal * 0.05f - new Vector3(chunk.ChunkPosition.X * Chunk.CHUNK_SIZE.X, 0, chunk.ChunkPosition.Y * Chunk.CHUNK_SIZE.X).Floor());

		if (!chunk.IsVoxelInChunk(voxelPosition))
			return;

		if (Input.IsActionJustPressed("break"))
		{	
			chunk.SetVoxel(voxelPosition, 0);
			chunk.Rebuild();

			Node3D breakDecoration = voxelBreakDecoration.Instantiate<Node3D>();
			chunk.AddChild(breakDecoration);
			CpuParticles3D particles = breakDecoration.GetNode<CpuParticles3D>("Particles");
			particles.Position = voxelPosition + new Vector3(0.5f, 0.5f, 0.5f) + new Vector3(chunk.ChunkPosition.X * Chunk.CHUNK_SIZE.X, 0, chunk.ChunkPosition.Y * Chunk.CHUNK_SIZE.X);
			particles.Emitting = true;
			particles.Finished += () => {
				particles.QueueFree();
			};
		}

		if (Input.IsActionJustPressed("place"))
		{
			Vector3I newVoxelPosition = voxelPosition + (Vector3I)collisionNormal;

			// TODO: fuck math, me and my homies use:
			if (newVoxelPosition.X < 0) return;
			if (newVoxelPosition.Z < 0) return;

			if (newVoxelPosition.X >= Chunk.CHUNK_SIZE.X) return;
			if (newVoxelPosition.Z >= Chunk.CHUNK_SIZE.X) return;
			//

			chunk.SetVoxel(newVoxelPosition, 2);
			chunk.Rebuild();
		}
	}

	private void WalkMove(float delta)
	{
		if (collision.Disabled)
			collision.Disabled = false;

		Vector3 velocity = Velocity;
		Vector3 wishDir = moveInput.Normalized();

		if (IsOnFloor())
		{
			if (!allowBunnyHoping)
			{
				// This heavily limits bunny hop
				if (wishJump)
					velocity.Y = jumpImpulse;
					Velocity = velocity;
				
				Velocity = UpdateVelocityGround(wishDir, delta);
			}
			else
			{
				// This doesnt
				if (wishJump)
				{
					velocity.Y = jumpImpulse;
					Velocity = velocity;
					Velocity = UpdateVelocityAir(wishDir, delta);
				}
				else
				{
					Velocity = UpdateVelocityGround(wishDir, delta);
				}
			}
		}
		else
		{
			velocity.Y -= gravity * delta;
			Velocity = velocity;

			Velocity = UpdateVelocityAir(wishDir, delta);
		}

		Vector3 horizontalVelocity = new(Velocity.X, 0, Velocity.Z);

		if (horizontalVelocity.Abs().Length() > 7f)
		{
			Vector3 forward = GlobalTransform.Basis.Z;
			Velocity += forward * (horizontalVelocity.Abs().Length() - (7f - 1f)) * delta;
		}

		MoveAndSlide();
	}

	private Vector3 Accelerate(Vector3 wishDir, float maxSpeed, float delta)
	{
		float currentSpeed = Velocity.Dot(wishDir);
		float addSpeed = Mathf.Clamp(maxSpeed - currentSpeed, 0, maxAcceleration * delta);

		return Velocity + addSpeed * wishDir;
	}

	private Vector3 UpdateVelocityGround(Vector3 wishDir, float delta)
	{
		float speed = Velocity.Length();

		if (!Mathf.IsZeroApprox(speed))
		{
			float control = Mathf.Max(stopSpeed, speed);
			float drop = control * friction * delta;

			Velocity *= Mathf.Max(speed - drop, 0f) / speed;
		}

		return Accelerate(wishDir, maxVelocityGround, delta);
	}

	private Vector3 UpdateVelocityAir(Vector3 wishDir, float delta)
	{
		return Accelerate(wishDir, maxVelocityAir, delta);
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("exit"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
				Input.MouseMode = Input.MouseModeEnum.Visible;
			else
				GetTree().Quit();
		}

		if (@event is InputEventMouseButton)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
			CameraRotation(mouseMotion.Relative);
	}
}
