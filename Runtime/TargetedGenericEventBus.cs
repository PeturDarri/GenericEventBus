namespace GenericEventBus
{
	/// <summary>
	/// An event bus that has the concept of a target and a source object for every raised event.
	/// </summary>
	/// <inheritdoc/>
	/// <typeparam name="TObject">The type for the target and source objects, e.g. <see cref="UnityEngine.GameObject"/>.</typeparam>
	internal class GenericEventBus<TBaseEvent, TObject> : GenericEventBus<TBaseEvent>
	{
		public delegate void TargetedEventHandler<TEvent>(ref TEvent eventData, TObject target, TObject source);

		/// <summary>
		/// <para>The default object value used if <c>target</c> and/or <c>source</c> are omitted in a raised event.</para>
		/// <para>If a listener subscribes to an event to or from an object that equals this, the listener will receive all the events, regardless of the target or source.</para>
		/// </summary>
		public TObject DefaultObject { get; set; } = default;

		public override void Raise<TEvent>(ref TEvent @event)
		{
			Raise(ref @event, DefaultObject, DefaultObject);
		}

		public void Raise<TEvent>(ref TEvent @event, TObject target, TObject source)
		{
		}
	}
}