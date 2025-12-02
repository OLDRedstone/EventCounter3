using RhythmBase.Global.Components.Easing;
using SkiaSharp;
using System.Diagnostics.CodeAnalysis;

namespace EvtCtr3.Animation
{

	public class EaseTPoint(AnimationTimer timer, TimeSpan duration, SKPoint origin = default)
			: EaseValueBase<SKPoint>(timer, duration, origin), IEquatable<EaseTPoint>
	{
		public EaseTPoint(AnimationTimer timer, SKPoint origin = default)
				: this(timer, TimeSpan.FromSeconds(1), origin) { }
		protected override SKPoint GetCurrent() => Duration == TimeSpan.Zero
				? _target
				: new(
						(float)EaseType.Calculate(Percent, _origin.X, _target.X),
						(float)EaseType.Calculate(Percent, _origin.Y, _target.Y)
						);
		public static implicit operator SKPoint(EaseTPoint e) => e.GetCurrent();
		public bool Equals([NotNullWhen(true)] EaseTPoint? other)
		{
			if (other is null) return false;
			return Value.Equals(other.Value);
		}
		public override bool Equals([NotNullWhen(true)] object? obj) => obj is EaseTPoint e && Equals(e);
		public override int GetHashCode() => Value.GetHashCode();
	}
}
