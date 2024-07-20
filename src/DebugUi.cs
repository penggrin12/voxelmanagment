using Godot;

namespace Game;

public partial class DebugUi : Control
{
	public bool Enabled { get; set; } = true; // should probably show/hide itself on change

	public DebugUi()
	{
		Find.DebugUi = this;
	}

	public T Get<T>(NodePath name) where T : Control
	{
		return GetNode<VBoxContainer>("VBox").GetNode<T>(name);
	}
}
