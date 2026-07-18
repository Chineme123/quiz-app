namespace Quiztin.Modules.Assessment.Domain.Enums
{
    /// <summary>
    /// Where an attempt is in the feedback lifecycle. Grading sets <see cref="Pending"/>;
    /// the background feedback job sets <see cref="Ready"/> when the per answer feedback
    /// is written. The score is readable from grading onward, while feedback is still
    /// <see cref="Pending"/> (spec 0005, AC-7).
    /// </summary>
    public enum FeedbackStatus
    {
        Pending,
        Ready
    }
}
