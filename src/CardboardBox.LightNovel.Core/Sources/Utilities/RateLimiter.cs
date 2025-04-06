using System.Runtime.CompilerServices;

namespace CardboardBox.LightNovel.Core.Sources.Utilities;

/// <summary>
/// Represents a min and max range for a value
/// </summary>
/// <param name="min">The minimum value available</param>
/// <param name="max">The maximum value available</param>
public class MinMax(int min, int max)
{
    /// <summary>
    /// The random instance for values
    /// </summary>
    public static Random Rand { get; set; } = new();

    /// <summary>
    /// The minimum value available
    /// </summary>
    public int Min { get; init; } = Math.Min(min, max);

    /// <summary>
    /// The maximum value available
    /// </summary>
    public int Max { get; init; } = Math.Max(min, max);

    /// <summary>
    /// Whether or not the range is "enabled"
    /// </summary>
    public bool Enabled => Min > 0 || Max > 0;

    /// <summary>
    /// The generated value
    /// </summary>
    public int Value => RandomCap();

    /// <summary>
    /// The generated value for timeouts (if given seconds)
    /// </summary>
    public int TimeoutMilliseconds => TimeoutValue();

    /// <summary>
    /// Generates a random number between the min and max values
    /// </summary>
    /// <returns>The random number</returns>
    public virtual int RandomCap()
    {
        var number = Rand.Next(Min - 1, Max + 1);
        return Math.Max(Min, Math.Min(Max, number));
    }

    /// <summary>
    /// Generates a random timeout value between the min and max values
    /// </summary>
    /// <returns>The millisecond value</returns>
    public virtual int TimeoutValue()
    {
        if (!Enabled) return 0;
        if (Max <= Min) return Min;

        var timeoutSec = Value;
        double offset = Rand.NextDouble();
        double dblTimeout = timeoutSec + offset;
        return (int)(dblTimeout * 1000);
    }
}

/// <summary>
/// Represents a rate limiter for some request
/// </summary>
/// <param name="Limits">The number of requests to allow before waiting</param>
/// <param name="Duration">The number of seconds to wait</param>
public record class RateLimiterBase(
    MinMax Limits,
    MinMax Duration)
{
    /// <summary>
    /// The number of retries to attempt before failing
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// How long to wait before retrying a request after a 429 code
    /// </summary>
    public MinMax TooManyRequestDuration { get; set; } = new(30, 90);

    /// <summary>
    /// The current count of total requests
    /// </summary>
    public int Count { get; internal set; } = 0;

    /// <summary>
    /// The current count of requests since the last pause
    /// </summary>
    public int Rate { get; internal set; } = 0;

    /// <summary>
    /// Whether or not the rate limiter is enabled
    /// </summary>
    public bool Enabled => Limits.Enabled && Duration.Enabled;

    /// <summary>
    /// Gets the latest rate limit values
    /// </summary>
    /// <returns>The rate limit values</returns>
    public virtual (int limit, int timeout) GetRateLimit()
    {
        return (Limits.Value, Duration.TimeoutMilliseconds);
    }
}

