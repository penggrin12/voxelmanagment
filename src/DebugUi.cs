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

	public override void _Process(double delta)
	{
		Get<Label>("Frame").Text = $"FRM: {Engine.GetProcessFrames()}";
		Get<Label>("Fps").Text = $"FPS: {Performance.GetMonitor(Performance.Monitor.TimeFps)} ({Performance.GetMonitor(Performance.Monitor.TimeProcess)} ms)";
	}
}
