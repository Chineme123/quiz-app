using System;
using System.Linq;
using System.Threading.Tasks;
using QuizService.Domain.Entities;
using QuizService.Domain.Interfaces;
using QuizService.Application.DTOs;

namespace QuizService.Application.Commands
{
    public class SubmitQuizCommand : IQuizCommand
    {
        private readonly Guid _attemptId;
        private readonly SubmitQuizDto _dto;
        private readonly IQuizRepository _quizRepository; // We might need a specialized repository or just use IRepository<QuizAttempt>

        // In a real app, we'd inject repositories through constructor of the handler, 
        // but here the command object itself often carries the data, and execution logic might be in a handler.
        // However, the prompt asks for "SubmitQuizCommand" to implement "ExecuteAsync". 
        // So we need to inject dependencies into the command or the Invoker passes them.
        // Given constraints, I will inject dependencies into the constructor for now, 
        // typically factories create these commands or we use MediatR. 
        // For this specific pattern request:
        
        private readonly QuizAttempt _attempt;
        
        public SubmitQuizCommand(QuizAttempt attempt, SubmitQuizDto dto)
        {
            _attempt = attempt;
            _dto = dto;
        }

        public Task ExecuteAsync()
        {
            // Idempotency check could be here or before command creation.
            // Requirement: "Submission must be idempotent"
            // If already submitted, maybe do nothing or throw specific error? 
            // The constraint says "Prevent duplicate grading".
            
            if (_attempt.CurrentStateName != "InProgress")
            {
                 // Already submitted or not started. 
                 // If submitted, we can just return (idempotent success)
                 if (_attempt.CurrentStateName == "Submitted" || _attempt.CurrentStateName == "Graded" || _attempt.CurrentStateName == "Reviewable")
                 {
                     return Task.CompletedTask;
                 }
                 // If NotStarted or Abandoned, it's an error?
            }

            var answers = _dto.Responses.Select(r => new QuizAnswer(r.QuestionId, r.Answer));
            _attempt.Submit(answers);
            
            return Task.CompletedTask;
        }
    }
}
