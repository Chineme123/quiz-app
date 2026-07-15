using System;
using System.Collections.Generic;
using System.Linq;
using QuizService.Domain.Enums;
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

        // Feedback lifecycle (spec 0005). Grading sets Pending; the background job sets
        // Ready once per answer feedback is written. Score is readable while Pending.
        public FeedbackStatus FeedbackStatus { get; private set; } = FeedbackStatus.Pending;
        public DateTime? FeedbackGeneratedAt { get; private set; }

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

        public void Evaluate(IScoringStrategy strategy, IReadOnlyList<Question> questions)
        {
            _currentState.Evaluate(this);
            strategy.Score(this, questions);
            GradedAt = DateTime.UtcNow;
            // Feedback comes later, in the background; the attempt is graded and Pending.
            FeedbackStatus = FeedbackStatus.Pending;
        }

        /// <summary>
        /// Completes the attempt after the feedback strategy has written the per answer
        /// feedback: moves Graded to Reviewable, marks feedback Ready, and stamps the time.
        /// The strategy runs outside the entity (in the background job), so this stays pure.
        /// Guarded: a redelivered graded event finds it already Ready and this is a safe
        /// no op, never a second transition or throw (spec 0005, AC-2, AC-7).
        /// </summary>
        public void MarkFeedbackReady()
        {
            if (FeedbackStatus == FeedbackStatus.Ready) return;
            _currentState.GenerateFeedback(this); // Graded -> Reviewable
            FeedbackStatus = FeedbackStatus.Ready;
            FeedbackGeneratedAt = DateTime.UtcNow;
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
