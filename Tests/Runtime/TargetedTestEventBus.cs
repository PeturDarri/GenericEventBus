using System;

namespace GenericEventBus.Tests
{
	public class TargetedTestEventBus : GenericEventBus<ITestEvent, object>
	{
		public TestListener<TEvent, object> TestListener<TEvent>() where TEvent : ITestEvent
		{
			return new TestListener<TEvent, object>(this);
		}

		public TestListener<TEvent, object> TestListen<TEvent>(float priority = 0,
			TestListener<TEvent, object>.EventReceivedHandler callback = null) where TEvent : ITestEvent
		{
			var listener = TestListener<TEvent>();
			listener.Subscribe(priority, callback);

			return listener;
		}

		public TestListener<TEvent, object> TestListen<TEvent>(Action callback,
			float priority = 0) where TEvent : ITestEvent
		{
			var listener = TestListener<TEvent>();
			listener.Subscribe(callback, priority);

			return listener;
		}
	}
}