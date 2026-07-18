using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quiztin.Modules.Assessment.Application.Commands;

namespace Quiztin.Modules.Assessment.Application.Invokers
{
    public class QuizCommandInvoker
    {
        // Could add history tracking here if needed
        
        public async Task ExecuteCommandAsync(IQuizCommand command)
        {
            await command.ExecuteAsync();
        }
    }
}
