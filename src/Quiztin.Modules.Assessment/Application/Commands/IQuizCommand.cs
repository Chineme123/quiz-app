using System.Threading.Tasks;

namespace Quiztin.Modules.Assessment.Application.Commands
{
    public interface IQuizCommand
    {
        Task ExecuteAsync();
    }
}
