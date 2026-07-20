using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    public class QuizDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public Guid ClassroomId { get; set; }
        public Guid TeacherId { get; set; }

        // Publish state (spec 0009). Publish is the only writer of these; the take path
        // already reads them, so surfacing them here lets the authoring side see what a
        // publish did without a second round trip.
        public bool IsPublished { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableTo { get; set; }
        public int MaxAttempts { get; set; }

        public List<QuestionDto> Questions { get; set; }
    }

    public class QuestionDto
    {
        public Guid Id { get; set; }
        public string QuestionType { get; set; }
        public string Prompt { get; set; }
        public int Points { get; set; }
        public List<string> Options { get; set; } // For MCQ
    }
}
