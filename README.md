# Generic Event Bus
A synchronous event bus for Unity, using strictly typed events and generics to reduce runtime overhead.

## Features
* Events are defined as types, instead of as members in some class or as string IDs.
* Generics are used to move runtime overhead to compile time. There's no `Dictionary<Type, Listeners>`. 
* Listeners can include a `priority` number when subscribing to an event to control their order in the event execution, regardless of _when_ the listener subscribes.

## Usage
To create an event bus, use the `GenericEventBus<TBaseEvent>` type:
```c#
var eventBus = new GenericEventBus<TBaseEvent>();
```
`TBaseEvent` is the base type all event types must inherit/implement. You can use `System.Object` as the base type to allow any type to be used as an event, but I recommend defining an empty interface as the base type:
```c#
public interface IEvent {}
```
```c#
var eventBus = new GenericEventBus<IEvent>();
```
Otherwise, _any_ object can be raised as an event, which is weird and confusing.

---

To define new events, just define a type that inherits/implements your base event type:
```c#
public struct GameStartedEvent : IEvent
{
}
```
<details>
  <summary><em>Can events be defined as classes instead?</em></summary>

Events can be defined as either `class` or `struct`, but I recommend using structs to avoid allocation when creating new instances. Events are passed around in the event bus and to listeners by references  using `ref`, so you don't have to worry about the overhead of struct copying.

And you also don't need to worry about the struct being boxed. Generic type parameters ensure it is never boxed.
</details>

---

This event can now be raised:
```c#
eventBus.Raise(new GameStartedEvent());
```
Including data with events is very simple:
```c#
public struct GameStartedEvent : IEvent
{
    public int NumberOfPlayers;
}
```
```c#
eventBus.Raise(new GameStartedEvent { NumberOfPlayers = 1 });
```

---

Here's how you subscribe to and unsubscribe from events:
```c#
private void OnEnable()
{
    eventBus.SubscribeTo<GameStartedEvent>(OnGameStartedEvent);
}

private void OnDisable()
{
    eventBus.UnsubscribeFrom<GameStartedEvent>(OnGameStartedEvent);
}

private void OnGameStartedEvent(ref GameStartedEvent eventData)
{
    Debug.Log($"Game started with {eventData.NumberOfPlayers} player(s)");
}
```
You can also include a `float priority` argument when calling `SubscribeTo`. Subscribing to an event with a high priority means you'll receive the event before other listeners that have a lower priority. This is great for defining the order of listeners without having to worry about _when_ each listener subscribes to the event.
```c#
private void OnEnable()
{
    eventBus.SubscribeTo<GameStartedEvent>(OnGameStartedEvent);
    eventBus.SubscribeTo<GameStartedEvent>(OnGameStartedEventPriority, 10f);
}

private void OnDisable()
{
    eventBus.UnsubscribeFrom<GameStartedEvent>(OnGameStartedEvent);
    eventBus.UnsubscribeFrom<GameStartedEvent>(OnGameStartedEventPriority);
}

private void OnGameStartedEvent(ref GameStartedEvent eventData)
{
    Debug.Log($"Game started with {eventData.NumberOfPlayers} player(s)");
}

private void OnGameStartedEventPriority(ref GameStartedEvent eventData)
{
    Debug.Log("This will be invoked first, even though it was added last!");
}
```
The default `priority` is `0` and listeners with the same priority will be invoked in the order they were added.