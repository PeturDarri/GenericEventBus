using System;

namespace GenericEventBus.Tests
{
	public static class TestEventBusExtensions
	{
		public static TestListener<TEvent> TestListener<TEvent>(this GenericEventBus<ITestEvent> bus) where TEvent : ITestEvent
		{
			return new TestListener<TEvent>(bus);
		}

		public static TestListener<TEvent> TestListen<TEvent>(this GenericEventBus<ITestEvent> bus, float priority = 0,
			TestListener<TEvent>.EventReceivedHandler callback = null) where TEvent : ITestEvent
		{
			var listener = bus.TestListener<TEvent>();
			listener.Subscribe(priority, callback);

			return listener;
		}

		public static TestListener<TEvent> TestListen<TEvent>(this GenericEventBus<ITestEvent> bus, Action callback,
			float priority = 0) where TEvent : ITestEvent
		{
			var listener = bus.TestListener<TEvent>();
			listener.Subscribe(callback, priority);

			return listener;
		}
	}
}