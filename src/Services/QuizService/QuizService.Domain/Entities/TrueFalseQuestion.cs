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
    }
}
