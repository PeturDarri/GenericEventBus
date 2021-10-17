using GenericEventBus.Tests;
using NUnit.Framework;

namespace GenericEventBus.Editor.Tests
{
	[TestFixture]
	public class BasicTargetedEventBusTests
	{
		[Test]
		public void SubscribeRaiseUnsubscribeRaise_Works()
		{
			var bus = new TargetedTestEventBus();
			var targetListener = bus.TestListener<StructTestEvent>();
			var sourceListener = bus.TestListener<StructTestEvent>();

			var target = new object();
			var source = new object();
			
			targetListener.SubscribeTarget(target);
			sourceListener.SubscribeSource(source);

			bus.Raise(new StructTestEvent(), target, source);
			targetListener.AssertReceivedFrom(target, source);
			sourceListener.AssertReceivedFrom(target, source);

			targetListener.UnsubscribeTarget(target);
			sourceListener.UnsubscribeSource(source);
			bus.Raise(new StructTestEvent(), target, source);
			targetListener.AssertDidNotReceive();
			sourceListener.AssertDidNotReceive();
		}
	}
}