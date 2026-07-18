using System.Threading.Tasks;

namespace Quiztin.Modules.Assessment.Domain.Events
{
    public interface IEventDispatcher
    {
        Task DispatchAsync<TEvent>(TEvent domainEvent);
    }
}
