﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace Revenj.DomainPatterns
{
	internal class EventSource : IEventSource
	{
		private readonly IServiceProvider Locator;
		private readonly ConcurrentDictionary<Type, object> EventSources = new ConcurrentDictionary<Type, object>(1, 17);

		public EventSource(IServiceProvider locator)
		{
			Contract.Requires(locator != null);

			this.Locator = locator;
		}

		public IObservable<TEvent> Track<TEvent>() where TEvent : IEvent
		{
			object observable;
			if (!EventSources.TryGetValue(typeof(TEvent), out observable))
			{
				IEventSource<TEvent> domainEventSource;
				try
				{
					domainEventSource = Locator.Resolve<IEventSource<TEvent>>();
				}
				catch (Exception ex)
				{
					throw new ArgumentException(string.Format(@"Can't find domain event source for {0}.
Is {0} a domain event and does it have registered source", typeof(TEvent).FullName), ex);
				}
				observable = domainEventSource.Events;
				EventSources.TryAdd(typeof(TEvent), observable);
			}
			return (IObservable<TEvent>)observable;
		}
	}
}
