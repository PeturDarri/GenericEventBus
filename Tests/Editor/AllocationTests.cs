using GenericEventBus.Tests;
using NUnit.Framework;

namespace GenericEventBus.Editor.Tests
{
	[TestFixture]
	public class AllocationTests
	{
		[Test]
		public void RaisingEvent_DoesNotAllocate()
		{
			var bus = new TestEventBus();
			bus.TestListen<StructTestEvent>();
			bus.TestListen<StructTestEvent>();
			bus.TestListen<StructTestEvent>();
			
			EventAssert.AssertDoesNotAllocate(() =>
			{
				bus.Raise(new StructTestEvent());
			});
		}
	}
}