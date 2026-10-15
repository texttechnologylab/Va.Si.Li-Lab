# Event System (Va.Si.Li-ES)

The event system lets users build *event systems* at run time, inside VR: **triggers** (an area is entered, an object
is used, a timer runs out) start **events** (spawn, move or delete objects, teleport players, change the weather,
start a vote or show feedback). Event systems can be saved as templates and loaded automatically when a scene starts.
It is described in the paper [Va.Si.Li-ES](../publications.md#vasili-es).

The code is in `Assets/Lab/EventSystem` (assembly `Vasili.EventSystem`). As a Unity package it is installed
separately from the core, see [Setting Up](../getting_started/setting_up.md#import-as-unity-package).

## Concepts

| Concept | Interface | Implementations (`Runtime/...`) |
|---|---|---|
| Event system | `IEventSystem` | `EventSystem`: holds triggers and events in numbered groups, can be repeatable |
| Trigger | `ITrigger` | `Triggers/AreaTrigger`, `InteractionTrigger`, `TimeTrigger` |
| Event | `IEvent` | `Events/ObjectSpawningEvent`, `ObjectMovingEvent`, `ObjectDestructionEvent`, `PlayerTeleportationEvent`, `WeatherEvent`, `VotingEvent`, `FeedbackEvent`, `FollowingEventSystemEvent` (starts another event system) |
| Modifier | `IModifier` | `Modifiers/Modifier_Gravity`, `_Jump`, `_Speed`, `_Weather`, `_Zoom`, `_Map`, `_Admin`; `Modefier_Handler` switches the modifiers in its `Modifier_Container` on or off according to the scene's `ModifierSettings` |

When all triggers of a group have fired, the events of that group run. Group `0` is shared: its triggers are required
by every group, and its events run whenever any group fires. A repeatable event system resets its triggers afterwards,
otherwise it deletes itself. Triggers and events serialise themselves to JSON (`createJson`), which is how templates
are stored.

## Building blocks

| Prefab | Purpose |
|---|---|
| `Manager/EventSystemManager` | Menu that lists the scene's event systems, loads templates and the scene's default event systems on start |
| `Factory/EventSystemFactory` | Creates one event system with its triggers and events |
| `MarkerTool/MarkerTool` | Tool for placing triggers and events in the scene |
| `EnvironmentSystem/Weather/Weather_Controller` | Weather (rain, snow, thunderstorm) used by `WeatherEvent` and `Modifier_Weather` |
| `VotingSystem/Voting_Controller` | Votes (yes/no or single choice) used by `VotingEvent` |
| `FeedbackSystem/Notification Manager`, `FeedbackWindow` | Notifications and feedback used by `FeedbackEvent` |
| `EventSystemPrefabs/*` | Ready-made items: binoculars, jump and sprint shoes, map, weather clouds, environment changer |

To use it in a scene, add the `EventSystemManager` prefab (and the controllers for the events you need) to the scene.

## Templates
`EventSystemManager` reads and writes templates at `eventTemplates + "/defaultTemplates"` (see
[Endpoints](endpoints.md)). A template belongs to the scene whose name it stores; templates of the current scene are
offered in the template panel, and default ones are created when the scene loads.

## Extending
- **New trigger:** a `MonoBehaviour` implementing `ITrigger`. Call `notifySystem()` when it fires, and return its
  state from `getState()`.
- **New event:** a `MonoBehaviour` implementing `IEvent`. Do the work in `trigger()`, show what will happen in
  `previewOn()`/`previewOff()`.
- **New modifier:** implement `IModifier` and add it to the `Modifier_Container` used by `Modefier_Handler`. The
  scene's `ModifierSettings` decide whether it is allowed and clamp its value.

Both triggers and events need `createJson(groupID)` so they can be saved in templates.
