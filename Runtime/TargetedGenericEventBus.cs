using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GenericEventBus.Helpers;
using UnityEngine;

namespace GenericEventBus
{
	/// <summary>
	/// An event bus that has the concept of a target and a source object for every raised event.
	/// </summary>
	/// <inheritdoc/>
	/// <typeparam name="TObject">The type for the target and source objects, e.g. <see cref="UnityEngine.GameObject"/>.</typeparam>
	public class GenericEventBus<TBaseEvent, TObject> : GenericEventBus<TBaseEvent>
	{
		public delegate void TargetedEventHandler<TEvent>(ref TEvent eventData, TObject target, TObject source);

		private static readonly EqualityComparer<TObject> ObjectComparer = EqualityComparer<TObject>.Default;

		/// <summary>
		/// <para>The default object value used if <c>target</c> and/or <c>source</c> are omitted in a raised event.</para>
		/// <para>If a listener subscribes to an event to or from an object that equals this, the listener will receive all the events, regardless of the target or source.</para>
		/// </summary>
		public ref TObject DefaultObject => ref _defaultObject;

		private TObject _defaultObject;

		private bool IsDefaultObject(TObject obj)
		{
			return ObjectComparer.Equals(obj, _defaultObject);
		}

		public override bool Raise<TEvent>(in TEvent @event)
		{
			return Raise(@event, DefaultObject, DefaultObject);
		}

		/// <summary>
		/// <para>Raises the given event. If there are other events currently being raised, this event will be raised after those events finish.</para>
		/// </summary>
		/// <param name="event">The event to raise.</param>
		/// <param name="target">The target object for this event.</param>
		/// <param name="source">The source object for this event.</param>
		/// <typeparam name="TEvent">The type of event to raise.</typeparam>
		/// <returns>If the event was raised immediately, returns true if the event was consumed with <see cref="GenericEventBus{TBaseEvent}.ConsumeCurrentEvent"/>.</returns>
		public bool Raise<TEvent>(TEvent @event, TObject target, TObject source) where TEvent : TBaseEvent
		{
			if (!IsEventBeingRaised)
			{
				return RaiseImmediately(ref @event, target, source);
			}

			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.EnqueueEvent(in @event, target, source);

			return false;
		}

		public override bool RaiseImmediately<TEvent>(ref TEvent @event)
		{
			return RaiseImmediately(ref @event, DefaultObject, DefaultObject);
		}

		/// <summary>
		/// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
		/// </summary>
		/// <param name="event">The event to raise.</param>
		/// <param name="target">The target object for this event.</param>
		/// <param name="source">The source object for this event.</param>
		/// <typeparam name="TEvent">The type of event to raise.</typeparam>
		/// <returns>Returns true if the event was consumed with <see cref="GenericEventBus{TBaseEvent}.ConsumeCurrentEvent"/>.</returns>
		public bool RaiseImmediately<TEvent>(TEvent @event, TObject target, TObject source) where TEvent : TBaseEvent
		{
			return RaiseImmediately(ref @event, target, source);
		}

		/// <summary>
		/// <para>Raises the given event immediately, regardless if another event is currently still being raised.</para>
		/// </summary>
		/// <param name="event">The event to raise.</param>
		/// <param name="target">The target object for this event.</param>
		/// <param name="source">The source object for this event.</param>
		/// <typeparam name="TEvent">The type of event to raise.</typeparam>
		/// <returns>Returns true if the event was consumed with <see cref="GenericEventBus{TBaseEvent}.ConsumeCurrentEvent"/>.</returns>
		public bool RaiseImmediately<TEvent>(ref TEvent @event, TObject target, TObject source)
			where TEvent : TBaseEvent
		{
			var wasConsumed = false;
			
			OnBeforeRaiseEvent();

			try
			{
				var listeners = TargetedEventListeners<TEvent>.Get(this);

				foreach (var listener in listeners.GetListeners(target, source))
				{
					try
					{
						listener?.Invoke(ref @event, target, source);
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

		public override void SubscribeTo<TEvent>(EventHandler<TEvent> handler, float priority = 0)
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.AddListener(handler, priority);
		}

		/// <summary>
		/// Subscribe to a given event type.
		/// </summary>
		/// <param name="handler">The method that should be invoked when the event is raised.</param>
		/// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
		///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
		/// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
		public void SubscribeTo<TEvent>(TargetedEventHandler<TEvent> handler, float priority = 0)
			where TEvent : TBaseEvent
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.AddListener(handler, priority);
		}

		public override void UnsubscribeFrom<TEvent>(EventHandler<TEvent> handler)
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.RemoveListener(handler);
		}

		/// <summary>
		/// Unsubscribe from a given event type.
		/// </summary>
		/// <param name="handler">The method that was previously given in SubscribeTo.</param>
		/// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
		public void UnsubscribeFrom<TEvent>(TargetedEventHandler<TEvent> handler) where TEvent : TBaseEvent
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.RemoveListener(handler);
		}

