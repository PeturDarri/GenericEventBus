# Generic Event Bus
A synchronous event bus for Unity written in C#, using strictly typed events and generics to reduce runtime overhead.

## Features
* Events are defined as types, instead of as members in some class or as string IDs.
* Generics are used to move runtime overhead to compile time. _(There's no `Dictionary<Type, Listeners>`)_ 
* Listeners can include a [priority](#priority) number when subscribing to an event to control their order in the event execution, regardless of _when_ the listener subscribes.
* Built-in support for [targeting events](#targeted-events) to specific objects, with an optional source object that raised the event.
* Event data can be [modified by listeners](#modifying-event-data), or completely [consumed](#consuming-events) to stop it.
* Events can be queued if other events are currently being raised. 

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
### Priority
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

### Targeted events
Things get a lot more interesting when using targeted events. You can think of this more like a message bus, where objects can raise events that are meant to be heard by specific target object.

To use targeted events, you must include a second generic type parameter in `GenericEventBus` to specify what type of object can be a target, like `GameObject`:
```c#
var eventBus = new GenericEventBus<IEvent, GameObject>();
```

You get all the same methods in this event bus as in the other one, so you can still raise non-targeted events, but now you can include a target and source object with raised events:
```c#
eventBus.Raise(new DamagedEvent { Damage = 10f }, targetGameObject, sourceGameObject);
```
In this example, `DamagedEvent` is defined just like any other event:
```c#
public struct DamagedEvent : IEvent
{
    public float Damage;
}
```
---
To listen to this event, use the `SubscribeToTarget` method:
```c#
private float health = 100f;

private void OnEnable()
{
    eventBus.SubscribeToTarget<DamagedEvent>(gameObject, OnDamagedEvent);
}

private void OnDisable()
{
    eventBus.UnsubscribeFromTarget<DamagedEvent>gameObject, OnDamagedEvent);
}

private void OnDamagedEvent(ref DamagedEvent eventData, GameObject target, GameObject source)
{
    health -= eventData.Damage;
    
    Debug.Log($"{target} received {eventData.Damage} damage from {source}");
}
```
---
This pattern allows you to have objects communicate with each other in a very decoupled way. If no one is listening to the target object, the event is ignored.

Another benefit from this pattern is that now you have an event of when objects are damaged, which any script can listen to.

For example, if you wanted to have some UI showing damage numbers on anything the player damages, you could do that like this:
```c#
private void OnEnable()
{
    eventBus.SubscribeToSource<DamagedEvent>(playerObject, OnPlayerInflictedDamageEvent);
}

private void OnDisable()
{
    eventBus.UnsubscribeFromSource<DamagedEvent>(playerObject, OnPlayerInflictedDamageEvent);
}

private void OnPlayerInflictedDamageEvent(ref DamagedEvent eventData, GameObject target, GameObject source)
{
    SpawnDamageNumberOn(target, eventData.Damage);
}
```
---
And any listeners that don't specify a target or source will simply get all events, regardless of the target or source. Perfect for something like a kill feed UI:
```c#
public struct KilledEvent : IEvent
{
    public IWeapon Weapon;
}

private void OnEnable()
{
    eventBus.SubscribeTo<KilledEvent>(OnKilledEvent);
}

private void OnDisable()
{
    eventBus.UnsubscribeFrom<KilledEvent>(OnKilledEvent);
}

private void OnKilledEvent(ref KilledEvent eventData, GameObject target, GameObject source)
{
    Debug.Log($"{source} killed {target} with {eventData.Weapon}!");
}
```
---
### Modifying event data
Listeners can modify the event data they receive, so listeners afterwards will receive the modified data. This can be extremely useful for implementing features like damage type resistance/weakness:
```c#
public enum DamageType
{
    Bludgeoning,
    Fire,
    Cold
}

public struct DamagedEvent : IEvent
{
    public DamageType Type;
    public float Amount;
}
```
```c#
[SerializeField]
private DamageType resistanceType;

private void OnEnable()
{
    // Subscribe to the damage event targeting this game object with a higher priority than default.
    eventBus.SubscribeToTarget<DamagedEvent>(gameObject, OnDamagedEvent, 100f);
}

private void OnDisable()
{
    eventBus.UnsubscribeFromTarget<DamagedEvent>(gameObject, OnDamagedEvent);
}

private void OnDamagedEvent(ref DamagedEvent eventData)
{
    // If we are resistant to this damage type, halve the damage.
    if (eventData.Type == resistanceType)
    {
        eventData.Amount *= 0.5f;
    }
}
```
#### Consuming events
You can also stop the event completely using `ConsumeCurrentEvent()`. This can be used to implement a quick god mode script that's completely decoupled from the rest of the health/damage scripts:
```c#
[SerializeField]
private bool godMode;

private void OnEnable()
{
    // Subscribe to the damage event targeting this game object with a higher priority than default.
    eventBus.SubscribeToTarget<DamagedEvent>(gameObject, OnDamagedEvent, 100f);
}

private void OnDisable()
{
    eventBus.UnsubscribeFromTarget<DamagedEvent>(gameObject, OnDamagedEvent);
}

private void OnDamagedEvent(ref DamagedEvent eventData)
{
    // If we're in god mode, consume the event.
    if (godMode)
    {
        eventBus.ConsumeCurrentEvent();
    }
}
```