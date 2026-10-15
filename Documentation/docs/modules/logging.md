# Logging

Va.Si.Li-Lab records what happens in a session (movement, gaze, faces, hands, interactions, audio) and sends it to
the [Database API](../server/python_data_logger.md), which stores it in [MongoDB](../server/mongodb.md). The data
model behind it is described in
[A Multimodal Data Model for Simulation-Based Learning with Va.Si.Li-Lab](../publications.md#mehler2023).

The code is in `Assets/Lab/Core/Runtime/Scripts/Logging`. The address comes from `api` in the
[endpoints configuration](endpoints.md).

## MetaDataLogger
`MetaDataLogger` on the `Social Network Scene` prefab collects the data of the local player and sends it in batches:

| Route (`api` + ...) | Content |
|---|---|
| `/logging/playerLogIn` | A player joins |
| `/logging/playerRoleLogIn` | A player takes a role |
| `/logging/levelChange` | The admin changes the level |
| `/logging/player` | Head, hand and body tracking; with Meta avatars also eyes, face and fingers (`metaMessage`) |
| `/logging/object` | Positions of logged objects |
| `/logging/special` | Button presses and spawned objects |
| `/logging/logMisc` | Other events |

The message types are in `DataLoggerTypes.cs`. How the API splits them into collections is described in the
[Database API documentation](https://texttechnologylab.github.io/Va.Si.Li-Lab-backend/database_api/).

!!! warning "Logging failures don't stop a session"
    If the API can't be reached, the session continues normally and nothing is stored. Check that data arrives
    (e.g. `api + /logging/status`, shown by `ConnectionStatusMonitor`) before a study session starts.

## Other loggers
| Component | Purpose |
|---|---|
| `FileLogger` | Writes the JSON messages to the local file system instead |
| `LogManager`, `Collectors/MovementLogCollector` | Collector-based logging (`ILogCollector`, `IContinousLogCollector`, `IEventLogCollector`) |
| `LogToScreen` | Shows Unity log output in VR |
| `DevLogConfig` | Disables per-log stack traces in development builds |
