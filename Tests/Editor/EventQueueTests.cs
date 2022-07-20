using GenericEventBus.Tests;
using NUnit.Framework;

namespace GenericEventBus.Editor.Tests
{
	[TestFixture]
	public class EventQueueTests
	{
		[Test]
		public void EventQueue_Works()
		{
			using (var bus = new TestEventBus())
			{
				var listener1 = bus.TestListen<StructTestEvent>(() => bus.Raise(new ClassTestEvent()));
				var listener2 = bus.TestListen<StructTestEvent>();
				var listener3 = bus.TestListen<ClassTestEvent>();

				bus.AssertListenersInvokedInOrder(new StructTestEvent(), listener1, listener2, listener3);
			}
		}

		[Test]
		public void RaiseImmediately_Works()
		{
			using (var bus = new TestEventBus())
			{
				var listener1 = bus.TestListen<StructTestEvent>(() => bus.RaiseImmediately(new ClassTestEvent()));
				var listener2 = bus.TestListen<StructTestEvent>();
				var listener3 = bus.TestListen<ClassTestEvent>();

				bus.AssertListenersInvokedInOrder(new StructTestEvent(), listener1, listener3, listener2);
			}
		}
	}
}