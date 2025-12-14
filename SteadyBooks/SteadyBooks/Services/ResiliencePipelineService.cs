using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using SteadyBooks.Configuration;

namespace SteadyBooks.Services;

public interface IResiliencePipelineService
{
    ResiliencePipeline GetHttpPipeline();
    ResiliencePipeline GetDatabasePipeline();
    ResiliencePipeline<TResult> GetHttpPipeline<TResult>();
    ResiliencePipeline<TResult> GetDatabasePipeline<TResult>();
}

public class ResiliencePipelineService : IResiliencePipelineService
{
    private readonly ResiliencePipeline _httpPipeline;
    private readonly ResiliencePipeline _databasePipeline;
    private readonly ILogger<ResiliencePipelineService> _logger;

    public ResiliencePipelineService(
        ILogger<ResiliencePipelineService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        var pollySettings = configuration.GetSection("Polly").Get<PollySettings>() ?? new PollySettings();

        _httpPipeline = CreateHttpPipeline(pollySettings);
        _databasePipeline = CreateDatabasePipeline(pollySettings);
    }

    public ResiliencePipeline GetHttpPipeline() => _httpPipeline;
    public ResiliencePipeline GetDatabasePipeline() => _databasePipeline;

    public ResiliencePipeline<TResult> GetHttpPipeline<TResult>()
    {
        var options = new ResiliencePipelineBuilder<TResult>()
            .AddRetry(new RetryStrategyOptions<TResult>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "HTTP request retry attempt {AttemptNumber} after {Duration}ms. Exception: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<TResult>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnOpened = args =>
                {
                    _logger.LogError("Circuit breaker opened due to failures");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed, service recovered");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker half-opened, testing service");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();

        return options;
    }

    public ResiliencePipeline<TResult> GetDatabasePipeline<TResult>()
    {
        var options = new ResiliencePipelineBuilder<TResult>()
            .AddRetry(new RetryStrategyOptions<TResult>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<TResult>()
                    .Handle<Npgsql.NpgsqlException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Database operation retry attempt {AttemptNumber} after {Duration}ms. Exception: {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();

        return options;
    }

    private ResiliencePipeline CreateHttpPipeline(PollySettings settings)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = settings.Retry.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(settings.Retry.InitialDelayMilliseconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "HTTP request retry attempt {AttemptNumber} after {Duration}ms",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(settings.CircuitBreaker.SamplingDurationSeconds),
                MinimumThroughput = settings.CircuitBreaker.MinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(settings.CircuitBreaker.BreakDurationSeconds),
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutException>(),
                OnOpened = args =>
                {
                    _logger.LogError("HTTP Circuit breaker opened");
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(settings.Timeout.HttpTimeoutSeconds))
            .Build();
    }

    private ResiliencePipeline CreateDatabasePipeline(PollySettings settings)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = settings.Retry.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(settings.Retry.InitialDelayMilliseconds * 2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<Npgsql.NpgsqlException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Database operation retry attempt {AttemptNumber} after {Duration}ms",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(settings.Timeout.DatabaseTimeoutSeconds))
            .Build();
    }
}
