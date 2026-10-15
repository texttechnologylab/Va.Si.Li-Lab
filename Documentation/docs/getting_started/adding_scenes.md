# Adding Scenes
To connect a scene via MetaStart, 
you must first add the scene to the Unity project and then to the MongoDB database.

## Add Scene in Unity
### Create a new Scene
- Place your scenes in a folder of your own project, e.g. `Assets/Scenes`, not inside `Assets/Lab`.
- The easiest start is a copy of the `Example` sample scene (see [Special Scenes](special_scenes.md)). It already
  contains the `Social Network Scene` prefab for networking.
- Don't add a player or camera: the [player](player_controller.md) comes from MetaStart.
- Add a GameObject for each spawn point and name it like the role's `spawnPosition` (e.g. `DefaultSpawn`).
- Add the scene to the build profile (*File > Build Profiles > Scene List*), after MetaStart.

#### Debug Scene without MetaStart
To test a scene on its own, temporarily add the prefabs `MetaPlayer2.0` and `AvatarSdkManagerStyle2Meta` (from the
Meta Avatars *Sample Scenes*, folder `Common/Prefabs/AvatarSdkManager/Recommended`), as MetaStart does. Rename the
manager to `AvatarSdkManagerMeta`: the avatar scripts look it up by that name. Remove both again before building.

## Add Scene to the Database
The Scene must be added to the database to be accessible via MetaStart. There are
two ways to do this:

- **Via the Database API** (recommended). The
  [Database API](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/database_api/#creating-a-scene)
  now has endpoints for creating scenes, roles, levels and their locales, so you
  don't need direct database access - just an API key. Start there; the
  walkthrough builds exactly the structure documented below, one call at a time.
- **Directly in MongoDB.** As a student, ask your corresponding supervisor to add
  the scene for you. Scenes are stored in the `scenarios` collection, in the
  structure below.

This is the structure of a scene, as `GET /scene` returns it and as the client reads it (`ApiScene` in
`Assets/Lab/Core/Runtime/Scripts/SceneManagement/SceneManagerTypes.cs`):

| Field | Meaning |
|---|---|
| `name`, `shortName` | Name in the scene selector, and a short version where space is limited |
| `author` | Author of the scenario |
| `internalName` | Name of the Unity scene |
| `enabled` | Visible in MetaStart |
| `roles` | Roles by identifier (your own key, e.g. `interviewee`) |
| `roles.<id>.mode` | `player` or `spectator` (`webcam` exists but is not used by the client yet) |
| `roles.<id>.spawnPosition` | Name of the GameObject in the scene the role spawns at |
| `roles.<id>.maxCount` | Maximum number of players with this role |
| `roles.<id>.admin` | The admin starts and ends the levels |
| `roles.<id>.restrictions` | Restrictions of the role, e.g. `blur` |
| `roles.<id>.locales.<lang>` | `name` and `description` of the role per language (`EN`, `DE`) |
| `levels` | The stages of the scenario, in order |
| `levels[].delay` | Stops the level after n minutes (`null`: no limit) |
| `levels[].roleDescriptions.<id>.locales.<lang>` | What the role does in this level |

The example below defines the `InterView` sample scene: an interviewee in VR who starts and ends the level, and an
optional spectator. The spawn points `DefaultSpawn` and `spectator` exist in that scene.

``` json title="Example Scene Entry"
{
  "name": "InterView Sample",
  "shortName": "InterView",
  "author": "Jane Doe",
  "internalName": "InterView",
  "enabled": true,
  "roles": {
    "interviewee": {
      "mode": "player",
      "spawnPosition": "DefaultSpawn",
      "maxCount": 1,
      "admin": true,
      "restrictions": [],
      "locales": {
        "EN": { "name": "Interviewee", "description": ["Answer the interviewer's questions."] },
        "DE": { "name": "Befragte Person", "description": ["Beantworte die Fragen der interviewenden Person."] }
      }
    },
    "spectator": {
      "mode": "spectator",
      "spawnPosition": "spectator",
      "maxCount": 1,
      "admin": false,
      "restrictions": [],
      "locales": {
        "EN": { "name": "Spectator", "description": ["Watch the interview."] },
        "DE": { "name": "Beobachtung", "description": ["Beobachte das Interview."] }
      }
    }
  },
  "levels": [
    {
      "id": 1,
      "delay": null,
      "roleDescriptions": {
        "interviewee": { "locales": { "EN": { "description": ["Interview"] }, "DE": { "description": ["Interview"] } } },
        "spectator": { "locales": { "EN": { "description": ["Observe"] }, "DE": { "description": ["Beobachten"] } } }
      }
    }
  ]
}
```
