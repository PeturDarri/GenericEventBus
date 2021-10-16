using System;
using NUnit.Framework;

namespace GenericEventBus.Tests
{
	public abstract class TestListener
	{
		public delegate void EventReceivedHandler(TestListener listener);
		
		public bool DidReceiveEvent { get; protected set; }
		public event EventReceivedHandler EventReceivedEvent;

		protected void InvokeEventReceivedEvent()
		{
			EventReceivedEvent?.Invoke(this);
		}
	}
	
	public class TestListener<TEvent> : TestListener where TEvent : ITestEvent
	{
		public new delegate void EventReceivedHandler(TestListener<TEvent> listener);
		
		private readonly TestEventBus _bus;

		private EventReceivedHandler _callback;

		public TestListener(TestEventBus bus)
		{
			_bus = bus;
		}

		public TEvent LastReceivedEvent { get; private set; }
		public new event EventReceivedHandler EventReceivedEvent;

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

		public void Subscribe(Action callback, float priority = 0)
		{
			_bus.SubscribeTo<TEvent>(OnEvent, priority);

			_callback = _ => callback();
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
			
			EventReceivedEvent?.Invoke(this);
			InvokeEventReceivedEvent();
			
			_callback?.Invoke(this);
			_callback = null;
		}
	}
}