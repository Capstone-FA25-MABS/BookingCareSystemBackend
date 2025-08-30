using System;
using System.Threading.Tasks;

namespace BookingCare.Shared.Common.CircuitBreaker
{
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }

    public interface ICircuitBreakerService
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName = "");
        Task ExecuteAsync(Func<Task> operation, string operationName = "");
        T Execute<T>(Func<T> operation, string operationName = "");
        void Execute(Action operation, string operationName = "");
        CircuitBreakerState GetCircuitBreakerState(string key = "default");
        void ResetCircuitBreaker(string key = "default");
    }

    public interface IDatabaseCircuitBreakerService : ICircuitBreakerService
    {
    }

    public interface IHttpClientCircuitBreakerService : ICircuitBreakerService
    {
    }

    public interface IExternalApiCircuitBreakerService : ICircuitBreakerService
    {
    }
}
