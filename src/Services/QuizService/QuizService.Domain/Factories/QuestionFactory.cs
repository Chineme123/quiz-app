using System;
using System.Collections.Generic;
using QuizService.Domain.Entities;

namespace QuizService.Domain.Factories
{
    public static class QuestionFactory
    {
        public static MultipleChoiceQuestion CreateMultipleChoice(string prompt, int points, List<string> options, int correctIndex)
        {
            if (options == null || options.Count < 2)
                throw new ArgumentException("Multiple choice questions must have at least 2 options.");
            
            if (correctIndex < 0 || correctIndex >= options.Count)
                throw new ArgumentException("Correct option index is out of bounds.");

            return new MultipleChoiceQuestion(prompt, points, options, correctIndex);
        }

        public static TrueFalseQuestion CreateTrueFalse(string prompt, int points, bool correctAnswer)
        {
            return new TrueFalseQuestion(prompt, points, correctAnswer);
        }

        public static ShortAnswerQuestion CreateShortAnswer(string prompt, int points, string correctAnswerText)
        {
            if (string.IsNullOrWhiteSpace(correctAnswerText))
                throw new ArgumentException("Correct answer text cannot be empty.");

            return new ShortAnswerQuestion(prompt, points, correctAnswerText);
        }
    }
}
