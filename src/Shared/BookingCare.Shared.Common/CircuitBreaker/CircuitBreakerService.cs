using BookingCare.Shared.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace BookingCare.Shared.Common.CircuitBreaker
{
    public class CircuitBreakerService : ICircuitBreakerService
    {
        private readonly ILogger<CircuitBreakerService> _logger;
        private readonly CircuitBreakerOptions _options;
        private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines;

        public CircuitBreakerService(
            ILogger<CircuitBreakerService> logger,
            IOptions<CircuitBreakerOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _pipelines = new ConcurrentDictionary<string, ResiliencePipeline>();
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "")
        {
            var key = string.IsNullOrEmpty(operationName) ? "default" : operationName;
            var pipeline = GetOrCreatePipeline(key);

            try
            {
                _logger.LogDebug("Executing operation {OperationName} with circuit breaker", operationName);
                
                var result = await pipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    return await operation();
                });

                _logger.LogDebug("Operation {OperationName} completed successfully", operationName);
                return result;
            }
            catch (Exception ex) when (ex.Message.Contains("circuit") || ex.Message.Contains("Circuit"))
            {
                _logger.LogWarning("Circuit breaker is open for operation {OperationName}: {Message}", 
                    operationName, ex.Message);
                throw new ServiceUnavailableException($"Service temporarily unavailable: {operationName}");
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning("Operation {OperationName} timed out: {Message}", operationName, ex.Message);
                throw new TimeoutException($"Operation {operationName} timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing operation {OperationName}", operationName);
                throw;
            }
        }

        public async Task ExecuteAsync(Func<Task> operation, string operationName = "")
        {
            var key = string.IsNullOrEmpty(operationName) ? "default" : operationName;
            var pipeline = GetOrCreatePipeline(key);

            try
            {
                _logger.LogDebug("Executing operation {OperationName} with circuit breaker", operationName);
                
                await pipeline.ExecuteAsync(async (cancellationToken) =>
                {
                    await operation();
                });

                _logger.LogDebug("Operation {OperationName} completed successfully", operationName);
            }
            catch (Exception ex) when (ex.Message.Contains("circuit") || ex.Message.Contains("Circuit"))
            {
                _logger.LogWarning("Circuit breaker is open for operation {OperationName}: {Message}", 
                    operationName, ex.Message);
                throw new ServiceUnavailableException($"Service temporarily unavailable: {operationName}");
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning("Operation {OperationName} timed out: {Message}", operationName, ex.Message);
                throw new TimeoutException($"Operation {operationName} timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing operation {OperationName}", operationName);
                throw;
            }
        }

        public T Execute<T>(Func<T> operation, string operationName = "")
        {
            return ExecuteAsync(() => Task.FromResult(operation()), operationName).GetAwaiter().GetResult();
        }

        public void Execute(Action operation, string operationName = "")
        {
            ExecuteAsync(() => 
            {
                operation();
                return Task.CompletedTask;
            }, operationName).GetAwaiter().GetResult();
        }

        public CircuitBreakerState GetCircuitBreakerState(string key = "default")
        {
            // Since Polly v8 doesn't expose circuit breaker state directly,
            // we'll return a default state. In production, you might want to
            // implement a custom state tracking mechanism.
            return CircuitBreakerState.Closed;
        }

        public void ResetCircuitBreaker(string key = "default")
        {
            _pipelines.TryRemove(key, out _);
            _logger.LogInformation("Circuit breaker {Key} has been reset", key);
        }

        private ResiliencePipeline GetOrCreatePipeline(string key)
        {
            return _pipelines.GetOrAdd(key, _ => CreatePipeline());
        }

        private ResiliencePipeline CreatePipeline()
        {
            return new ResiliencePipelineBuilder()
                .AddTimeout(_options.Timeout)
                .AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    MaxRetryAttempts = _options.RetryCount,
                    Delay = _options.BaseDelay,
                    BackoffType = Polly.DelayBackoffType.Exponential,
                    OnRetry = args =>
                    {
                        _logger.LogWarning("Retry attempt {AttemptNumber} for operation", args.AttemptNumber);
                        return ValueTask.CompletedTask;
                    }
                })
                .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
                {
                    FailureRatio = _options.FailureThreshold,
                    SamplingDuration = TimeSpan.FromSeconds(_options.SamplingDuration),
                    MinimumThroughput = _options.MinimumThroughput,
                    BreakDuration = _options.DurationOfBreak,
                    OnClosed = args =>
                    {
                        _logger.LogInformation("Circuit breaker closed");
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogInformation("Circuit breaker half-opened");
                        return ValueTask.CompletedTask;
                    },
                    OnOpened = args =>
                    {
                        _logger.LogWarning("Circuit breaker opened");
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }
    }

    public class DatabaseCircuitBreakerService : CircuitBreakerService, IDatabaseCircuitBreakerService
    {
        public DatabaseCircuitBreakerService(
            ILogger<DatabaseCircuitBreakerService> logger,
            IOptions<ServiceCircuitBreakerOptions> options)
            : base(logger, new OptionsWrapper<CircuitBreakerOptions>(options.Value.Database))
        {
        }
    }

    public class HttpClientCircuitBreakerService : CircuitBreakerService, IHttpClientCircuitBreakerService
    {
        public HttpClientCircuitBreakerService(
            ILogger<HttpClientCircuitBreakerService> logger,
            IOptions<ServiceCircuitBreakerOptions> options)
            : base(logger, new OptionsWrapper<CircuitBreakerOptions>(options.Value.HttpClient))
        {
        }
    }

    public class ExternalApiCircuitBreakerService : CircuitBreakerService, IExternalApiCircuitBreakerService
    {
        public ExternalApiCircuitBreakerService(
            ILogger<ExternalApiCircuitBreakerService> logger,
            IOptions<ServiceCircuitBreakerOptions> options)
            : base(logger, new OptionsWrapper<CircuitBreakerOptions>(options.Value.ExternalApi))
        {
        }
    }
}
