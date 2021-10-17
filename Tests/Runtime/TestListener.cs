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
		
		private readonly GenericEventBus<ITestEvent> _bus;

		private EventReceivedHandler _callback;

		public TestListener(GenericEventBus<ITestEvent> bus)
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
	
	public class TestListener<TEvent, TObject> : TestListener where TEvent : ITestEvent
	{
		public new delegate void EventReceivedHandler(TestListener<TEvent, TObject> listener);
		
		private readonly GenericEventBus<ITestEvent, TObject> _bus;

		private EventReceivedHandler _callback;

		public TestListener(GenericEventBus<ITestEvent, TObject> bus)
		{
			_bus = bus;
		}

		public TEvent LastReceivedEvent { get; private set; }
		public TObject LastReceivedTarget { get; private set; }
		public TObject LastReceivedSource { get; private set; }
		
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
		
		public void SubscribeTarget(TObject target, float priority = 0, EventReceivedHandler callback = null)
		{
			_bus.SubscribeToTarget<TEvent>(target, OnEvent, priority);

			_callback = callback;
		}

		public void SubscribeTarget(TObject target, EventReceivedHandler callback)
		{
			_bus.SubscribeToTarget<TEvent>(target, OnEvent);

			_callback = callback;
		}

		public void SubscribeTarget(TObject target, Action callback, float priority = 0)
		{
			_bus.SubscribeToTarget<TEvent>(target, OnEvent, priority);

			_callback = _ => callback();
		}

		public void UnsubscribeTarget(TObject target)
		{
			_bus.UnsubscribeFromTarget<TEvent>(target, OnEvent);
			DidReceiveEvent = false;
		}
		
		public void SubscribeSource(TObject source, float priority = 0, EventReceivedHandler callback = null)
		{
			_bus.SubscribeToSource<TEvent>(source, OnEvent, priority);

			_callback = callback;
		}

		public void SubscribeSource(TObject source, EventReceivedHandler callback)
		{
			_bus.SubscribeToSource<TEvent>(source, OnEvent);

			_callback = callback;
		}

		public void SubscribeSource(TObject source, Action callback, float priority = 0)
		{
			_bus.SubscribeToSource<TEvent>(source, OnEvent, priority);

			_callback = _ => callback();
		}

		public void UnsubscribeSource(TObject source)
		{
			_bus.UnsubscribeFromSource<TEvent>(source, OnEvent);
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

		public void AssertReceivedFrom(TObject target, TObject source)
		{
			Assert.IsTrue(DidReceiveEvent);
			Assert.AreEqual(target, LastReceivedTarget);
			Assert.AreEqual(source, LastReceivedSource);

			DidReceiveEvent = false;
		}

		private void OnEvent(ref TEvent eventData, TObject target, TObject source)
		{
			DidReceiveEvent = true;
			LastReceivedEvent = eventData;
			LastReceivedTarget = target;
			LastReceivedSource = source;
			
			EventReceivedEvent?.Invoke(this);
			InvokeEventReceivedEvent();
			
			_callback?.Invoke(this);
			_callback = null;
		}
	}
}