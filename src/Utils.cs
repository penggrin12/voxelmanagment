using Godot;

namespace Game;

public static class Utils
{
    public static Color GetRandomColor() { return new Color(GD.Randf(), GD.Randf(), GD.Randf()); }
}