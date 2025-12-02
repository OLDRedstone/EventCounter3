using System.Globalization;
using SkiaSharp;

namespace EvtCtr3.Assets;

internal static class Localization
{
	private static readonly Dictionary<string, Dictionary<string, string>> _languages = AssetManager.LoadLangs();
	private static string _currentKey = NormalizeCulture(CultureInfo.CurrentUICulture.Name);
	public static string CurrentKey
	{
		get => _currentKey;
		set
		{
			string k = NormalizeCulture(value);
			if (_languages.ContainsKey(k))
				_currentKey = k;
		}
	}
	private static string NormalizeCulture(string s)
	{
		if (string.IsNullOrWhiteSpace(s))
			return "en-us";
		s = s.Replace('_', '-').ToLowerInvariant();
		return s;
	}
	public static void Reload()
	{
		_languages.Clear();
		foreach (var kv in AssetManager.LoadLangs())
			_languages[kv.Key] = kv.Value;
	}
	public static string Get(params string[] path)
	{
		if (path == null || path.Length == 0)
			return "[empty key]";
		string joined = string.Join('.', path);
		if (!_languages.TryGetValue(_currentKey, out var root))
		{
			if (!_languages.TryGetValue("en-us", out root))
				return "[empty lang]";
		}
		if (root.TryGetValue(joined, out var val))
			return val;
		// try fallback to en-us if not current
		if (_currentKey != "en-us" && _languages.TryGetValue("en-us", out var en) && en.TryGetValue(joined, out var val2))
			return val2;
		return $"[{joined}]";
	}
}
