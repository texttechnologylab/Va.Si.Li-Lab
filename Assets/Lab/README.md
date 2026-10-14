# Lab

Va.Si.Li-Lab's own code and content. Each subfolder is a UPM package.

| Folder | Contents |
|---|---|
| `Core/` | Networking, scene/role management, player, avatars (`org.texttechnologylab.vasili`) |
| `EventSystem/` | Event/scenario system built on Core (`org.texttechnologylab.vasili-es`) |

Inside a package: `Runtime/` (scripts, prefabs, art), `Editor/` (editor tools), `Samples/` (example scenes, shipped as UPM samples).

Server addresses are not stored here: create a `VasiliEndpoints` asset via *Tools > Va.Si.Li > Create Endpoints Config*.

> [!NOTE]
> For developers: if you add a new feature (rather than changing an existing one), give it its own subfolder.
