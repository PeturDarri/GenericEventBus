using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GenericEventBus.Helpers;
using UnityEngine;

namespace GenericEventBus
{
	/// <summary>
	/// <para>An event bus.</para>
	/// If you want to be able to raise events that are targeted to specific objects and that can have source objects, use <see cref="GenericEventBus{TEvent, TObject}"/> instead.
	/// </summary>
	/// <typeparam name="TBaseEvent"><para>The base type all events must inherit/implement.</para> If you don't want to restrict event types to a base type, use <see cref="object"/> as the base type.</typeparam>
	public class GenericEventBus<TBaseEvent>
	{
		static GenericEventBus()
		{
			// Initialize some things to avoid allocation on first use.
			var comparer = EqualityComparer<uint>.Default;
		}

		private readonly List<uint> _raiseRecursionsConsumed = new List<uint>();
		
		protected readonly Queue<QueuedEvent> QueuedEvents = new Queue<QueuedEvent>(32);

		private uint _currentRaiseRecursionDepth;

		/// <summary>
		/// Has the current raised event been consumed?
		/// </summary>
		public bool CurrentEventIsConsumed =>
			_currentRaiseRecursionDepth > 0 && _raiseRecursionsConsumed.Contains(_currentRaiseRecursionDepth);

		/// <summary>
		/// Is an event currently being raised?
		/// </summary>
		public bool IsEventBeingRaised => _currentRaiseRecursionDepth > 0;

		/// <summary>
		/// A delegate for the callback methods given when subscribing to an event type.
		/// </summary>
		/// <param name="eventData">The event that was raised.</param>
		/// <typeparam name="TEvent">The type of event this callback handles.</typeparam>
		public delegate void EventHandler<TEvent>(ref TEvent eventData) where TEvent : TBaseEvent;

		/// <summary>
		/// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
		/// </summary>
		/// <param name="event">The event to raise.</param>
		/// <typeparam name="TEvent">The type of event to raise.</typeparam>
		/// <returns>Returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
		public bool RaiseImmediately<TEvent>(TEvent @event) where TEvent : TBaseEvent
		{
			return RaiseImmediately(ref @event);
		}

