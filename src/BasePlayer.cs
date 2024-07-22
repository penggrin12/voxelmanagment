using System;
using Godot;

namespace Game;

// exists just for chunk rendering without
// duplicating code between player controllers
public partial class BasePlayer : CharacterBody3D
{
	private double lastTimeRenderedChunks = 0f;
	private float renderChunksEverySeconds = 1f;
    public World world;

	[Obsolete]
    public void HandleUpdateRenderDistance(Vector3 position)
	{
		
		// double now = Time.GetUnixTimeFromSystem();
		// if (now - lastTimeRenderedChunks >= renderChunksEverySeconds)
		// {
		// 	world.EmitSignal(World.SignalName.UpdateRenderDistance, new Variant[] { new Vector2(position.X / 16f, position.Z / 16f) });
		// 	lastTimeRenderedChunks = now;
		// }
	}
}