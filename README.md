# Bust N' Blast

**A team-based multiplayer shooter for up to 10 players, running on dedicated servers hosted in the cloud with Hathora and synchronized with Photon Fusion 2.**

Built in Unity. Attackers and Defenders fight over a destructible arena, with AI-controlled NPCs on the map, four playable characters with different abilities, and a persistent account, rank and shop system.

> **Note on history:** this repository was imported from Unity Version Control (formerly Plastic SCM), where the project was developed.

## Highlights

- **Cloud-hosted dedicated servers.** A headless Linux/Windows server build is deployed to [Hathora](https://hathora.dev). Players are matched and connected automatically, with no manual IP entry or port forwarding.
- **Multiple game rooms per server process.** The server runs one Photon Fusion `NetworkRunner` for each Hathora room and port it is assigned, and starts and stops them as rooms appear and disappear.
- **Region-aware matchmaking.** The client pings every Hathora region, picks the lowest average latency, then joins an open public lobby or creates a new one.
- **Authoritative, tick-based netcode.** Server-authoritative gameplay built on Photon Fusion 2: networked state, tick timers, a kinematic character controller and lag-compensated hitboxes.
- **Combat sandbox.** Rifle, pistol, minigun, RPG, machete, molotov, tear gas, jetpack and deployable protection shields, plus destructible buildings with pooled fragments.
- **Persistent meta-game.** Account login, rank and XP, currency, and an item shop backed by PlayFab.

## Architecture

```text
 Unity client                          Hathora Cloud                   Dedicated server (container)
┌───────────────────┐   1. auth       ┌───────────────┐   room       ┌───────────────────────────────┐
│ FusionLauncher    │ ──────────────► │ Auth / Lobby  │   created    │ HathoraManager (entry point)  │
│ HathoraClient     │   2. ping       │ / Rooms /     │ ───────────► │ HathoraServer                 │
│ HathoraRegion...  │ ◄────────────── │ Discovery API │              │  ├─ polls active rooms (1 Hz) │
└───────┬───────────┘   regions       └───────────────┘              │  ├─ 1 NetworkRunner per room  │
        │ 3. list / create public lobby                              │  └─ maps room → exposed port  │
        │ 4. join Photon session lobby, wait for the room to appear  └──────────────┬────────────────┘
        │ 5. connect as GameMode.Client                                             │
        └───────────────────────────────────────────────────────────────────────────┘
                              Photon Fusion 2 game traffic (UDP)
```

### Connection flow (client)

1. **Login** to Hathora and obtain a player token (`HathoraClient.Initialize`).
2. **Pick a region.** Use the player's preferred region, or ping every Hathora endpoint six times and choose the lowest average (`HathoraRegionCollection.FindBestRegion`, with Frankfurt as the fallback).
3. **Find or create a lobby.** List the active public lobbies in that region. If none are open, create one through the Hathora Lobby API.
4. **Confirm the room is live.** Join the Photon session lobby and wait for the session whose name equals the Hathora `RoomId` to show up as open and not full. This avoids connecting before the server has actually booted the room.
5. **Timeout and recovery.** If nothing joinable shows up within a window, the client creates a fresh lobby and keeps waiting, up to a 30 s limit. Failures are reported back to the UI.
6. **Connect** as a Fusion `GameMode.Client` to the session in the chosen region.

### Server lifecycle (headless build)

- `HathoraManager` detects a dedicated-server platform and starts `HathoraServer`. It quits if the Hathora server context can't be retrieved.
- `HathoraServer` reads the process context from the Hathora API and polls it every second, then:
  - **Boots** a new Fusion `GameMode.Server` runner for each new active room. The room is bound to an unused exposed port, with the public address resolved from the Hathora host and the container port derived from the port name. The Photon region is mapped from the Hathora region the process runs in.
  - **Tears down** any runner whose Hathora room no longer exists.
  - Runs with Fusion multi-peer mode, so several rooms can share one process.
- Once all players are ready, the room closes and hides itself in the session list so no one can join a match in progress.

## Tech stack

| Area | Technology |
|---|---|
| Engine / language | Unity 2022.3.50f1 (URP), C# |
| Networking | Photon Fusion 2 (server mode, multi-peer, lag-compensated hitboxes, Simple KCC) |
| Server hosting and matchmaking | Hathora Cloud SDK for Unity (Auth, Lobby, Room, Process and Discovery APIs) |
| Accounts, progression, shop | Microsoft PlayFab |
| Gameplay tooling | Cinemachine, Input System, Animation Rigging, AI Navigation (NavMesh), ProBuilder |
| Serialization | Newtonsoft.Json |
| Version control | Git, Git LFS for large assets |

## Gameplay

- **Modes and rules:** Attackers vs Defenders, timed rounds, with a lobby ready-up phase before each match. The side with more points wins at the end of the round.
- **Characters:** Maverick, Bulwark, Nadja and Zaphyr, each with their own attachments and cosmetics.
- **Weapons and gadgets:** rifle, pistol, minigun, RPG, machete, molotov, tear gas, jetpack, protection shields.
- **World:** a destructible arena, with NPC agents navigating via NavMesh and reacting to placed shields.

## Project layout

```text
Assets/
├── Code/Scripts/
│   ├── Deployment/        # Hathora client, server, region mapping, entry point
│   ├── FusionHelpers/     # Launcher, session and player base classes, runner singletons
│   ├── Gameplay/          # App, GameManager, LevelManager, spawn points
│   ├── Player/ Weapons/   # Player, health, input, camera, weapon system
│   └── UI/                # HUD, ready-up, menus
├── Level/Devs/            # Per-developer scenes and features (menu, arena, shop, AI, weapons)
├── Hathora/               # Hathora Cloud SDK for Unity
├── Photon/                # Photon Fusion 2
└── PlayFabSDK/            # PlayFab SDK
Packages/                  # Unity Package Manager manifest
ProjectSettings/           # Unity project settings
```

Key entry points for the network layer:

- [`Assets/Code/Scripts/Deployment/HathoraServer.cs`](Assets/Code/Scripts/Deployment/HathoraServer.cs): room polling, per-room runners, port binding
- [`Assets/Code/Scripts/Deployment/HathoraClient.cs`](Assets/Code/Scripts/Deployment/HathoraClient.cs): login, lobby discovery and creation, join with timeout
- [`Assets/Code/Scripts/Deployment/HathoraRegionCollection.cs`](Assets/Code/Scripts/Deployment/HathoraRegionCollection.cs): latency-based region selection, Hathora↔Photon region mapping
- [`Assets/Code/Scripts/FusionHelpers/Launcher/FusionLauncher.cs`](Assets/Code/Scripts/FusionHelpers/Launcher/FusionLauncher.cs): client connection state machine

## Getting started

### Requirements

- Unity Editor `2022.3.50f1`
- Windows 10+, macOS or Linux
- About 2 GB of free disk space, 8 GB RAM recommended
- [Git LFS](https://git-lfs.com) (large assets are tracked with LFS)

### Run in the editor

```bash
git lfs install
git clone https://github.com/AdrianCDS/bust_n_blast.git
```

1. Add the cloned folder in Unity Hub and open it with Unity `2022.3.50f1`.
2. Open `Assets/Level/Devs/Goran/MainMenuCopy.unity` (start scene).
3. Press **Play**. The arena scene is `Assets/Level/Devs/Adrian/ArenaExtended.unity`.

Both scenes must be in **File → Build Settings → Scenes In Build** (use *Add Open Scenes*).

### Multiplayer modes

| Mode | What it does | What you need |
|---|---|---|
| **Quick Play** | Host/client session through Photon, no dedicated server | A Photon Fusion App ID |
| **Server Play** | Connects through Hathora to a cloud dedicated server | A Hathora app and a deployed server build |

### Bring your own credentials

The project needs your own service accounts to run online. Set these in the Unity Editor:

1. **Photon Fusion:** create an App ID and enter it in *Fusion → Realtime Settings*.
2. **PlayFab:** set your Title ID in the PlayFab editor extension.
3. **Hathora:** create an app, then fill in the client and server config assets in `Assets/Code/Scripts/Deployment/` (app ID, org ID and app secret or dev token).

### Build and deploy the dedicated server

1. In **Build Settings** select the **Dedicated Server** platform (Linux) and build.
2. Containerize the build and deploy it from the Hathora Unity window or the Hathora console, exposing the game port (UDP) along with any additional ports you want for extra rooms per process.
3. Start the client, choose **Server Play** and the region you deployed to.

## Team

- **[Adrian-Mihai Codăuși](https://github.com/AdrianCDS)**
- **[Goran-Gabriel Codăuși](https://github.com/goran-cds)**

## License

[MIT](LICENSE). Third-party assets (Photon Fusion, PlayFab SDK, Hathora SDK and the art and audio packs in `Assets/3rdParty Assets` and related folders) remain under their own licenses.
