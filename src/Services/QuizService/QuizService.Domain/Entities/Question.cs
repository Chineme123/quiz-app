using System;

namespace QuizService.Domain.Entities
{
    public abstract class Question
    {
        public Guid Id { get; set; }
        public Guid QuizId { get; set; }
        public string Prompt { get; set; }
        public int Points { get; set; }
        
        // Discriminator for EF Core TPH (Table Per Hierarchy)
        public string QuestionType { get; set; } 

        protected Question(string prompt, int points)
        {
            Id = Guid.NewGuid();
            Prompt = prompt;
            Points = points;
        }
        
        protected Question() { } // EF Core

        /// <summary>
        /// Grades a student's submitted answer against this question's correct answer.
        /// The question is the Information Expert — it owns its correct answer, so it
        /// decides correctness. Scoring strategies compose these per-question verdicts.
        /// </summary>
        public abstract bool IsCorrect(string providedAnswer);

        /// <summary>
        /// The correct answer as plain text. Used to give the feedback model the reference
        /// answer without exposing the internal encoding (an option index, a bool). The
        /// question owns this too (Information Expert). No identity crosses here (spec 0005,
        /// AC-5).
        /// </summary>
        public abstract string GetCorrectAnswerText();
    }
}
