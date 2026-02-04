using System;

namespace QuizService.Application.DTOs
{
    public class CreateQuizDto
    {
        public string Title { get; set; }
        public int DurationMinutes { get; set; }
        // ClassroomId usually comes from the URL, but DTO can carry it if needed.
        // TeacherId comes from JWT.
    }
}
