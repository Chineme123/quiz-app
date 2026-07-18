using System;

namespace Quiztin.Modules.Assessment.Domain.Entities
{
    public class ShortAnswerQuestion : Question
    {
        public string CorrectAnswerText { get; set; }

        public ShortAnswerQuestion(string prompt, int points, string correctAnswerText) 
            : base(prompt, points)
        {
            CorrectAnswerText = correctAnswerText;
            QuestionType = nameof(ShortAnswerQuestion);
        }
        
        protected ShortAnswerQuestion() {}

        // Case-insensitive, trimmed exact match. (Fuzzier matching / AI grading is a later enhancement.)
        public override bool IsCorrect(string providedAnswer)
        {
            return !string.IsNullOrWhiteSpace(providedAnswer)
                && string.Equals(providedAnswer.Trim(), CorrectAnswerText?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override string GetCorrectAnswerText() => CorrectAnswerText;
    }
}
