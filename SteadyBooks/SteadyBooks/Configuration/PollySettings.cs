namespace SteadyBooks.Configuration;

public class PollySettings
{
    public RetrySettings Retry { get; set; } = new();
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
    public TimeoutSettings Timeout { get; set; } = new();
}

public class RetrySettings
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialDelayMilliseconds { get; set; } = 100;
    public int MaxDelayMilliseconds { get; set; } = 5000;
}

public class CircuitBreakerSettings
{
    public int FailureThreshold { get; set; } = 5;
    public int SamplingDurationSeconds { get; set; } = 30;
    public int MinimumThroughput { get; set; } = 10;
    public int BreakDurationSeconds { get; set; } = 30;
}

public class TimeoutSettings
{
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int HttpTimeoutSeconds { get; set; } = 10;
    public int DatabaseTimeoutSeconds { get; set; } = 30;
}
