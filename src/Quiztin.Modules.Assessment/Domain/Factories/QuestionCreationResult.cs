using System;
using System.Collections.Generic;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Factories
{
    /// <summary>
    /// The outcome of trying to build a question from untrusted input. It reports success or
    /// failure instead of throwing, so both the manual authoring path and the generation
    /// candidate check (spec 0009) run every input through one rule set, and neither has to
    /// catch exceptions to steer control flow. On success it carries the constructed question;
    /// on failure it carries the reasons, so a caller can surface them or drop the candidate.
    /// </summary>
    public sealed class QuestionCreationResult
    {
        public bool IsSuccess { get; }
        public Question? Question { get; }
        public IReadOnlyList<string> Errors { get; }

        private QuestionCreationResult(bool isSuccess, Question? question, IReadOnlyList<string> errors)
        {
            IsSuccess = isSuccess;
            Question = question;
            Errors = errors;
        }

        public static QuestionCreationResult Success(Question question) =>
            new(true, question, Array.Empty<string>());

        public static QuestionCreationResult Failure(IReadOnlyList<string> errors) =>
            new(false, null, errors);

        public static QuestionCreationResult Failure(string error) =>
            new(false, null, new[] { error });
    }
}
