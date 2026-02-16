using System.Threading.Tasks;

namespace QuizService.Domain.Events
{
    public interface IEventDispatcher
    {
        Task DispatchAsync<TEvent>(TEvent domainEvent);
    }
}
