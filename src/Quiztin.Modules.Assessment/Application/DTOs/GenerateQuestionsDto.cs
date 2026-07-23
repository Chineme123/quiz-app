namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// Inputs for a generation request (spec 0009). Only these reach the model: topic, difficulty,
    /// and count, never any identity (security.md section 2). Task 4 adds pasted source text and a
    /// file part alongside these.
    /// </summary>
    public class GenerateQuestionsDto
    {
        public string? Topic { get; set; }
        public string? Difficulty { get; set; }
        public int Count { get; set; }
    }
}
