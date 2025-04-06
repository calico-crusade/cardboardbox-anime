namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;

public abstract class RatedSource
{
    public virtual int MaxRequestsBeforePauseMin => 10;
    public virtual int MaxRequestsBeforePauseMax => 15;
    public virtual int PauseDurationSecondsMin => 3;
    public virtual int PauseDurationSecondsMax => 10;

    public RateLimiter<T> CreateLimiter<T>(Func<Task<T>> requester)
    {
        return new RateLimiter<T>(
            new(MaxRequestsBeforePauseMin, MaxRequestsBeforePauseMax),
            new(PauseDurationSecondsMin, PauseDurationSecondsMax),
            requester);
    }

    public RateLimiter<TIn, TOut> CreateLimiter<TIn, TOut>(Func<TIn, Task<TOut>> requester)
    {
        return new RateLimiter<TIn, TOut>(
            new(MaxRequestsBeforePauseMin, MaxRequestsBeforePauseMax),
            new(PauseDurationSecondsMin, PauseDurationSecondsMax),
            requester);
    }
}
