namespace EvtCtr3.Animation;

public class AnimationTimer : System.Timers.Timer
{
	public event EventHandler<TimeSpan>? Updated;
	private TimeSpan _last;
	private TimeSpan _elapsedGameTime;
	public TimeSpan ElapsedGameTime => _elapsedGameTime;
	public TimeSpan CurrentTime => _last;
	public AnimationTimer() : base(TimeSpan.FromMilliseconds(10))
	{
		_elapsedGameTime = TimeSpan.Zero;
		Elapsed += (s,e)=> OnUpdate(e.SignalTime.TimeOfDay) ;
	}
	private void AnimationTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
	{
		//e.
	}

	protected void OnStopped()
	{
		_elapsedGameTime = TimeSpan.Zero;
	}
	protected void OnUpdate(TimeSpan gameTime)
	{
		_elapsedGameTime = gameTime - _last;
		_last = gameTime;
		Updated?.Invoke(this, gameTime);
	}
}
