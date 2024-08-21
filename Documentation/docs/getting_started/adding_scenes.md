# Adding Scenes
To connect a scene via MetaStart, 
you must first add the scene to the Unity project and then to the MongoDB database.

## Add Scene in Unity
### Create a new Scene
- Scenes are placed in the `Assets/Local/Scenes` folder. 
- Right-click in the `Assets/Local/Scenes` folder and select `Create > Scene`.
- Add and activate scene in the build settings `File/Build Setting > Add Open Scene`.

#### Debug Scene without MetaStart
Add the following prefabs: `Social Network Scene`, `MetaPlayer`, `AvatarSdkManagerHorizon` to the scene.
(Not testet. `Social Network Scene` e.g. is not nessessary for the scene to work.)

## Add Scene to MongoDB
The Scene must be added to the MongoDB database to be accessible via MetaStart.
As a student, please ask your coresponding supervisor to add the scene to the database.
The scenes need to be added to `Experiment/scenarios-languages`.
The .json structure is as follow:

``` json title="Parameter Explanation"
{
  "name": Name of the Scene in the scene selector,
  "internalName": Name of the Scene in Unity,
  "roles": Roles in the Scenario. Currently supporting english and german
    {
      "name": Name of the Role,
      "description": Description of the Role,
      "spawnPosition": Spawn Position of the Role,
      "mode": Mode of the Role (player, observer),
      "maxCount": Maximum Count of the Role,
      "admin": Admin Role (start and ending levels),
      "level": Level Descriptions for that role
    },
    "level": Levels in the Scenario,
      {
        "id": ID of the Level,
        "delay": Delay of the Level (stops the level after n minutes; null if no delay)
      },
    "enable": Visible via MetaStart,
    "amountPlayersRequired": Amount of Players Required, until admin can start the scenario
}
```

``` json title="Example Scene Entry"
{
  "name": "Praktikum Experiment 3",
  "shortName": "Praktikum 3",
  "author": "Alexander Henlein",
  "internalName": "PraktikumScenario03",
  "roles": {
    "DE": [
      {
        "name": "Person 1",
        "description": [
          {
            "id": 1,
            "description": "Rundfahrt erklären"
          }
        ],
        "spawnPosition": "P1",
        "mode": "player",
        "maxCount": 1,
        "admin": true,
        "level": [
          {
            "id": 1,
            "description": "Rundfahrt"
          },
          {
            "id": 2,
            "description": "Erklären"
          },
          {
            "id": 3,
            "description": "Warten"
          }
        ]
      },
      {
        "name": "Person 2",
        "description": [
          {
            "id": 1,
            "description": "Weg finden"
          }
        ],
        "spawnPosition": "P2",
        "mode": "player",
        "maxCount": 1,
        "admin": false,
        "level": [
          {
            "id": 1,
            "description": "Warten"
          },
          {
            "id": 2,
            "description": "Erklären lassen"
          },
          {
            "id": 3,
            "description": "Finden"
          }
        ]
      }
    ],
    "EN": [
      {
        "name": "Person 1",
        "description": [
          {
            "id": 1,
            "description": "Rundfahrt"
          }
        ],
        "spawnPosition": "P1",
        "mode": "player",
        "maxCount": 1,
        "admin": true,
        "level": [
          {
            "id": 1,
            "description": "Rundfahrt"
          },
          {
            "id": 2,
            "description": "Erklären"
          },
          {
            "id": 3,
            "description": "Warten"
          }
        ]
      },
      {
        "name": "Person 2",
        "description": [
          {
            "id": 1,
            "description": "Erklären lassen"
          }
        ],
        "spawnPosition": "P2",
        "mode": "player",
        "maxCount": 1,
        "admin": false,
        "level": [
          {
            "id": 1,
            "description": "Warten"
          },
          {
            "id": 2,
            "description": "Erklären lassen"
          },
          {
            "id": 3,
            "description": "Finden"
          }
        ]
      }
    ]
  },
  "level": [
    {
      "id": 1,
      "delay": null
    },
    {
      "id": 2,
      "delay": null
    },
    {
      "id": 3,
      "delay": null
    }
  ],
  "enable": true,
  "amountPlayersRequired": 2
}
```

