using System;

namespace QuizService.Domain.Entities
{
    public class ProcessedCommand
    {
        public Guid Id { get; private set; }
        public Guid CommandId { get; private set; }
        public string CommandType { get; private set; }
        public DateTime ProcessedAt { get; private set; }

        public ProcessedCommand(Guid commandId, string commandType)
        {
            Id = Guid.NewGuid();
            CommandId = commandId;
            CommandType = commandType;
            ProcessedAt = DateTime.UtcNow;
        }

        private ProcessedCommand() { }
    }
}
