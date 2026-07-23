using System.Collections.Generic;
using Quiztin.Modules.Assessment.Domain.Entities;

namespace Quiztin.Modules.Assessment.Domain.Factories
{
    /// <summary>
    /// Builds questions from validated input. Every rule that decides whether a question is well
    /// formed lives here, once, so the manual add and edit paths and the generation candidate
    /// check (spec 0009) cannot drift on what a valid question is. The methods report success or
    /// failure through <see cref="QuestionCreationResult"/> rather than throwing, so validating a
    /// batch of candidates is never a matter of catching an exception per element.
    /// </summary>
    public static class QuestionFactory
    {
        // Shared across every type: a question with no prompt, or a point value that is not
        // positive, is malformed whatever its type. Errors accumulate so a caller sees every
        // problem at once rather than one at a time.
        private static void ValidateCommon(string? prompt, int points, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                errors.Add("A question needs a prompt.");
            if (points <= 0)
                errors.Add("A question's points must be greater than zero.");
        }

        public static QuestionCreationResult TryCreateMultipleChoice(string? prompt, int points, List<string>? options, int correctOptionIndex)
        {
            var errors = new List<string>();
            ValidateCommon(prompt, points, errors);

            if (options == null || options.Count < 2)
                errors.Add("A multiple choice question needs at least two options.");
            else if (options.Exists(o => string.IsNullOrWhiteSpace(o)))
                errors.Add("A multiple choice option cannot be blank.");
            else if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
                errors.Add("The correct option index is outside the range of options.");

            if (errors.Count > 0)
                return QuestionCreationResult.Failure(errors);

            return QuestionCreationResult.Success(
                new MultipleChoiceQuestion(prompt!, points, options!, correctOptionIndex));
        }

        public static QuestionCreationResult TryCreateTrueFalse(string? prompt, int points, bool correctAnswer)
        {
            var errors = new List<string>();
            ValidateCommon(prompt, points, errors);

            // A boolean answer cannot be malformed, so there is nothing further to check (spec 0009).
            if (errors.Count > 0)
                return QuestionCreationResult.Failure(errors);

            return QuestionCreationResult.Success(new TrueFalseQuestion(prompt!, points, correctAnswer));
        }

        public static QuestionCreationResult TryCreateShortAnswer(string? prompt, int points, string? correctAnswerText)
        {
            var errors = new List<string>();
            ValidateCommon(prompt, points, errors);

            if (string.IsNullOrWhiteSpace(correctAnswerText))
                errors.Add("A short answer question needs a correct answer.");

            if (errors.Count > 0)
                return QuestionCreationResult.Failure(errors);

            return QuestionCreationResult.Success(new ShortAnswerQuestion(prompt!, points, correctAnswerText!));
        }
    }
}
