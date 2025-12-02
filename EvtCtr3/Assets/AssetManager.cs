using SkiaSharp;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;

namespace EvtCtr3.Assets;

internal record struct SliceInfo()
{
	public SKRectI Bounds { get; set; }
	public SKRectI Center { get; set; }
	public readonly bool IsNinePatch => Center != SKRectI.Empty;
	public SKPointI Pivot { get; set; }
	public bool HasSpace { get; set; }
	public int Scale { get; set; } = 1;
}
internal static class AssetManager
{
	private const string AssetFilePath = "Assets/assets.png";
	private const string SlicesFilePath = "Assets/assets.json";
	private const string LangDirPath = "Assets/Lang";
	private const string ConfigFilePath = "config.yaml";
	//private const string assemblyPath = @"E:/csharp/EvtCtr3/EvtCtr3/bin/Debug/net10.0-windows/";
	internal static readonly Dictionary<string, SliceInfo> _slices = LoadSlices();
	internal static readonly SKBitmap _assetFile = SKBitmap.Decode(AssetFilePath);
	public static Dictionary<string, SliceInfo> LoadSlices()
	{
		using Stream stream = File.OpenRead(SlicesFilePath);
		JsonDocument document = JsonDocument.Parse(stream);
		var slices = document.RootElement.GetProperty("meta").GetProperty("slices");
		Dictionary<string, SliceInfo> sliceInfos = [];
		foreach (var slice in slices.EnumerateArray())
		{
			string name = slice.GetProperty("name").GetString() ?? "";
			var keys = slice.GetProperty("keys");
			foreach (var key in keys.EnumerateArray())
			{
				int frame = key.GetProperty("frame").GetInt32();
				var boundsProp = key.GetProperty("bounds");
				SKRectI bounds = SKRectI.Create(
					boundsProp.GetProperty("x").GetInt32(),
					boundsProp.GetProperty("y").GetInt32(),
					boundsProp.GetProperty("w").GetInt32(),
					boundsProp.GetProperty("h").GetInt32()
				);
				SliceInfo sliceInfo = new()
				{
					Bounds = bounds,
				};
				if (key.TryGetProperty("center", out var centerProp))
				{
					SKRectI center = SKRectI.Create(
						centerProp.GetProperty("x").GetInt32(),
						centerProp.GetProperty("y").GetInt32(),
						centerProp.GetProperty("w").GetInt32(),
						centerProp.GetProperty("h").GetInt32()
					);
					sliceInfo.Center = center;
				}
				if (key.TryGetProperty("pivot", out var pivotProp))
				{
					SKPointI pivot = new(
						pivotProp.GetProperty("x").GetInt32(),
						pivotProp.GetProperty("y").GetInt32()
					);
					sliceInfo.Pivot = pivot;
				}
				if (slice.TryGetProperty("data", out var dataProp))
				{
					string data = dataProp.GetString() ?? "";
					if (data.Length >= 3)
						sliceInfo.Scale = data[0..3] switch
						{
							"@2x" => 2,
							_ => 1,
						};
					if (data.Length > 3)
						sliceInfo.HasSpace = data[3] is 'T';
				}
				sliceInfos[name] = sliceInfo;
			}
		}
		return sliceInfos;
	}
	public static Config LoadConfig()
	{
		// If YAML config is present, use YAML loader
		string ext = Path.GetExtension(ConfigFilePath);
		if (ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase) || ext.Equals(".yml", StringComparison.OrdinalIgnoreCase))
		{
			return LoadConfigFromYaml();
		}

		using Stream stream = File.OpenRead(ConfigFilePath);
		Config? config = JsonSerializer.Deserialize<Config>(stream);
		return config ?? new Config();
	}
	public static Config LoadConfigFromYaml()
	{
		if (!File.Exists(ConfigFilePath))
			return new Config();
		using StreamReader sr = new(ConfigFilePath);
		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();
		try
		{
			var cfg = deserializer.Deserialize<Config>(sr);
			return cfg ?? new Config();
		}
		catch
		{
			// Fallback to default if deserialization fails
			return new Config();
		}
	}
	public static Dictionary<string, string> LoadLangYaml(string langId)
	{
		string path = Path.Combine(LangDirPath, langId + ".yaml");
		if (!File.Exists(path))
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		using StreamReader sr = new(path);
		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();
		var obj = deserializer.Deserialize(sr);
		if (obj is not IDictionary dic)
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var root = ConvertDictionary(dic);
		if (!root.TryGetValue("values", out var valuesObj) || valuesObj is not Dictionary<string, object> valuesDict)
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		void Recurse(string prefix, Dictionary<string, object> node)
		{
			foreach (var kv in node)
			{
				string key = string.IsNullOrEmpty(prefix) ? kv.Key : prefix + "." + kv.Key;
				switch (kv.Value)
				{
					case string s:
						flat[key] = s;
						break;
					case Dictionary<string, object> d:
						Recurse(key, d);
						break;
					default:
						flat[key] = kv.Value?.ToString() ?? string.Empty;
						break;
				}
			}
		}
		Recurse(string.Empty, valuesDict);
		return flat;
	}
	private static Dictionary<string, object> ConvertDictionary(IDictionary src)
	{
		var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		foreach (DictionaryEntry entry in src)
		{
			string key = entry.Key?.ToString() ?? "";
			object value = entry.Value ?? "";
			switch (value)
			{
				case IDictionary nested:
					dict[key] = ConvertDictionary(nested);
					break;
				case IList list:
					var newList = new List<object>();
					foreach (var item in list)
					{
						switch (item)
						{
							case IDictionary ni:
								newList.Add(ConvertDictionary(ni));
								break;
							default:
								newList.Add(item ?? "");
								break;
						}
					}
					dict[key] = newList;
					break;
				default:
					dict[key] = value;
					break;
			}
		}
		return dict;
	}

	public static Dictionary<string, Dictionary<string, string>> LoadLangs()
	{
		var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (!Directory.Exists(LangDirPath))
				return dict;
			foreach (var file in Directory.EnumerateFiles(LangDirPath, "*.yaml").Concat(Directory.EnumerateFiles(LangDirPath, "*.yml")))
			{
				string key = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
				var data = LoadLangYaml(key);
				dict[key] = data;
			}
		}
		catch
		{
			// ignore errors and return what we have
		}
		return dict;
	}
	public static void SaveConfigToYaml(Config cfg)
	{
		var serializer = new SerializerBuilder()
			.WithNamingConvention(CamelCaseNamingConvention.Instance)
			.Build();
		string temp = ConfigFilePath + ".tmp";
		using (var sw = new StreamWriter(temp, false, System.Text.Encoding.UTF8))
		{
			serializer.Serialize(sw, cfg);
		}
		// replace atomically
		File.Delete(ConfigFilePath);
		File.Move(temp, ConfigFilePath);
	}
	public static SKColor GetColor(string src, SKPointI offset)
	{
		if (!_slices.TryGetValue(src, out SliceInfo info))
			return SKColors.Transparent;
		SKPointI pixel = new(info.Bounds.Left + offset.X, info.Bounds.Top + offset.Y);
		if (pixel.X < info.Bounds.Left || pixel.X >= info.Bounds.Right || pixel.Y < info.Bounds.Top || pixel.Y >= info.Bounds.Bottom)
			return SKColors.Transparent;
		return _assetFile.GetPixel(pixel.X, pixel.Y);
	}
}
