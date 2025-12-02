using RhythmBase.Global.Extensions;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.RhythmDoctor.Utils;
using System.IO.Compression;
using System.Text;
namespace EvtCtr3.Core;

public interface ICounterResultItem
{
	EventType Type { get; init; }
	int Count { get; set; }
}
public class CounterResultItemSimply : ICounterResultItem
{
	public EventType Type { get; init; }
	public int Count { get; set; }
}
public class CounterResultItemDetailed : ICounterResultItem
{
	public EventType Type { get; init; }
	public int Count { get; set; }
	public int[] CountsPerBar { get; init; }
	public CounterResultItemSimply ToSimply()
	{
		return new CounterResultItemSimply
		{
			Type = this.Type,
			Count = this.Count
		};
	}
	public CounterResultItemDetailed(int barCount)
	{
		CountsPerBar = new int[barCount];
	}
}
public struct CounterResultCollection<TType, T> where TType: struct, Enum where T : ICounterResultItem
{
	public T this[EventType type]
	{
		get => values[(int)type];
		set => values[(int)type] = value;
	}
	private readonly T[] values;
	public readonly int TotalCount => values.Sum(i => i.Count);
	public readonly int Count => values.Sum(i => i?.Count??0);
	public CounterResultCollection()
	{
		int capacity = 0;
		foreach (var type in Enum.GetValues<EventType>())
			capacity = capacity > (int)type ? capacity : (int)type;
		values = new T[capacity + 1];
	}
}
internal class Counter
{
	private readonly CharTree _eventTypeTree;
	public Counter()
	{
		_eventTypeTree = new CharTree();
		foreach (var type in Enum.GetValues<EventType>().Except(EventTypeUtils.CustomTypes))
			_eventTypeTree.Insert(type.ToString());
		RhythmBase.Global.Settings.GlobalSettings.CachePath = "Cache";
	}
	public CounterResultCollection<EventType, CounterResultItemSimply> CountSimply(string filepath)
	{
		if (!File.Exists(filepath))
		{
			throw new ArgumentException("File does not exist", nameof(filepath));
		}
		Stream stream;
		if (Path.GetExtension(filepath) is ".zip" or ".rdzip")
		{
			ZipArchive archive = ZipFile.OpenRead(filepath);
			ZipArchiveEntry entry = archive.GetEntry("main.rdlevel") ?? throw new ArgumentException("Zip file does not contain main.rdlevel", nameof(filepath));
			stream = entry.Open();
		}
		else if (Path.GetExtension(filepath) == ".rdlevel")
		{
			stream = File.Open(filepath, FileMode.Open, FileAccess.Read);
		}
		else
		{
			throw new ArgumentException("File is not a .rdlevel or .zip/.rdzip containing a .rdlevel", nameof(filepath));
		}

		string? match;
		using StreamReader reader = new(stream);
		CounterResultCollection<EventType, CounterResultItemSimply> result = new();
		while (!reader.EndOfStream)
		{
			int ch = reader.Read();
			if (ch == -1)
				break;

			if (ch != '"')
				continue;
			string? key = ReadUntilQuote(reader);
			if (key is null)
				break;
			if (!string.Equals(key, "type", StringComparison.Ordinal))
				continue;
			SkipWhitespace(reader);
			int colon = reader.Read();
			if (colon == -1)
				break;
			if (colon != ':')
				continue;
			SkipWhitespace(reader);
			int next = reader.Peek();
			if (next != '"')
				continue;
			reader.Read();
			match = _eventTypeTree.MatchFromStream(reader, '"');
			if (string.IsNullOrEmpty(match))
				continue;

			if (!EnumConverter.TryParse(match, out EventType type))
				continue;

			result[type] ??= new CounterResultItemSimply()
			{
				Type = type,
			};
			result[type].Count++;
		}
		return result;
		static string? ReadUntilQuote(StreamReader r)
		{
			var sb = new StringBuilder();
			while (true)
			{
				int x = r.Read();
				if (x == -1)
					return null;
				if (x == '"')
					break;
				sb.Append((char)x);
			}
			return sb.ToString();
		}
		static void SkipWhitespace(StreamReader r)
		{
			while (true)
			{
				int p = r.Peek();
				if (p == -1) break;
				if (char.IsWhiteSpace((char)p))
					r.Read();
				else
					break;
			}
		}
	}
	public CounterResultCollection<EventType, CounterResultItemDetailed> CountDetailed(string filepath)
	{
		using RDLevel level = RDLevel.FromFile(filepath);
		(int bar, _) = level.Length;
		CounterResultCollection<EventType, CounterResultItemDetailed> result = new();
		foreach (var evt in level)
		{
			(int b, _) = evt.Beat;
			result[evt.Type]??= new CounterResultItemDetailed(bar)
			{
				Type = evt.Type,
				Count = 0,
			};
			result[evt.Type].CountsPerBar[b - 1]++;
			result[evt.Type].Count++;
		}
		return result;
	}
}
