using System.Threading.Tasks;

namespace Quiztin.Modules.Assessment.Domain.Observers
{
    // Generic observer interface
    public interface IObserver<TEvent>
    {
        Task UpdateAsync(TEvent domainEvent);
    }
}
