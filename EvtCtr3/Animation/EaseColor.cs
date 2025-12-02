using RhythmBase.Global.Components.Easing;
using SkiaSharp;
using System;

namespace EvtCtr3.Animation;

public class EaseColor(AnimationTimer timer, TimeSpan duration, SKColor origin) : EaseValueBase<SKColor>(timer, duration, origin)
{
	public EaseColor(AnimationTimer timer, SKColor origin) : this(timer, TimeSpan.FromSeconds(1), origin) { }
	protected override SKColor GetCurrent() => Duration == TimeSpan.Zero
		? _target
		: new(
			(byte)EaseType.Calculate(Percent, _origin.Alpha, _target.Alpha),
			(byte)EaseType.Calculate(Percent, _origin.Red, _target.Red),
			(byte)EaseType.Calculate(Percent, _origin.Green, _target.Green),
			(byte)EaseType.Calculate(Percent, _origin.Blue, _target.Blue));
	public static implicit operator SKColor(EaseColor e) => e.GetCurrent();
}
