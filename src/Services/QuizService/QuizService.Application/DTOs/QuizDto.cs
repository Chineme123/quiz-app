using System;
using System.Collections.Generic;

namespace QuizService.Application.DTOs
{
    public class QuizDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        public Guid ClassroomId { get; set; }
        public string TeacherId { get; set; }
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
