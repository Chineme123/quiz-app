using System.Collections.Generic;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    public class AddQuestionDto
    {
        public string QuestionType { get; set; } // "MultipleChoice", "TrueFalse", "ShortAnswer"
        public string Prompt { get; set; }
        public int Points { get; set; }

        // MCQ
        public List<string> Options { get; set; }
        public int CorrectOptionIndex { get; set; }

        // TrueFalse
        public bool CorrectAnswerBool { get; set; }

        // ShortAnswer
        public string CorrectAnswerText { get; set; }
    }
}
