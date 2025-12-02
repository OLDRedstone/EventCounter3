using System;
using RhythmBase.Global.Components.Easing;
using System.Numerics;

namespace EvtCtr3.Animation
{
	public class EaseFloat(AnimationTimer timer, TimeSpan duration, float origin = 0) :
		EaseValueBase<float>(timer, duration, origin)
	{
		public EaseFloat(AnimationTimer timer, float origin = 0) : this(timer,TimeSpan.FromSeconds(1),origin) { }
		protected override float GetCurrent() => Duration == TimeSpan.Zero
			? _target
			: (float)EaseType.Calculate(Percent, _origin, _target);
		public static implicit operator float(EaseFloat e) => e.GetCurrent();
	}
}