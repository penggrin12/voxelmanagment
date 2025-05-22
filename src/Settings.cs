using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Game;

public static class Settings
{
	private const string    SETTINGS_FILE_PATH = "user://settings.json";
	private const ushort    SETTINGS_VERSION = 5;
	public static ushort    WorldSize { get; set; } = 15;
	public static bool      ShowDebugDraw { get; set; } = true;
	public static bool      ShowEvenMoreDebugDraw { get; set; } = false;
	public static bool      IslandMode { get; set; } = false;

	private static void NewSettingsFile(string path)
	{
		using StreamWriter textReader = new(new FileStream(path, FileMode.Create, FileAccess.Write));

		Dictionary<string, object> dictToWrite = new() { { "FileVersion", SETTINGS_VERSION } };

		foreach (PropertyInfo property in typeof(Settings).GetProperties(BindingFlags.Public | BindingFlags.Static))
		{
			dictToWrite.Add(property.Name, property.GetValue(null));
		}

		textReader.Write(JsonConvert.SerializeObject(dictToWrite, Formatting.Indented, new JsonSerializerSettings()));
	}

	private static void LoadSettingsFile(string path)
	{
		using StreamReader textReader = new(new FileStream(path, FileMode.Open, FileAccess.Read));
		foreach (KeyValuePair<string, object> setting in JsonConvert.DeserializeObject<Dictionary<string, object>>(textReader.ReadToEnd()))
		{
			if (setting.Key == "FileVersion")
			{
				Godot.GD.Print($"got settings versioned {setting.Value}");

				if (((ushort)(long)setting.Value) != SETTINGS_VERSION)
				{
					Godot.GD.PushWarning("mismatch settings versions with loaded file, rewriting file!");
					NewSettingsFile(path);
					LoadSettingsFile(path);
					return;
				}

				continue;
			}

			Godot.GD.Print($"{setting.Key}: {setting.Value}");
			PropertyInfo field = typeof(Settings).GetProperty(setting.Key);

			if (field.PropertyType == typeof(int)) // Json.Net gives longs no matter what
				field.SetValue(null, (int)(long)setting.Value);
			else if (field.PropertyType == typeof(short))
				field.SetValue(null, (short)(long)setting.Value);
			else if (field.PropertyType == typeof(uint))
				field.SetValue(null, (uint)(long)setting.Value);
			else if (field.PropertyType == typeof(ushort))
				field.SetValue(null, (ushort)(long)setting.Value);
			else
				field.SetValue(null, setting.Value);
		}
	}

	static Settings()
	{
		string settingsPath = Godot.ProjectSettings.GlobalizePath(SETTINGS_FILE_PATH);
		if (!File.Exists(settingsPath))
			NewSettingsFile(settingsPath);

		LoadSettingsFile(settingsPath);
	}
}
