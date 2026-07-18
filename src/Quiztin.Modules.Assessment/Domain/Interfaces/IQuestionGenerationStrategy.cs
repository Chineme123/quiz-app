using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Interfaces
{
    public interface IQuestionGenerationStrategy
    {
        string ModeName { get; }
        Task<List<Question>> GenerateQuestionsAsync(string topic, int count, string difficulty);
    }
}
