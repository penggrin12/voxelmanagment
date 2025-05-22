using Godot;
using Godot.NativeInterop;

namespace Game;

public partial class FreeCamera : Camera3D
{
	private const float SHIFT_MULTIPLIER = 2.5f;
	private const float ALT_MULTIPLIER = 1.0f / SHIFT_MULTIPLIER;

	[Export(PropertyHint.Range, "0,1")] private float sensitivity = 0.25f;

	private Vector2 mousePosition = Vector2.Zero;
	private float totalPitch = 0.0f;

	private Vector3 direction = Vector3.Zero;
	private Vector3 velocity = Vector3.Zero;
	private float acceleration = 30;
	private float deceleration = -10;
	private float velMultiplier = 4;

	private bool w = false;
	private bool s = false;
	private bool a = false;
	private bool d = false;
	private bool q = false;
	private bool e = false;
	private bool shift = false;
	private bool alt = false;

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			mousePosition = mouseMotion.Relative;
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			switch (mouseButton.ButtonIndex)
			{
				case MouseButton.Right:
					Input.MouseMode = mouseButton.Pressed ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
					break;
				case MouseButton.WheelUp:
					velMultiplier = Mathf.Clamp(velMultiplier * 1.1f, 0.2f, 20f);
					break;
				case MouseButton.WheelDown:
					velMultiplier = Mathf.Clamp(velMultiplier / 1.1f, 0.2f, 20f);
					break;
				default:
					break;
			}
		}

		if (@event is InputEventKey key)
		{
			switch (key.Keycode)
			{
				case Key.W:
					w = key.Pressed; break;
				case Key.S:
					s = key.Pressed; break;
				case Key.A:
					a = key.Pressed; break;
				case Key.D:
					d = key.Pressed; break;
				case Key.Q:
					q = key.Pressed; break;
				case Key.E:
					e = key.Pressed; break;
				case Key.Shift:
					shift = key.Pressed; break;
				case Key.Alt:
					alt = key.Pressed; break;
			}
		}
	}

	public override void _Process(double delta)
	{
		UpdateMouselook();
		UpdateMovement(delta);
	}

	private void UpdateMovement(double delta)
	{
		direction = new Vector3(
			(float)d.ToGodotBool() - (float)a.ToGodotBool(),
			(float)e.ToGodotBool() - (float)q.ToGodotBool(),
			(float)s.ToGodotBool() - (float)w.ToGodotBool()
		);

		Vector3 offset = (direction.Normalized() * acceleration * velMultiplier * (float)delta)
			+ (velocity.Normalized() * deceleration * velMultiplier * (float)delta);

		float speedMulti = 1f;
		if (shift) speedMulti *= SHIFT_MULTIPLIER;
		if (alt) speedMulti *= ALT_MULTIPLIER;

		if (direction.IsZeroApprox() && offset.LengthSquared() > velocity.LengthSquared())
		{
			velocity = Vector3.Zero;
		}
		else
		{
			velocity.X = Mathf.Clamp(velocity.X + offset.X, -velMultiplier, velMultiplier);
			velocity.Y = Mathf.Clamp(velocity.Y + offset.Y, -velMultiplier, velMultiplier);
			velocity.Z = Mathf.Clamp(velocity.Z + offset.Z, -velMultiplier, velMultiplier);

			Translate(velocity * (float)delta * speedMulti);
		}
	}

	private void UpdateMouselook()
	{
		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			mousePosition *= sensitivity;
			float yaw = mousePosition.X;
			float pitch = mousePosition.Y;
			mousePosition = Vector2.Zero;

			pitch = Mathf.Clamp(pitch, -90 - totalPitch, 90 - totalPitch);
			totalPitch += pitch;

			RotateY(Mathf.DegToRad(-yaw));
			RotateObjectLocal(new Vector3(1, 0, 0), Mathf.DegToRad(-pitch));
		}
	}
}