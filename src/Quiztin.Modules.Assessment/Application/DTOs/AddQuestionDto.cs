using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    public class AddQuestionDto
    {
        // Every field below is optional on the wire: only the ones a given type needs are read,
        // and QuestionFactory.TryCreate is the single gate that decides whether what arrived is a
        // valid question of that type (spec 0009). Nullable so a TrueFalse add need not send an
        // empty Options list, nor a MultipleChoice add a dummy CorrectAnswerText.
        public string? QuestionType { get; set; } // "MultipleChoice", "TrueFalse", "ShortAnswer"
        public string? Prompt { get; set; }
        public int Points { get; set; }

        // MultipleChoice
        public List<string>? Options { get; set; }
        public int CorrectOptionIndex { get; set; }

        // TrueFalse
        public bool CorrectAnswerBool { get; set; }

        // ShortAnswer
        public string? CorrectAnswerText { get; set; }
    }
}
