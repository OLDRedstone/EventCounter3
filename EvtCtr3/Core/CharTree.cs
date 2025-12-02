using System.Text;

namespace EvtCtr3.Core;

internal class CharTreeNode
{
	public Dictionary<char, CharTreeNode> Children { get; } = new();
	public bool IsEndOfWord { get; set; } = false;
}
internal class CharTree
{
	private readonly CharTreeNode root = new();
	private readonly object syncRoot = new();

	public void Insert(string word)
	{
		ArgumentNullException.ThrowIfNull(word);
		if (word.Length == 0)
		{
			root.IsEndOfWord = true;
			return;
		}
		lock (syncRoot)
		{
			CharTreeNode current = root;
			foreach (char c in word)
			{
				if (!current.Children.TryGetValue(c, out CharTreeNode? next))
				{
					next = new CharTreeNode();
					current.Children[c] = next;
				}
				current = next;
			}
			current.IsEndOfWord = true;
		}
	}

	public bool Search(string word)
	{
		CharTreeNode current = root;
		foreach (char c in word)
		{
			if (!current.Children.ContainsKey(c))
			{
				return false;
			}
			current = current.Children[c];
		}
		return current.IsEndOfWord;
	}

	public string? MatchFromStream(StreamReader reader, char terminator)
	{
		var sb = new StringBuilder();
		int read;
		var current = root;
		while ((read = reader.Read()) != -1)
		{
			char c = (char)read;
			if (c == terminator)
				if (current.IsEndOfWord)
					return sb.ToString();
				else
					return null;
			if (!current.Children.TryGetValue(c, out var next))
				return null;
			current = next;
			sb.Append(c);
		}
		return null;
	}
}