/// <summary>
/// Represents a rate limiter for some request
/// </summary>
/// <typeparam name="T">The type of result returned by the request</typeparam>
/// <param name="Limits">The number of requests to allow before waiting</param>
/// <param name="Duration">The number of seconds to wait</param>
/// <param name="Fetcher">How the request occurs</param>
public record class RateLimiter<T>(
    MinMax Limits,
    MinMax Duration,
    Func<Task<T>> Fetcher) : RateLimiterBase(Limits, Duration)
{
    /// <summary>
    /// Make a request using the <see cref="Fetcher"/> and retries if a 429 is returned
    /// </summary>
    /// <param name="logger">The logger for messages</param>
    /// <param name="token">The cancellation token for the requests</param>
    /// <param name="count">The number of requests already been made</param>
    /// <returns>The result of the request</returns>
    public virtual async Task<T> MakeRequest(ILogger logger, CancellationToken token, int count = 0)
    {
        try
        {
            return await Fetcher();
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode != HttpStatusCode.TooManyRequests)
            {
                logger.LogError(ex, "Failed to fetch data - HTTP Exception");
                throw;
            }

            if (count >= MaxRetries)
            {
                logger.LogError(ex, "Failed to fetch data after {count} retries", count);
                throw;
            }

            var timeout = TooManyRequestDuration.TimeoutMilliseconds;
            logger.LogWarning("Too many requests, waiting {timeout}ms before retrying", timeout);
            await Task.Delay(timeout, token);
            logger.LogInformation("Retrying request after Too many requests");
            return await MakeRequest(logger, token, count + 1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch data");
            throw;
        }
    }

    /// <summary>
    /// Fetches the data from the endpoint using the rate limiter
    /// </summary>
    /// <param name="logger">The logger to use for messages</param>
    /// <param name="token">The cancellation token for stopping iteration</param>
    /// <returns>All of the records from the request</returns>
    public async IAsyncEnumerable<T> Fetch(ILogger logger, [EnumeratorCancellation] CancellationToken token)
    {
        Count = 0;
        Rate = 0;
        var (limit, timeout) = GetRateLimit();

        while(true)
        {
            if (token.IsCancellationRequested) yield break;

            Count++;
            Rate++;

            var result = await MakeRequest(logger, token);
            yield return result;

            if (!Enabled || Rate < limit) continue;

            logger.LogInformation("Rate limit reached. Pausing for {timeout}ms. Count: {count} - {rate}/{limit}",
                timeout, Count, Rate, limit);

            await Task.Delay(timeout, token);
            Rate = 0;
            (limit, timeout) = GetRateLimit();

            logger.LogInformation("Resuming after pause. New Limits {limit} - {timeout}ms", limit, timeout);
        }
    }
}

public record class RateLimiter<TIn, TOut>(
    MinMax Limits,
    MinMax Duration,
    Func<TIn, Task<TOut>> Fetcher) : RateLimiterBase(Limits, Duration)
{
    /// <summary>
    /// Make a request using the <see cref="Fetcher"/> and retries if a 429 is returned
    /// </summary>
    /// <param name="item">The item to fetch for</param>
    /// <param name="logger">The logger for messages</param>
    /// <param name="token">The cancellation token for the requests</param>
    /// <param name="count">The number of requests already been made</param>
    /// <returns>The result of the request</returns>
    public virtual async Task<TOut> MakeRequest(TIn item, ILogger logger, CancellationToken token, int count = 0)
    {
        try
        {
            return await Fetcher(item);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode != HttpStatusCode.TooManyRequests)
            {
                logger.LogError(ex, "Failed to fetch data - HTTP Exception");
                throw;
            }

            if (count >= MaxRetries)
            {
                logger.LogError(ex, "Failed to fetch data after {count} retries", count);
                throw;
            }

            var timeout = TooManyRequestDuration.TimeoutMilliseconds;
            logger.LogWarning("Too many requests, waiting {timeout}ms before retrying", timeout);
            await Task.Delay(timeout, token);
            logger.LogInformation("Retrying request after Too many requests");
            return await MakeRequest(item, logger, token, count + 1);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch data");
            throw;
        }
    }

    /// <summary>
    /// Fetches the data from the endpoint using the rate limiter
    /// </summary>
    /// <param name="input">The input data</param>
    /// <param name="logger">The logger to use for messages</param>
    /// <param name="token">The cancellation token for stopping iteration</param>
    /// <returns>All of the records from the request</returns>
    public async IAsyncEnumerable<TOut> Fetch(IAsyncEnumerable<TIn> input, ILogger logger, [EnumeratorCancellation] CancellationToken token)
    {
        Count = 0;
        Rate = 0;
        var (limit, timeout) = GetRateLimit();

        await foreach(var item in input)
        {
            if (token.IsCancellationRequested) yield break;

            if (Enabled && Rate >= limit)
            {
                logger.LogInformation("Rate limit reached. Pausing for {timeout}ms. Count: {count} - {rate}/{limit}",
                    timeout, Count, Rate, limit);

                await Task.Delay(timeout, token);
                Rate = 0;
                (limit, timeout) = GetRateLimit();

                logger.LogInformation("Resuming after pause. New Limits {limit} - {timeout}ms", limit, timeout);
            }

            Count++;
            Rate++;

            var result = await MakeRequest(item, logger, token);
            yield return result;
        }
    }
}