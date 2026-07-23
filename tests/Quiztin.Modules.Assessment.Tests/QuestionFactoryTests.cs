using System.Collections.Generic;
using Quiztin.Modules.Assessment.Domain.Entities;
using Quiztin.Modules.Assessment.Domain.Factories;
using Xunit;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// The one question validation rule set (spec 0009, AC-3, AC-6). Pure domain, no database:
    /// TryCreate reports success or failure instead of throwing, and both the manual authoring
    /// path and generation run through it, so this is the single place a question is judged well
    /// formed. Cheap to run, so it covers the matrix the persistence tests should not have to.
    /// </summary>
    public class QuestionFactoryTests
    {
        [Fact]
        public void MultipleChoice_with_two_options_and_an_in_range_index_is_valid()
        {
            var result = QuestionFactory.TryCreateMultipleChoice("Pick one", 5, new List<string> { "A", "B" }, 1);

            Assert.True(result.IsSuccess);
            Assert.IsType<MultipleChoiceQuestion>(result.Question);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void MultipleChoice_needs_at_least_two_options(int optionCount)
        {
            var options = new List<string>();
            for (var i = 0; i < optionCount; i++) options.Add($"opt{i}");

            var result = QuestionFactory.TryCreateMultipleChoice("Pick one", 5, options, 0);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Question);
        }

        [Fact]
        public void MultipleChoice_rejects_a_null_option_list()
        {
            Assert.False(QuestionFactory.TryCreateMultipleChoice("Pick one", 5, null, 0).IsSuccess);
        }

        [Fact]
        public void MultipleChoice_rejects_a_blank_option()
        {
            Assert.False(QuestionFactory.TryCreateMultipleChoice("Pick one", 5, new List<string> { "A", "  " }, 0).IsSuccess);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void MultipleChoice_rejects_an_out_of_range_correct_index(int index)
        {
            Assert.False(QuestionFactory.TryCreateMultipleChoice("Pick one", 5, new List<string> { "A", "B" }, index).IsSuccess);
        }

        [Fact]
        public void TrueFalse_is_valid_with_a_prompt_and_positive_points()
        {
            var result = QuestionFactory.TryCreateTrueFalse("The sky is blue", 3, true);

            Assert.True(result.IsSuccess);
            Assert.IsType<TrueFalseQuestion>(result.Question);
        }

        [Fact]
        public void ShortAnswer_needs_a_correct_answer()
        {
            Assert.False(QuestionFactory.TryCreateShortAnswer("Capital of France", 2, "   ").IsSuccess);
        }

        [Fact]
        public void ShortAnswer_is_valid_with_a_correct_answer()
        {
            var result = QuestionFactory.TryCreateShortAnswer("Capital of France", 2, "Paris");

            Assert.True(result.IsSuccess);
            Assert.IsType<ShortAnswerQuestion>(result.Question);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void A_blank_prompt_is_rejected_for_every_type(string? prompt)
        {
            Assert.False(QuestionFactory.TryCreateTrueFalse(prompt, 1, true).IsSuccess);
            Assert.False(QuestionFactory.TryCreateShortAnswer(prompt, 1, "x").IsSuccess);
            Assert.False(QuestionFactory.TryCreateMultipleChoice(prompt, 1, new List<string> { "A", "B" }, 0).IsSuccess);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Points_must_be_positive(int points)
        {
            Assert.False(QuestionFactory.TryCreateTrueFalse("Prompt", points, true).IsSuccess);
        }

        [Fact]
        public void Failures_accumulate_every_reason_at_once()
        {
            // Blank prompt AND zero points: the caller should see both reasons, not just the first.
            var result = QuestionFactory.TryCreateTrueFalse("  ", 0, true);

            Assert.False(result.IsSuccess);
            Assert.Equal(2, result.Errors.Count);
        }
    }
}
