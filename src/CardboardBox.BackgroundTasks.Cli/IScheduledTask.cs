namespace CardboardBox
{
	public interface IScheduledTask
	{
		int DelayMs { get; }

		Task Run();
	}
}
