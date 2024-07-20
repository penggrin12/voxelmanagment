using Godot;

namespace Game;

public partial class HideInEditor : Node3D
{
	public override void _Ready()
	{
		Visible = true;
	}
}
