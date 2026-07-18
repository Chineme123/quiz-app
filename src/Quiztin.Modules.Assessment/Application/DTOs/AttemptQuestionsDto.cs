using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// Everything the take screen needs for one attempt (spec 0006, AC-4). Read through the
    /// attempt, which is itself the authorisation, so this never uses the unscoped quiz read.
    ///
    /// It deliberately carries no correct answers. A student holds this payload in their
    /// browser, so anything in it is effectively public to them.
    /// </summary>
    public class AttemptQuestionsDto
    {
        public Guid AttemptId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;

        /// <summary>The attempt's state, so a client resuming a finished attempt can tell.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>When this attempt's time runs out, pinned when it started.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// The server's clock at the moment of this read. The client counts down on the gap
        /// between this and ExpiresAt rather than trusting its own clock, so a device set to
        /// the wrong time still shows the true time remaining (AC-10).
        /// </summary>
        public DateTime ServerNow { get; set; }

        /// <summary>The answers already saved, so resuming restores the student's work (AC-9).</summary>
        public Dictionary<Guid, string> DraftAnswers { get; set; } = new();

        public List<AttemptQuestionDto> Questions { get; set; } = new();
    }

    /// <summary>One question as the student taking it may see it: never the correct answer.</summary>
    public class AttemptQuestionDto
    {
        public Guid Id { get; set; }

        /// <summary>MultipleChoiceQuestion, TrueFalseQuestion, or ShortAnswerQuestion, matching
        /// the persisted discriminator so the client can pick its input by exact name.</summary>
        public string QuestionType { get; set; } = string.Empty;

        public string Prompt { get; set; } = string.Empty;
        public int Points { get; set; }

        /// <summary>The choices, for a multiple choice question only; null otherwise.</summary>
        public List<string>? Options { get; set; }
    }
}
