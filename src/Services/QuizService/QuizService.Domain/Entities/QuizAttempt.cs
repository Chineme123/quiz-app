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

        /// <summary>
        /// The instant this attempt's time runs out, pinned at <see cref="Start"/> (spec 0006,
        /// AC-3). Deliberately stored rather than derived from the quiz: DurationMinutes is
        /// mutable, so a teacher editing it would otherwise silently move the clock on a
        /// student already running. Pinning makes the deadline a contract, and gives the
        /// client countdown and the server one instant to agree on.
        /// </summary>
        public DateTime ExpiresAt { get; private set; }

        public DateTime? SubmittedAt { get; private set; }
        public DateTime? GradedAt { get; private set; }
        public decimal? TotalScore { get; set; }
        public string CurrentStateName { get; private set; }

        /// <summary>
        /// Why this attempt was abandoned, null unless it is Abandoned (spec 0006, AC-15).
        /// Stored because the attempt limit treats the reasons differently: only a
        /// superseded attempt consumes one of the quiz's MaxAttempts.
        /// </summary>
        public AbandonReason? AbandonReason { get; private set; }

        // Feedback lifecycle (spec 0005). Grading sets Pending; the background job sets
        // Ready once per answer feedback is written. Score is readable while Pending.
        public FeedbackStatus FeedbackStatus { get; private set; } = FeedbackStatus.Pending;
        public DateTime? FeedbackGeneratedAt { get; private set; }

        private List<QuizAnswer> _answers = new();
        public IReadOnlyCollection<QuizAnswer> Answers => _answers.AsReadOnly();

        // The in progress answers, stored as one jsonb column (spec 0006). They live here
        // rather than as QuizAnswer rows so that an answer row always means a submitted
        // answer, never provisional work, and so a save is one whole set write with no
        // per question upsert to race.
        private Dictionary<Guid, string> _draftAnswers = new();
        public IReadOnlyDictionary<Guid, string> DraftAnswers => _draftAnswers;

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

        /// <summary>
        /// Begins the attempt and pins its deadline from the quiz's duration (spec 0006, AC-3).
        /// The duration is passed in rather than read from the quiz so Domain keeps no
        /// reference to the quiz aggregate, and so the value is captured once, at this instant.
        /// </summary>
        public void Start(int durationMinutes)
        {
            _currentState.Start(this);
            StartedAt = DateTime.UtcNow;
            ExpiresAt = StartedAt.AddMinutes(durationMinutes);
        }

        /// <summary>
        /// Replaces the whole draft set in one write (spec 0006, AC-6). A whole set replace,
        /// never a merge: the server never reads then modifies then writes, so two saves in
        /// flight cannot interleave and drop an answer. The last write simply wins.
        /// Refuses once the attempt is not running or its time is up (AC-7), which is what
        /// makes the deadline server enforced rather than merely counted down by the client.
        /// </summary>
        public void SaveDraftAnswers(IReadOnlyDictionary<Guid, string> answers, DateTime now)
        {
            if (CurrentStateName != "InProgress")
                throw new InvalidOperationException($"Cannot save answers from state {CurrentStateName}.");
            if (now > ExpiresAt)
                throw new InvalidOperationException("Cannot save answers after the attempt has expired.");

            _draftAnswers = new Dictionary<Guid, string>(answers);
        }

        /// <summary>
        /// Submits the answers already saved (spec 0006, AC-11). Takes nothing: the saved
        /// drafts are the single source of truth, so a request body cannot disagree with
        /// them, and the normal path and the expiry path become the same path. The drafts
        /// become QuizAnswer rows here, which is the first moment answer rows exist.
        /// </summary>
        public void Submit()
        {
            _answers.Clear();
            foreach (var draft in _draftAnswers)
                _answers.Add(new QuizAnswer(draft.Key, draft.Value));

            _currentState.Submit(this);
            SubmittedAt = DateTime.UtcNow;
            // The drafts are now answers; keeping both would be two sources of truth.
            _draftAnswers.Clear();
        }

        /// <summary>
        /// Abandons a running attempt (foundation §69 triggers 3 and 4, spec 0006). The reason
        /// is recorded because the attempt limit treats them differently: a superseded attempt
        /// costs the student one of MaxAttempts, an explicit quit does not (AC-15).
        /// </summary>
        public void Abandon(AbandonReason reason)
        {
            _currentState.Abandon(this);
            AbandonReason = reason;
            _draftAnswers.Clear();
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
