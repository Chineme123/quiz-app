namespace QuizService.Application.DTOs
{
    public class GenerateQuestionsDto
    {
        public string Mode { get; set; } // "LLM", "Template", etc.
        public string Topic { get; set; }
        public int Count { get; set; }
        public string Difficulty { get; set; }
    }
}
