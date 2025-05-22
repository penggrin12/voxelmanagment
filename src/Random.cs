namespace Game;

public static class Random
{
	private static int _seed = 0;
	public static int Seed { get { return _seed; } set { _seed = value; random = new(value); Godot.GD.Seed((ulong)value); } }
	private static System.Random random = new(Seed);

	static Random()
	{
		Seed = (int)Godot.Time.GetUnixTimeFromSystem(); // 356
	}

	public static int Next() { return random.Next(); }
	public static int Next(int min) { return random.Next(min); }
	public static int Next(int min, int max) { return random.Next(min, max); }
	public static double NextDouble() { return random.NextDouble(); }
	public static long NextLong() { return random.NextInt64(); }
	public static float NextFloat() { return random.NextSingle(); }
	public static double RandRange(double from, double to) { return Godot.GD.RandRange(from, to); }
	public static float RandRange(float from, float to) { return (float)Godot.GD.RandRange(from, to); }
	public static int RandRange(int from, int to) { return Godot.GD.RandRange(from, to); }
}
