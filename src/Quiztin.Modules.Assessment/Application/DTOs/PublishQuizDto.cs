using System;

namespace Quiztin.Modules.Assessment.Application.DTOs
{
    /// <summary>
    /// What a teacher sets when publishing a quiz (spec 0009). Publish is the first and only
    /// writer of these fields, and the take path (`Quiz.CanStart`, the available quizzes read)
    /// already reads them, so publish needs no take side change.
    /// </summary>
    public class PublishQuizDto
    {
        /// <summary>Null means no lower bound (available immediately).</summary>
        public DateTime? AvailableFrom { get; set; }

        /// <summary>Null means no upper bound (available indefinitely).</summary>
        public DateTime? AvailableTo { get; set; }

        public int MaxAttempts { get; set; }
    }
}
