using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quiztin.Modules.Assessment.Domain.Events;
using Quiztin.Modules.Assessment.Domain.Observers;

namespace Quiztin.Modules.Assessment.Infrastructure.Events
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
            var observers = _serviceProvider.GetServices<Quiztin.Modules.Assessment.Domain.Observers.IObserver<TEvent>>();
            foreach (var observer in observers)
            {
                await observer.UpdateAsync(domainEvent);
            }
        }
    }
}
