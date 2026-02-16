using System.Threading.Tasks;

namespace QuizService.Application.Commands
{
    public interface IQuizCommand
    {
        Task ExecuteAsync();
    }
}
