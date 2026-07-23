using System;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// One row of a teacher's per class quiz list (spec 0009, AC-10): enough to show a quiz and
    /// its state without loading every question. AttemptCount is how many attempts students have
    /// started; once it is above zero the quiz's question set is locked (AC-9).
    /// </summary>
    public class QuizSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int QuestionCount { get; set; }
        public int AttemptCount { get; set; }
    }
}
