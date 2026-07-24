using System.Linq;
using Quiztin.Modules.Assessment.Infrastructure.Strategies;
using Xunit;

namespace Quiztin.Modules.Assessment.Tests
{
    /// <summary>
    /// The untrusted model output parser (spec 0009, AC-6). Pure, no DB: it proves the tolerant
    /// element by element parse keeps the good candidates when some are bad, drops the invalid
    /// ones, caps text, and never throws on garbage.
    /// </summary>
    public class GeneratedCandidateParserTests
    {
        [Fact]
        public void Keeps_the_valid_candidates_and_drops_the_bad_ones_in_one_array()
        {
            var json = @"[
                {""questionType"":""MultipleChoice"",""prompt"":""2+2?"",""points"":5,""options"":[""3"",""4""],""correctOptionIndex"":1},
                {""questionType"":""MultipleChoice"",""prompt"":""bad, one option"",""points"":5,""options"":[""only""],""correctOptionIndex"":0},
                {""questionType"":""TrueFalse"",""prompt"":""Sky is blue?"",""points"":2,""correctAnswerBool"":true},
                {""questionType"":""ShortAnswer"",""prompt"":""Capital of France?"",""points"":3,""correctAnswerText"":""Paris""},
                {""questionType"":""ShortAnswer"",""prompt"":""blank answer"",""points"":3,""correctAnswerText"":""  ""},
                {""questionType"":""Essay"",""prompt"":""unknown type"",""points"":3},
                {""prompt"":""missing type"",""points"":3},
                ""not even an object""
            ]";

            var result = GeneratedCandidateParser.Parse(json);

            // Only the three well-formed ones survive: MC with two options, TF, SA with an answer.
            Assert.Equal(3, result.Count);
            Assert.Contains(result, c => c.QuestionType == "MultipleChoice" && c.Prompt == "2+2?");
            Assert.Contains(result, c => c.QuestionType == "TrueFalse");
            Assert.Contains(result, c => c.QuestionType == "ShortAnswer" && c.CorrectAnswerText == "Paris");
            // Every survivor carries a distinct id, so an accept request can name them.
            Assert.Equal(3, result.Select(c => c.Id).Distinct().Count());
        }

        [Fact]
        public void Extracts_the_array_from_prose_or_a_markdown_fence()
        {
            var wrapped = "Here you go:\n```json\n[{\"questionType\":\"TrueFalse\",\"prompt\":\"P\",\"points\":1,\"correctAnswerBool\":false}]\n```";
            Assert.Single(GeneratedCandidateParser.Parse(wrapped));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not json at all")]
        [InlineData("{\"questionType\":\"TrueFalse\"}")] // an object, not an array
        [InlineData("[")]                                 // truncated
        public void Returns_empty_on_garbage_and_never_throws(string raw)
        {
            Assert.Empty(GeneratedCandidateParser.Parse(raw));
        }

        [Fact]
        public void Caps_prompt_and_option_length()
        {
            var longPrompt = new string('x', GeneratedCandidateParser.MaxPromptChars + 50);
            var longOption = new string('y', GeneratedCandidateParser.MaxOptionChars + 50);
            var json = $@"[{{""questionType"":""MultipleChoice"",""prompt"":""{longPrompt}"",""points"":1,""options"":[""{longOption}"",""ok""],""correctOptionIndex"":1}}]";

            var result = GeneratedCandidateParser.Parse(json);

            Assert.Single(result);
            Assert.Equal(GeneratedCandidateParser.MaxPromptChars, result[0].Prompt.Length);
            Assert.NotNull(result[0].Options);
            Assert.Equal(GeneratedCandidateParser.MaxOptionChars, result[0].Options![0].Length);
        }
    }
}
