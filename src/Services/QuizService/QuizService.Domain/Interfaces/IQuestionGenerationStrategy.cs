using System.Collections.Generic;
using System.Threading.Tasks;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Interfaces
{
    public interface IQuestionGenerationStrategy
    {
        string ModeName { get; }
        Task<List<Question>> GenerateQuestionsAsync(string topic, int count, string difficulty);
    }
}
