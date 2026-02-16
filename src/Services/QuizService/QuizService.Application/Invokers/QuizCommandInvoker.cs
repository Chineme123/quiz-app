using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuizService.Application.Commands;

namespace QuizService.Application.Invokers
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
