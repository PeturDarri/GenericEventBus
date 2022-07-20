using GenericEventBus.Tests;
using NUnit.Framework;

namespace GenericEventBus.Editor.Tests
{
	[TestFixture]
	public class BasicEventBusTests
	{
		[Test]
		public void SubscribeRaiseUnsubscribeRaise_Works()
		{
			using (var bus = new TestEventBus())
			{
				var listener = bus.TestListener<StructTestEvent>();

				listener.Subscribe();
				listener.AssertDidNotReceive();
				bus.Raise(new StructTestEvent());
				listener.AssertDidReceiveAndReset();

				listener.Unsubscribe();
				bus.Raise(new StructTestEvent());
				listener.AssertDidNotReceive();
			}
		}
	}
}