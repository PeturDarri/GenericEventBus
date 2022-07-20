using GenericEventBus.Tests;
using NUnit.Framework;

namespace GenericEventBus.Editor.Tests
{
	[TestFixture]
	public class MultipleEventListenersTests
	{
		[Test]
		public void MultipleListeners_SubscribeOrder_Works()
		{
			using (var bus = new TestEventBus())
			{
				var listener1 = bus.TestListen<StructTestEvent>();
				var listener2 = bus.TestListen<StructTestEvent>();

				bus.AssertListenersInvokedInOrder(new StructTestEvent(), listener1, listener2);
			}
		}

		[Test]
		public void ListenerPriority_Works()
		{
			using (var bus = new TestEventBus())
			{
				var listener1 = bus.TestListen<StructTestEvent>(-10);
				var listener2 = bus.TestListen<StructTestEvent>(100);
				var listener3 = bus.TestListen<StructTestEvent>(-5);

				bus.AssertListenersInvokedInOrder(new StructTestEvent(), listener2, listener3, listener1);
			}
		}

		[Test]
		public void RemovingAndAddingListenersDuringRaise_Works()
		{
			using (var bus = new TestEventBus())
			{
				var listener1 = bus.TestListener<StructTestEvent>();
				var listener2 = bus.TestListener<StructTestEvent>();
				var listener3 = bus.TestListener<StructTestEvent>();
				var listener4 = bus.TestListener<StructTestEvent>();
				var listener5 = bus.TestListener<StructTestEvent>();
				var listener6 = bus.TestListener<StructTestEvent>();
				var listener7 = bus.TestListener<StructTestEvent>();

				listener1.Subscribe();

				listener2.Subscribe(() =>
					listener1.Unsubscribe()); // Unsubscribing previous listener is shouldn't affect this raise.

				listener3.Subscribe(() =>
					listener1.Subscribe(10)); // Adding a listener to the front shouldn't affect this raise.

				listener4.Subscribe(() =>
					listener6.Unsubscribe()); // Unsubscribing an upcoming listener should make it not receive the event.

				listener5.Subscribe(() => listener7.Subscribe()); // Adding a listener to the end should make it receive the event.

				listener6.Subscribe();

				bus.AssertListenersInvokedInOrder(new StructTestEvent(), new[]
				{
					listener1, listener2, listener3, listener4, listener5, listener6, listener7
				}, new[]
				{
					listener1, listener2, listener3, listener4, listener5, listener7
				});
			}
		}
	}
}