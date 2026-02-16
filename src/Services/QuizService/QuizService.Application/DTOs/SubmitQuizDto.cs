using System;
using System.Collections.Generic;

namespace QuizService.Application.DTOs
{
    public class SubmitQuizDto
    {
        public Guid CommandId { get; set; }
        public List<QuizAnswerDto> Responses { get; set; } = new();
    }

    public class QuizAnswerDto
    {
        public Guid QuestionId { get; set; }
        public string Answer { get; set; } = string.Empty;
    }
}
