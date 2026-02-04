using System;

namespace QuizService.Domain.Entities
{
    public abstract class Question
    {
        public Guid Id { get; set; }
        public Guid QuizId { get; set; }
        public string Prompt { get; set; }
        public int Points { get; set; }
        
        // Discriminator for EF Core TPH (Table Per Hierarchy)
        public string QuestionType { get; set; } 

        protected Question(string prompt, int points)
        {
            Id = Guid.NewGuid();
            Prompt = prompt;
            Points = points;
        }
        
        protected Question() { } // EF Core
    }
}
