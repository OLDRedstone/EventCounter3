using RhythmBase.Global.Components.Easing;

namespace EvtCtr3.Animation
{
	public abstract class EaseValueBase<TValue>
		where TValue : struct, IEquatable<TValue>
	{
		protected readonly AnimationTimer _timer;
		protected TValue _origin;
		protected TimeSpan _start;
		protected TValue _target;
		private TimeSpan duration;
		private bool isUpdating = false;
		public EaseType EaseType { get; set; } = EaseType.OutExpo;
		public TimeSpan Duration { get => duration; set => duration = value; }
		public TValue Origin
		{
			private get => _origin;
			set
			{
				_origin = value;
				_start = _timer.CurrentTime;
			}
		}
		public TValue Value
		{
			get => GetCurrent();
			set
			{
				_origin = GetCurrent();
				_start = _timer.CurrentTime;
				_target = value;
			}
		}
		public TValue Target => _target;
		public bool IsEasing
		{
			get
			{
				if (Duration == TimeSpan.Zero)
					return false;
				float time = (float)(_timer.CurrentTime - _start).TotalSeconds;
				return time < Duration.TotalSeconds;
			}
		}
		public float Percent
		{
			get
			{
				if (Duration == TimeSpan.Zero)
					return 1f;
				float time = (float)(_timer.CurrentTime - _start).TotalSeconds;
				return Math.Clamp(time / (float)Duration.TotalSeconds, 0f, 1f);
			}
		}
		public event EventHandler<TValue>? ValueChanging;
		public event EventHandler<TValue>? ValueChangingEnd;
		public void SetImmediately(TValue value)
		{
			_origin = value;
			_target = value;
			_start = _timer.CurrentTime;
			ValueChanging?.Invoke(this, value);
		}
		public EaseValueBase(AnimationTimer timer, TimeSpan duration, TValue origin = default)
		{
			Duration = duration;
			_timer = timer;
			_origin = origin;
			_target = origin;
			_start = timer.CurrentTime;
			_timer.Updated += Update;
		}
		public EaseValueBase(AnimationTimer timer, TValue origin = default) : this(timer, TimeSpan.FromSeconds(1), origin) { }
		protected abstract TValue GetCurrent();
		private void Update(object? sender, TimeSpan e)
		{
			TValue current = GetCurrent();
			if (!current.Equals(_target))
			{
				ValueChanging?.Invoke(this, current);
				isUpdating = true;
			}
			else if (isUpdating)
			{
				ValueChanging?.Invoke(this, current);
				ValueChangingEnd?.Invoke(this, current);
				isUpdating = false;
			}
		}
		public override string ToString()
		{
			return $"{Origin} ={Value}> {Target}";
		}
	}
}