		/// <summary>
		/// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
		/// </summary>
		/// <param name="event">The event to raise.</param>
		/// <typeparam name="TEvent">The type of event to raise.</typeparam>
		/// <returns>Returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
		public virtual bool RaiseImmediately<TEvent>(ref TEvent @event) where TEvent : TBaseEvent
		{
			var wasConsumed = false;
			
			OnBeforeRaiseEvent();

			try
			{
				var listeners = EventListeners<TEvent>.GetListeners(this);

				foreach (var listener in listeners)
				{
					try
					{
						listener?.Invoke(ref @event);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}

					if (CurrentEventIsConsumed)
					{
						wasConsumed = true;
						break;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			finally
			{
				OnAfterRaiseEvent();
			}
			
			return wasConsumed;
		}

		/// <summary>
		/// <para>Raises the given event. If there are other events currently being raised, this event will be raised after those events finish.</para>
		/// </summary>
		/// <param name="event">The event to raise.</param>
		/// <typeparam name="TEvent">The type of event to raise.</typeparam>
		/// <returns>If the event was raised immediately, returns true if the event was consumed with <see cref="ConsumeCurrentEvent"/>.</returns>
		public virtual bool Raise<TEvent>(in TEvent @event) where TEvent : TBaseEvent
		{
			if (!IsEventBeingRaised)
			{
				return RaiseImmediately(@event);
			}

			var listeners = EventListeners<TEvent>.GetListeners(this);
			listeners.EnqueueEvent(in @event);

			return false;
		}

		/// <summary>
		/// Subscribe to a given event type.
		/// </summary>
		/// <param name="handler">The method that should be invoked when the event is raised.</param>
		/// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
		///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
		/// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
		public virtual void SubscribeTo<TEvent>(EventHandler<TEvent> handler, float priority = 0)
			where TEvent : TBaseEvent
		{
			var listeners = EventListeners<TEvent>.GetListeners(this);
			listeners.AddListener(handler, priority);
		}

		/// <summary>
		/// Unsubscribe from a given event type.
		/// </summary>
		/// <param name="handler">The method that was previously given in SubscribeTo.</param>
		/// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
		public virtual void UnsubscribeFrom<TEvent>(EventHandler<TEvent> handler) where TEvent : TBaseEvent
		{
			var listeners = EventListeners<TEvent>.GetListeners(this);
			listeners.RemoveListener(handler);
		}

		/// <summary>
		/// Consumes the current event being raised, which stops the propagation to other listeners.
		/// </summary>
		public void ConsumeCurrentEvent()
		{
			if (_currentRaiseRecursionDepth == 0) return;
			
			if (!_raiseRecursionsConsumed.Contains(_currentRaiseRecursionDepth))
			{
				_raiseRecursionsConsumed.Add(_currentRaiseRecursionDepth);
			}
		}

		/// <summary>
		/// Removes all the listeners of the given event type.
		/// </summary>
		/// <typeparam name="TEvent">The event type.</typeparam>
		/// <exception cref="InvalidOperationException">Thrown if an event is currently being raised.</exception>
		public void ClearListeners<TEvent>() where TEvent : TBaseEvent
		{
			if (IsEventBeingRaised)
			{
				throw new InvalidOperationException("Not allowed to clear listeners while an event is being raised.");
			}
			
			ClearAllListeners<TEvent>();
		}

		protected virtual void ClearAllListeners<TEvent>() where TEvent : TBaseEvent
		{
			var listeners = EventListeners<TEvent>.GetListeners(this);
			listeners.Clear();
		}

		protected void OnBeforeRaiseEvent()
		{
			_currentRaiseRecursionDepth++;
		}

		protected void OnAfterRaiseEvent()
		{
			_raiseRecursionsConsumed.Remove(_currentRaiseRecursionDepth--);

			if (_currentRaiseRecursionDepth == 0)
			{
				_raiseRecursionsConsumed.Clear();

				while (QueuedEvents.Count > 0)
				{
					var queuedEvent = QueuedEvents.Dequeue();
					queuedEvent.Raise(this);
				}
			}
		}

		protected abstract class QueuedEvent
		{
			public abstract void Raise(GenericEventBus<TBaseEvent> eventBus);
		}
		
		private sealed class EventListeners<TEvent> : IEnumerable<EventHandler<TEvent>> where TEvent : TBaseEvent
		{
			private static readonly ConditionalWeakTable<GenericEventBus<TBaseEvent>, EventListeners<TEvent>>
				Listeners = new ConditionalWeakTable<GenericEventBus<TBaseEvent>, EventListeners<TEvent>>();

			private static readonly ObjectPool<Enumerator> EnumeratorPool = new ObjectPool<Enumerator>();

			private static readonly ObjectPool<DerivedQueuedEvent> QueuedEventPool =
				new ObjectPool<DerivedQueuedEvent>();

			private static readonly
				ConditionalWeakTable<GenericEventBus<TBaseEvent>, EventListeners<TEvent>>.CreateValueCallback
				CreateListeners = key => new EventListeners<TEvent>(key);

			static EventListeners()
			{
				// Initialize some things that would normally initialize with the first Raise, causing allocation.
				var enumeratorComparer = EqualityComparer<Enumerator>.Default;
			}

			public static EventListeners<TEvent> GetListeners(GenericEventBus<TBaseEvent> eventBus)
			{
				return Listeners.GetValue(eventBus, CreateListeners);
			}

			private readonly GenericEventBus<TBaseEvent> _eventBus;
			private readonly List<Listener> _sortedListeners = new List<Listener>();
			private readonly List<Enumerator> _activeEnumerators = new List<Enumerator>(4);

			private EventListeners(GenericEventBus<TBaseEvent> eventBus)
			{
				_eventBus = eventBus;
			}

			public void AddListener(EventHandler<TEvent> handler, float priority)
			{
				var listener = new Listener(handler, priority);

				var index = _sortedListeners.InsertIntoSortedList(listener);

				foreach (var enumerator in _activeEnumerators)
				{
					if (enumerator.Index > index)
					{
						enumerator.Index++;
					}
				}
			}
			
			public void RemoveListener(EventHandler<TEvent> handler)
			{
				for (var i = _sortedListeners.Count - 1; i >= 0; i--)
				{
					if (!Equals(_sortedListeners[i].Handler, handler)) continue;

					_sortedListeners.RemoveAt(i);

					foreach (var enumerator in _activeEnumerators)
					{
						if (enumerator.Index >= i && enumerator.Index > 0)
						{
							enumerator.Index--;
						}
					}
				}
			}

			public void EnqueueEvent(in TEvent @event)
			{
				var queuedEvent = QueuedEventPool.Get();
				queuedEvent.EventData = @event;
				_eventBus.QueuedEvents.Enqueue(queuedEvent);
			}

			private class DerivedQueuedEvent : QueuedEvent
			{
				public TEvent EventData;
				
				public override void Raise(GenericEventBus<TBaseEvent> eventBus)
				{
					eventBus.Raise(EventData);
					
					EventData = default;
					QueuedEventPool.Release(this);
				}
			}
			
			private readonly struct Listener : IEquatable<Listener>, IComparable<Listener>
			{
				public readonly EventHandler<TEvent> Handler;
				public readonly float Priority;

				public Listener(EventHandler<TEvent> handler, float priority)
				{
					Handler = handler;
					Priority = priority;
				}

				public bool Equals(Listener other)
				{
					return Handler.Equals(other.Handler);
				}

				public override bool Equals(object obj)
				{
					return obj is Listener other && Equals(other);
				}

				public override int GetHashCode()
				{
					return Handler.GetHashCode();
				}

				public int CompareTo(Listener other)
				{
					return other.Priority.CompareTo(Priority);
				}
			}

			public IEnumerator<EventHandler<TEvent>> GetEnumerator()
			{
				var enumerator = EnumeratorPool.Get();
				enumerator.Index = 0;
				enumerator.Owner = this;

				_activeEnumerators.Add(enumerator);

				return enumerator;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			private class Enumerator : IEnumerator<EventHandler<TEvent>>
			{
				public EventListeners<TEvent> Owner;
				public int Index;

				public EventHandler<TEvent> Current { get; private set; }
				object IEnumerator.Current => Current;

				public bool MoveNext()
				{
					if (Index >= Owner._sortedListeners.Count) return false;

					Current = Owner._sortedListeners[Index++].Handler;
					return true;
				}

				public void Dispose()
				{
					EnumeratorPool.Release(this);
					Owner._activeEnumerators.Remove(this);
				}

				public void Reset()
				{
					throw new NotImplementedException();
				}
			}

			public void Clear()
			{
				_sortedListeners.Clear();
			}
		}
	}
}