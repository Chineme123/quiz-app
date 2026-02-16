using System.Threading.Tasks;

namespace QuizService.Domain.Observers
{
    // Generic observer interface
    public interface IObserver<TEvent>
    {
        Task UpdateAsync(TEvent domainEvent);
    }
}
