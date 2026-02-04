namespace QuizService.Domain.Entities
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
    }
}
