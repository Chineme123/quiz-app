using System;
using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Domain.Entities
{
    public class MultipleChoiceQuestion : Question
    {
        // Storing options as a simplified list or JSON string might be better for simple microservice demos, 
        // but let's assume a simplified list of strings logic for this UC.
        // For EF Core, we might need a value converter or owned entity, but let's keep it simple: 
        // We'll model it as properties for now or simple collection if we set up the context right.
        // Let's go with a simple joined string or primitive collection for simplicity in this specific UC prompt
        // unless we want a separate Option table. Separate table is cleaner.
        
        // However, to keep it "contained" within the Question, we can say:
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectOptionIndex { get; set; }

        public MultipleChoiceQuestion(string prompt, int points, List<string> options, int correctOptionIndex) 
            : base(prompt, points)
        {
            Options = options;
            CorrectOptionIndex = correctOptionIndex;
            QuestionType = nameof(MultipleChoiceQuestion);
        }

        protected MultipleChoiceQuestion() {}

        // The submitted answer is the selected option index (e.g. "2"); fall back to
        // matching the option text so either encoding grades correctly.
        public override bool IsCorrect(string providedAnswer)
        {
            if (string.IsNullOrWhiteSpace(providedAnswer)) return false;
            var value = providedAnswer.Trim();
            if (int.TryParse(value, out var index))
                return index == CorrectOptionIndex;
            return CorrectOptionIndex >= 0 && CorrectOptionIndex < Options.Count
                && string.Equals(Options[CorrectOptionIndex]?.Trim(), value, StringComparison.OrdinalIgnoreCase);
        }

        public override string GetCorrectAnswerText()
            => CorrectOptionIndex >= 0 && CorrectOptionIndex < Options.Count
                ? Options[CorrectOptionIndex]
                : CorrectOptionIndex.ToString();
    }
}
