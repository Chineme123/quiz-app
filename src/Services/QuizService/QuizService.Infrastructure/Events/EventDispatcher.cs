using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using QuizService.Domain.Events;
using QuizService.Domain.Observers;

namespace QuizService.Infrastructure.Events
{
    public class EventDispatcher : IEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public EventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync<TEvent>(TEvent domainEvent)
        {
            var observers = _serviceProvider.GetServices<QuizService.Domain.Observers.IObserver<TEvent>>();
            foreach (var observer in observers)
            {
                await observer.UpdateAsync(domainEvent);
            }
        }
    }
}
