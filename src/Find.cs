using Game.Interfaces;

namespace Game;

// shamelessly stolen from rimworld, bad implementation
public static class Find
{
	public static World World { get; set; } = null; // last world to get ready gets this
	public static DebugUi DebugUi { get; set; } = null; // DebugUi autoload
	public static IPlayer Player { get; set; } = null; // world sets this before player spawn
}
