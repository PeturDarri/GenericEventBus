using GenericEventBus.Tests;
using NUnit.Framework;

namespace GenericEventBus.Editor.Tests
{
	[TestFixture]
	public class ConsumeEventTests
	{
		[Test]
		public void ConsumeEvent_Works()
		{
			using (var bus = new TestEventBus())
			{
				var listener1 = bus.TestListen<StructTestEvent>();
				var listener2 = bus.TestListen<StructTestEvent>(() => bus.ConsumeCurrentEvent());
				var listener3 = bus.TestListen<StructTestEvent>();
				var listener4 = bus.TestListen<StructTestEvent>();

				bus.AssertListenersInvokedInOrder(new StructTestEvent(),
					new[] { listener1, listener2, listener3, listener4 },
					new[] { listener1, listener2 });
			}
		}
	}
}