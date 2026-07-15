using System;

namespace QuizService.Domain.Entities
{
    public class TrueFalseQuestion : Question
    {
        public bool CorrectAnswer { get; set; }

        public TrueFalseQuestion(string prompt, int points, bool correctAnswer) 
            : base(prompt, points)
        {
            CorrectAnswer = correctAnswer;
            QuestionType = nameof(TrueFalseQuestion);
        }
        
        protected TrueFalseQuestion() {}

        public override bool IsCorrect(string providedAnswer)
        {
            return bool.TryParse(providedAnswer?.Trim(), out var value) && value == CorrectAnswer;
        }

        public override string GetCorrectAnswerText() => CorrectAnswer ? "True" : "False";
    }
}
