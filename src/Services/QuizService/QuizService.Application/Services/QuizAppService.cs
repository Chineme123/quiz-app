using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuizService.Application.DTOs;
using QuizService.Application.Interfaces;
using QuizService.Domain.Entities;
using QuizService.Domain.Factories;
using QuizService.Domain.Interfaces;

namespace QuizService.Application.Services
{
    public class QuizAppService : IQuizAppService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IEnumerable<IQuestionGenerationStrategy> _strategies;

        public QuizAppService(IQuizRepository quizRepository, IEnumerable<IQuestionGenerationStrategy> strategies)
        {
            _quizRepository = quizRepository;
            _strategies = strategies;
        }

        public async Task<QuizDto> CreateQuizAsync(Guid classroomId, string teacherId, CreateQuizDto input)
        {
            // Verify classroom ownership (optional if Repository handles it, but good to check)
            var classroom = await _quizRepository.GetClassroomAsync(classroomId);
            if (classroom == null)
            {
                throw new KeyNotFoundException($"Classroom with ID {classroomId} not found.");
            }
            if (classroom.TeacherId != teacherId)
            {
                throw new UnauthorizedAccessException("Teacher does not own this classroom.");
            }

            var quiz = new Quiz(classroomId, input.Title, input.DurationMinutes, teacherId);
            await _quizRepository.AddAsync(quiz);

            return MapToDto(quiz);
        }

        public async Task<QuizDto> AddQuestionAsync(Guid quizId, string teacherId, AddQuestionDto input)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null) throw new KeyNotFoundException("Quiz not found.");
            if (quiz.CreatedByTeacherId != teacherId) throw new UnauthorizedAccessException("Not authorized to modify this quiz.");

            Question question;
            switch (input.QuestionType)
            {
                case "MultipleChoice":
                    question = QuestionFactory.CreateMultipleChoice(input.Prompt, input.Points, input.Options, input.CorrectOptionIndex);
                    break;
                case "TrueFalse":
                    question = QuestionFactory.CreateTrueFalse(input.Prompt, input.Points, input.CorrectAnswerBool);
                    break;
                case "ShortAnswer":
                    question = QuestionFactory.CreateShortAnswer(input.Prompt, input.Points, input.CorrectAnswerText);
                    break;
                default:
                    throw new ArgumentException("Invalid question type.");
            }

            question.QuizId = quiz.Id;
            quiz.Questions.Add(question);
            await _quizRepository.UpdateAsync(quiz);

            return MapToDto(quiz);
        }

        public async Task<QuizDto> GenerateQuestionsAsync(Guid quizId, string teacherId, GenerateQuestionsDto input)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId);
            if (quiz == null) throw new KeyNotFoundException("Quiz not found.");
            if (quiz.CreatedByTeacherId != teacherId) throw new UnauthorizedAccessException("Not authorized.");

            var strategy = _strategies.FirstOrDefault(s => s.ModeName.Equals(input.Mode, StringComparison.OrdinalIgnoreCase));
            if (strategy == null)
            {
                throw new ArgumentException($"Strategy '{input.Mode}' not found.");
            }

            var generatedQuestions = await strategy.GenerateQuestionsAsync(input.Topic, input.Count, input.Difficulty);
            
            foreach (var q in generatedQuestions)
            {
                q.QuizId = quizId;
                quiz.Questions.Add(q);
            }

            await _quizRepository.UpdateAsync(quiz);
            return MapToDto(quiz);
        }

        public async Task<QuizDto> GetQuizAsync(Guid quizId)
        {
             var quiz = await _quizRepository.GetByIdAsync(quizId);
             if (quiz == null) throw new KeyNotFoundException("Quiz not found.");
             return MapToDto(quiz);
        }

        private QuizDto MapToDto(Quiz quiz)
        {
            return new QuizDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                DurationMinutes = quiz.DurationMinutes,
                ClassroomId = quiz.ClassroomId,
                TeacherId = quiz.CreatedByTeacherId,
                Questions = quiz.Questions.Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Prompt = q.Prompt,
                    Points = q.Points,
                    QuestionType = q.QuestionType,
                    Options = (q as MultipleChoiceQuestion)?.Options
                }).ToList()
            };
        }
    }
}
