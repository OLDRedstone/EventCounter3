using EvtCtr3.Animation;
using EvtCtr3.Core;
namespace EvtCtr3
{
	internal class EventInfoBox
	{
		public const int foldHeight = 12;
		public const int hoverHeight = 18;
		public const int unfoldHeight = 64;
		public event EventHandler? StateChanged;
		public EventInfoBox(AnimationTimer timer)
		{
			Height = new(timer, TimeSpan.FromMilliseconds(500));
			Height.Value = foldHeight;
			FullInfoPercentage = new(timer, TimeSpan.FromMilliseconds(500));
			Height.ValueChanging += (s, e) => StateChanged?.Invoke(this, EventArgs.Empty);
			FullInfoPercentage.ValueChanging += (s, e) => StateChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool IsHovering { get; set; }
		public bool IsExpanded { get; set; }
		public void UpdateState()
		{
			Height.Value = IsExpanded ? unfoldHeight : (IsHovering ? hoverHeight : foldHeight);
			FullInfoPercentage .Value = IsExpanded ? 1f : 0f;
		}
		public required ICounterResultItem ResultItem { get; set; }
		public EaseFloat Height { get; set; }
		public EaseFloat FullInfoPercentage { get; set; }
	}
}
