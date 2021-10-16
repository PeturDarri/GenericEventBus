using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace GenericEventBus.Tests
{
	public static class EventAssert
	{
		public static void AssertDoesNotAllocate(Action action)
		{
			Assert.That(() => action(), Is.Not.AllocatingGCMemory());
		}
		
		public static void AssertListenersInvokedInOrder<TEvent>(this TestEventBus bus, TEvent eventData,
			params TestListener<TEvent>[] listeners) where TEvent : ITestEvent
		{
			AssertListenersInvokedInOrder(bus, eventData, listeners, listeners);
		}

		public static void AssertListenersInvokedInOrder<TEvent>(this TestEventBus bus, TEvent eventData,
			TestListener<TEvent>[] listeners, TestListener<TEvent>[] expectedOrder) where TEvent : ITestEvent
		{
			var eventReceivedList = new List<TestListener<TEvent>>(listeners.Length);

			foreach (var listener in listeners)
			{
				listener.EventReceivedEvent += OnListenerReceivedEvent;
			}
			
			bus.Raise(eventData);

			Assert.AreEqual(expectedOrder.Length, eventReceivedList.Count);

			CollectionAssert.AreEqual(expectedOrder, eventReceivedList, "Listeners were not invoked in the expected order.");

			void OnListenerReceivedEvent(TestListener<TEvent> listener, TEvent @event)
			{
				listener.EventReceivedEvent -= OnListenerReceivedEvent;
				
				eventReceivedList.Add(listener);
			}
		}
	}
}