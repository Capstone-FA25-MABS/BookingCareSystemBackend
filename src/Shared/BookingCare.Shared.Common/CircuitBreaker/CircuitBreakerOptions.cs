using System;

namespace BookingCare.Shared.Common.CircuitBreaker
{
    public class CircuitBreakerOptions
    {
        public int HandledEventsAllowedBeforeBreaking { get; set; } = 5;
        public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);
        public int SamplingDuration { get; set; } = 60;
        public int MinimumThroughput { get; set; } = 5;
        public double FailureThreshold { get; set; } = 0.5;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int RetryCount { get; set; } = 3;
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);
    }

    public class ServiceCircuitBreakerOptions
    {
        public CircuitBreakerOptions Database { get; set; } = new();
        public CircuitBreakerOptions HttpClient { get; set; } = new();
        public CircuitBreakerOptions ExternalApi { get; set; } = new();
    }
}
