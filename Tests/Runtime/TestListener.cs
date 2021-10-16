using System;
using NUnit.Framework;

namespace GenericEventBus.Tests
{
	public class TestListener<TEvent> where TEvent : ITestEvent
	{
		public delegate void EventReceivedHandler(TestListener<TEvent> listener, TEvent eventData);
		
		private readonly TestEventBus _bus;

		private EventReceivedHandler _callback;

		public TestListener(TestEventBus bus)
		{
			_bus = bus;
		}

		public bool DidReceiveEvent { get; private set; }
		public TEvent LastReceivedEvent { get; private set; }

		public event EventReceivedHandler EventReceivedEvent;

		public void Subscribe(float priority = 0, EventReceivedHandler callback = null)
		{
			_bus.SubscribeTo<TEvent>(OnEvent, priority);

			_callback = callback;
		}

		public void Subscribe(EventReceivedHandler callback)
		{
			_bus.SubscribeTo<TEvent>(OnEvent);

			_callback = callback;
		}

		public void Subscribe(Action callback)
		{
			_bus.SubscribeTo<TEvent>(OnEvent);

			_callback = (_, __) => callback();
		}

		public void Unsubscribe()
		{
			_bus.UnsubscribeFrom<TEvent>(OnEvent);
			DidReceiveEvent = false;
		}

		public void AssertDidReceiveAndReset()
		{
			Assert.IsTrue(DidReceiveEvent);
			DidReceiveEvent = false;
		}

		public void AssertDidNotReceive()
		{
			Assert.IsFalse(DidReceiveEvent);
		}

		private void OnEvent(ref TEvent eventData)
		{
			DidReceiveEvent = true;
			LastReceivedEvent = eventData;
			
			EventReceivedEvent?.Invoke(this, eventData);
			_callback?.Invoke(this, eventData);
			_callback = null;
		}
	}
}