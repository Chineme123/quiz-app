using System;
using System.Collections.Generic;
using System.Linq;
using QuizService.Domain.States;
using QuizService.Domain.Strategies;

namespace QuizService.Domain.Entities
{
    public class QuizAttempt
    {
        public Guid Id { get; private set; }
        public Guid QuizId { get; private set; }
        public Guid StudentId { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? SubmittedAt { get; private set; }
        public DateTime? GradedAt { get; private set; }
        public decimal? TotalScore { get; set; }
        public string CurrentStateName { get; private set; }
        public byte[] RowVersion { get; set; } // For Optimistic Concurrency

        private List<QuizAnswer> _answers = new();
        public IReadOnlyCollection<QuizAnswer> Answers => _answers.AsReadOnly();

        // Not mapped to DB directly, used for runtime logic
        private QuizAttemptState _currentState;

        public QuizAttempt(Guid quizId, Guid studentId)
        {
            Id = Guid.NewGuid();
            QuizId = quizId;
            StudentId = studentId;
            TransitionTo(new NotStartedState());
        }

        // EF Core constructor
        private QuizAttempt() { }

        public void TransitionTo(QuizAttemptState state)
        {
            _currentState = state;
            CurrentStateName = state.Name;
        }

        public void Start()
        {
            _currentState.Start(this);
            StartedAt = DateTime.UtcNow;
        }

        public void Submit(IEnumerable<QuizAnswer> answers)
        {
             // Update answers
             _answers.Clear();
             _answers.AddRange(answers);
            _currentState.Submit(this);
            SubmittedAt = DateTime.UtcNow;
        }

        public void Evaluate(IScoringStrategy strategy)
        {
            _currentState.Evaluate(this);
            strategy.Score(this);
            GradedAt = DateTime.UtcNow;
        }

        public void GenerateFeedback(IFeedbackStrategy strategy)
        {
            _currentState.GenerateFeedback(this);
            strategy.Generate(this);
        }
        
        // Helper to hydrate state from name after EF load
        public void LoadState()
        {
            _currentState = CurrentStateName switch
            {
                "NotStarted" => new NotStartedState(),
                "InProgress" => new InProgressState(),
                "Submitted" => new SubmittedState(),
                "Graded" => new GradedState(),
                "Reviewable" => new ReviewableState(),
                "Abandoned" => new AbandonedState(),
                _ => throw new InvalidOperationException($"Unknown state: {CurrentStateName}")
            };
        }
    }
}
