using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// The submit body (spec 0006, AC-11). It carries no answers: the drafts already saved
    /// on the server are the single source of truth, so a request body cannot disagree with
    /// them, and the normal path and the expiry path grade exactly the same thing.
    /// </summary>
    public class SubmitQuizDto
    {
        /// <summary>
        /// Idempotency key. An exact replay returns the existing result rather than grading
        /// a second time.
        /// </summary>
        public Guid CommandId { get; set; }
    }

    /// <summary>
    /// The draft save body (spec 0006, AC-6). The client sends every answer it currently
    /// holds and the server replaces the whole set, so there is no read then modify then
    /// write and two saves in flight cannot interleave and silently drop an answer.
    /// </summary>
    public class SaveDraftAnswersDto
    {
        public Dictionary<Guid, string> Answers { get; set; } = new();
    }
}