		/// <summary>
		/// Subscribe to a given event type, but only if it targets the given object.
		/// </summary>
		/// <param name="target">The target object.</param>
		/// <param name="handler">The method that should be invoked when the event is raised.</param>
		/// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
		///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
		/// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
		public void SubscribeToTarget<TEvent>(TObject target, TargetedEventHandler<TEvent> handler, float priority = 0)
			where TEvent : TBaseEvent
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.AddTargetListener(target, handler, priority);
		}

		/// <summary>
		/// Unsubscribe from a given event type, but only if it targets the given object.
		/// </summary>
		/// <param name="target">The target object.</param>
		/// <param name="handler">The method that was previously given in SubscribeToTarget.</param>
		/// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
		public void UnsubscribeFromTarget<TEvent>(TObject target, TargetedEventHandler<TEvent> handler)
			where TEvent : TBaseEvent
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.RemoveTargetListener(target, handler);
		}

		/// <summary>
		/// Subscribe to a given event type, but only if it comes from the given object.
		/// </summary>
		/// <param name="source">The source object.</param>
		/// <param name="handler">The method that should be invoked when the event is raised.</param>
		/// <param name="priority">Higher priority means this listener will receive the event earlier than other listeners with lower priority.
		///                        If multiple listeners have the same priority, they will be invoked in the order they subscribed.</param>
		/// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
		public void SubscribeToSource<TEvent>(TObject source, TargetedEventHandler<TEvent> handler, float priority = 0)
			where TEvent : TBaseEvent
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.AddSourceListener(source, handler, priority);
		}

		/// <summary>
		/// Unsubscribe from a given event type, but only if it comes from the given object.
		/// </summary>
		/// <param name="source">The source object.</param>
		/// <param name="handler">The method that was previously given in SubscribeToSource.</param>
		/// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
		public void UnsubscribeFromSource<TEvent>(TObject source, TargetedEventHandler<TEvent> handler)
			where TEvent : TBaseEvent
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.RemoveSourceListener(source, handler);
		}

		protected override void ClearAllListeners<TEvent>()
		{
			var listeners = TargetedEventListeners<TEvent>.Get(this);
			listeners.Clear();
		}

		private class TargetedEventListeners<TEvent> where TEvent : TBaseEvent
		{
			private static readonly ConditionalWeakTable<GenericEventBus<TBaseEvent, TObject>,
				TargetedEventListeners<TEvent>> Listeners =
				new ConditionalWeakTable<GenericEventBus<TBaseEvent, TObject>, TargetedEventListeners<TEvent>>();

			private static readonly ConditionalWeakTable<EventHandler<TEvent>, TargetedEventHandler<TEvent>>
				ConvertedEventHandlers = new ConditionalWeakTable<EventHandler<TEvent>, TargetedEventHandler<TEvent>>();

			private static readonly ObjectPool<Enumerator> EnumeratorPool = new ObjectPool<Enumerator>();

			private static readonly ObjectPool<DerivedQueuedEvent> QueuedEventPool =
				new ObjectPool<DerivedQueuedEvent>();

			private static readonly ObjectPool<List<Listener>> ListenerListPool = new ObjectPool<List<Listener>>();

			private static readonly
				ConditionalWeakTable<GenericEventBus<TBaseEvent, TObject>, TargetedEventListeners<TEvent>>.
				CreateValueCallback CreateListeners = key => new TargetedEventListeners<TEvent>(key);

			private static readonly ConditionalWeakTable<EventHandler<TEvent>, TargetedEventHandler<TEvent>>.
				CreateValueCallback CreateConvertedEventHandler = key =>
					(ref TEvent @event, TObject target, TObject source) => key(ref @event);

			static TargetedEventListeners()
			{
				// Initialize some things that would normally initialize with the first Raise, causing allocation.
				var enumeratorComparer = EqualityComparer<Enumerator>.Default;
			}

			public static TargetedEventListeners<TEvent> Get(GenericEventBus<TBaseEvent, TObject> eventBus)
			{
				return Listeners.GetValue(eventBus, CreateListeners);
			}

			private readonly GenericEventBus<TBaseEvent, TObject> _eventBus;
			private readonly List<Listener> _sortedListeners = new List<Listener>();

			private readonly Dictionary<TObject, List<Listener>> _targetListeners =
				new Dictionary<TObject, List<Listener>>();

			private readonly Dictionary<TObject, List<Listener>> _sourceListeners =
				new Dictionary<TObject, List<Listener>>();

			private readonly List<Enumerator> _activeEnumerators = new List<Enumerator>(4);

			private TargetedEventListeners(GenericEventBus<TBaseEvent, TObject> eventBus)
			{
				_eventBus = eventBus;
			}

			public void AddListener(TargetedEventHandler<TEvent> handler, float priority)
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

			public void RemoveListener(TargetedEventHandler<TEvent> handler)
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

			public void AddListener(EventHandler<TEvent> handler, float priority)
			{
				var converted = ConvertedEventHandlers.GetValue(handler, CreateConvertedEventHandler);
				AddListener(converted, priority);
			}

			public void RemoveListener(EventHandler<TEvent> handler)
			{
				if (ConvertedEventHandlers.TryGetValue(handler, out var targetedHandler))
				{
					RemoveListener(targetedHandler);
				}
			}

			public void AddTargetListener(TObject target, TargetedEventHandler<TEvent> handler, float priority)
			{
				if (_eventBus.IsDefaultObject(target)) return;

				if (!_targetListeners.TryGetValue(target, out var listeners))
				{
					listeners = ListenerListPool.Get();

					_targetListeners[target] = listeners;
				}

				var listener = new Listener(handler, priority);

				var index = listeners.InsertIntoSortedList(listener);

				foreach (var enumerator in _activeEnumerators)
				{
					if (!ObjectComparer.Equals(target, enumerator.Target)) continue;

					enumerator.TargetListeners = listeners;

					if (enumerator.TargetIndex > index)
					{
						enumerator.TargetIndex++;
					}
				}
			}

			public void RemoveTargetListener(TObject target, TargetedEventHandler<TEvent> handler)
			{
				if (!_targetListeners.TryGetValue(target, out var listeners)) return;

				for (var i = listeners.Count - 1; i >= 0; i--)
				{
					if (!Equals(listeners[i].Handler, handler)) continue;

					listeners.RemoveAt(i);

					foreach (var enumerator in _activeEnumerators)
					{
						if (!ObjectComparer.Equals(target, enumerator.Target)) continue;

						if (enumerator.TargetIndex >= i && enumerator.TargetIndex > 0)
						{
							enumerator.TargetIndex--;
						}

						if (listeners.Count == 0)
						{
							enumerator.TargetListeners = null;
						}
					}
				}

				if (listeners.Count == 0)
				{
					ListenerListPool.Release(listeners);
					_targetListeners.Remove(target);
				}
			}

			public void AddSourceListener(TObject source, TargetedEventHandler<TEvent> handler, float priority)
			{
				if (_eventBus.IsDefaultObject(source)) return;

				if (!_sourceListeners.TryGetValue(source, out var listeners))
				{
					listeners = ListenerListPool.Get();

					_sourceListeners[source] = listeners;
				}

				var listener = new Listener(handler, priority);

				var index = listeners.InsertIntoSortedList(listener);

				foreach (var enumerator in _activeEnumerators)
				{
					if (!ObjectComparer.Equals(source, enumerator.Source)) continue;

					enumerator.SourceListeners = listeners;

					if (enumerator.SourceIndex > index)
					{
						enumerator.SourceIndex++;
					}
				}
			}

			public void RemoveSourceListener(TObject source, TargetedEventHandler<TEvent> handler)
			{
				if (!_sourceListeners.TryGetValue(source, out var listeners)) return;

				for (var i = listeners.Count - 1; i >= 0; i--)
				{
					if (!Equals(listeners[i].Handler, handler)) continue;

					listeners.RemoveAt(i);

					foreach (var enumerator in _activeEnumerators)
					{
						if (!ObjectComparer.Equals(source, enumerator.Source)) continue;

						if (enumerator.SourceIndex >= i && enumerator.SourceIndex > 0)
						{
							enumerator.SourceIndex--;
						}

						if (listeners.Count == 0)
						{
							enumerator.SourceListeners = null;
						}
					}
				}

				if (listeners.Count == 0)
				{
					ListenerListPool.Release(listeners);
					_sourceListeners.Remove(source);
				}
			}

			public void EnqueueEvent(in TEvent @event, TObject target, TObject source)
			{
				var queuedEvent = QueuedEventPool.Get();
				queuedEvent.EventData = @event;
				queuedEvent.Target = target;
				queuedEvent.Source = source;

				_eventBus.QueuedEvents.Enqueue(queuedEvent);
			}

			private class DerivedQueuedEvent : QueuedEvent
			{
				public TEvent EventData;
				public TObject Target;
				public TObject Source;

				public override void Raise(GenericEventBus<TBaseEvent> eventBus)
				{
					var bus = (GenericEventBus<TBaseEvent, TObject>)eventBus;

					bus.Raise(EventData, Target, Source);

					EventData = default;
					Target = default;
					Source = default;

					QueuedEventPool.Release(this);
				}
			}

			private readonly struct Listener : IEquatable<Listener>, IComparable<Listener>
			{
				public readonly TargetedEventHandler<TEvent> Handler;
				public readonly float Priority;

				public Listener(TargetedEventHandler<TEvent> handler, float priority)
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

			public IEnumerable<TargetedEventHandler<TEvent>> GetListeners(TObject target, TObject source)
			{
				var enumerator = EnumeratorPool.Get();
				enumerator.Owner = this;
				enumerator.SortedListeners = _sortedListeners;

				if (!_eventBus.IsDefaultObject(target))
				{
					enumerator.Target = target;

					if (_targetListeners.TryGetValue(target, out var targetListeners))
					{
						enumerator.TargetListeners = targetListeners;
					}
				}

				if (!_eventBus.IsDefaultObject(source))
				{
					enumerator.Source = source;

					if (_sourceListeners.TryGetValue(source, out var sourceListeners))
					{
						enumerator.SourceListeners = sourceListeners;
					}
				}

				_activeEnumerators.Add(enumerator);

				return enumerator;
			}

			private class Enumerator : IEnumerator<TargetedEventHandler<TEvent>>,
				IEnumerable<TargetedEventHandler<TEvent>>
			{
				public TargetedEventListeners<TEvent> Owner;
				public int Index;
				public int TargetIndex;
				public int SourceIndex;
				public List<Listener> SortedListeners;
				public List<Listener> TargetListeners;
				public List<Listener> SourceListeners;
				public TObject Target;
				public TObject Source;
				public float LastPriority = float.MaxValue;

				public TargetedEventHandler<TEvent> Current { get; private set; }

				public bool MoveNext()
				{
					Listener? nextListener = null;
					ref var index = ref Index;

					do
					{
						if (Index < SortedListeners.Count && SortedListeners.Count > 0)
						{
							nextListener = SortedListeners[Index];
						}

						var targetListenerCount = TargetListeners?.Count ?? 0;

						if (targetListenerCount > 0 && TargetIndex < targetListenerCount)
						{
							var targetListener = TargetListeners[TargetIndex];

							if (nextListener.HasValue)
							{
								if (targetListener.Priority > nextListener.Value.Priority)
								{
									nextListener = targetListener;
									index = ref TargetIndex;
								}
							}
							else
							{
								nextListener = targetListener;
								index = ref TargetIndex;
							}
						}

						var sourceListenerCount = SourceListeners?.Count ?? 0;

						if (sourceListenerCount > 0 && SourceIndex < sourceListenerCount)
						{
							var sourceListener = SourceListeners[SourceIndex];

							if (nextListener.HasValue)
							{
								if (sourceListener.Priority > nextListener.Value.Priority)
								{
									nextListener = sourceListener;
									index = ref SourceIndex;
								}
							}
							else
							{
								nextListener = sourceListener;
								index = ref SourceIndex;
							}
						}
					} while (nextListener.HasValue && nextListener.Value.Priority > LastPriority);

					if (nextListener.HasValue)
					{
						var value = nextListener.Value;
						Current = value.Handler;
						LastPriority = value.Priority;
						index++;

						return true;
					}

					return false;
				}

				public void Dispose()
				{
					Owner._activeEnumerators.Remove(this);
					Reset();
					EnumeratorPool.Release(this);
				}

				public void Reset()
				{
					Owner = null;
					Index = 0;
					TargetIndex = 0;
					SourceIndex = 0;
					SortedListeners = null;
					TargetListeners = null;
					SourceListeners = null;
					Target = default;
					Source = default;
					LastPriority = float.MaxValue;
				}

				object IEnumerator.Current => Current;

				public IEnumerator<TargetedEventHandler<TEvent>> GetEnumerator() => this;

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}

			public void Clear()
			{
				_sortedListeners.Clear();
				_targetListeners.Clear();
				_sourceListeners.Clear();
			}
		}
	}
}