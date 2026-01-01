# Vidar - Project Master Documentation

**Vidar** is a multiplayer turn-based strategy game built with **Unity 6 (6000.3.2f1)**.  
It leverages **Edgegap** for on-demand containerized hosting and **Unity Netcode for GameObjects (NGO)** for authoritative networking.

---

## 1. High-Level Architecture

The project follows a **Client-Server** authoritative model.
*   **Client:** Responsible for UI, Input, and Visuals.
*   **Server:** Responsible for Game State, Validation, and Win Conditions.
*   **Hosting:** Servers are stateless containers spun up on-demand via Edgegap's Distributed Cloud.

### **Technology Stack**
| Component | Technology | Description |
| :--- | :--- | :--- |
| **Engine** | Unity 6 (6000.3.2f1) | Core game engine. |
| **Networking** | Netcode for GameObjects (NGO) | State synchronization & RPCs. |
| **Transport** | Unity Transport (UTP) | UDP-based packet delivery. |
| **Hosting** | Edgegap | Arbitrium Orchestrator for Docker containers. |
| **Auth** | Unity Authentication | Anonymous login & Player ID persistence. |
| **Database** | Unity Cloud Save | Player profile (Gold, Unlocked Cards, Current Deck). |
| **DevOps** | Docker | Linux x86_64 containerization. |

---

## 2. Directory Structure & Key Files

```text
Assets/Vidar/
├── Resources/
│   └── Config/
│       └── EdgegapConfig.asset    # Stores API Keys & App Version for Launcher
├── Scenes/
│   ├── Boot.unity                 # Entry Point. Initializes Services & NetworkManager.
│   ├── Menu/
│   │   ├── Menu-Sign.unity        # Login Screen
│   │   └── Menu-Global.unity      # Main Hub (Deck Builder, Shop, Find Match)
│   └── Match.unity                # The Game Arena (Server loads this immediately)
├── Scripts/
│   ├── Core/
│   │   ├── BootController.cs      # Decides if we are Client or Server
│   │   ├── Data/
│   │   │   ├── PlayerProfile.cs   # JSON Data: Gold, UnlockedCards, CurrentDeckIds
│   │   │   └── CardRegistry.cs    # SO: Central Database of all CardDefinitions
│   │   ├── Services/
│   │   │   ├── DedicatedServerManager.cs # SERVER: Starts Netcode on 0.0.0.0:7777
│   │   │   ├── MatchmakingManager.cs     # CLIENT: Requests Edgegap Server
│   │   │   ├── PlayerDataManager.cs      # DATA: Cloud Save (Load/Save Profile & Deck)
│   │   │   └── EdgegapBootstrap/         # EDGEGAP Integration
│   ├── Gameplay/
│   │   ├── TurnManager.cs         # Core Game Loop (Turns, RPCs for Summoning)
│   │   ├── Cards/                 # Card Logic
│   │   │   ├── DeckManager.cs     # GAME: Loads player deck for the match
│   │   │   └── CardDefinition.cs  # SO: Stats (Mana, Health, Prefab)
│   │   ├── Placements/
│   │   │   └── PlacementClient.cs # MAP: Raycast logic for unit placement
│   └── UI/                        # UI Logic
│       ├── MainMenuUI.cs          # Hub UI (Deck Building & Shop)
│       ├── MatchUI.cs             # In-Game HUD (Turn Info)
│       └── MatchDeckUI.cs         # In-Game Hand (Card Buttons & Interactions)
```

---

## 3. Server Lifecycle (The "Backend")

The Dedicated Server is a **Headless Linux Build** wrapped in a Docker container.

### **A. Startup Sequence**
1.  **Docker Launch:** The container starts via command:
    `./Vidar_DEV -mode server -batchmode -nographics -port 7777`
2.  **Scene 0: Boot.unity:**
    *   `BootController.Start()` executes.
    *   Detects Server Mode via `#if UNITY_SERVER` or `-mode server` argument.
    *   Calls `DedicatedServerManager.StartServerService()`.
3.  **Netcode Initialization:**
    *   `DedicatedServerManager` finds `NetworkManager` (must be present in Boot!).
    *   Binds `UnityTransport` to IP **`0.0.0.0`** (All Interfaces) on Port **`7777`**.
    *   Calls `NetworkManager.Singleton.StartServer()`.
4.  **Scene Load:**
    *   Immediately loads `Match.unity`.
    *   Server is now "Ready" and waiting for clients.

### **B. Edgegap Integration**
*   **Env Vars:** Edgegap injects variables like `ARBITRIUM_PORTS_MAPPING` into the container.
*   **EdgegapServerBootstrap:** This script (if active) parses these variables to understand its public IP/Port, though `DedicatedServerManager` handles the critical bind logic.
*   **Termination:** When the match ends, the server should call `Application.Quit()` to kill the container (saving Edgegap costs).

---

## 4. Client Logic (The "Frontend")

The Client connects to the specific IP/Port returned by Edgegap.

### **A. Authentication & Data**
*   **Login:** `AuthenticationManager` logs the user in anonymously.
*   **Profile:** `PlayerDataManager` fetches `PlayerProfile` from Unity Cloud Save.
    *   **Data:** Contains Gold, Unlocked Hero IDs, and `CurrentDeckIds`.
    *   **Deck Building:** Handled in `MainMenuUI`. Players select cards from their collection to build a valid deck (max 4 cards), which is saved via `SaveDeck()`.
*   **Gacha:** `BuyPack()` runs client-side but saves results to Cloud immediately.

### **B. Matchmaking Flow**
1.  **User Action:** Player clicks "Find Match" in `Menu-Global`.
2.  **API Request:** `MatchmakingManager` calls `EdgegapLauncher.DeployServer()`.
    *   Payload: `{"app_name": "vidar-game", "version_name": "v1", "ip_list": ["<ClientIP>"]}`
3.  **Deployment:** Edgegap spins up a new server instance near the player.
4.  **Response:** Client receives JSON with `public_ip` and `external_port` (e.g., `30289`).
5.  **Warmup:** Client waits **2 seconds** to allow the server container to initialize Unity.
6.  **Connection:** `UnityTransport` updates target IP/Port -> `NetworkManager.Singleton.StartClient()`.

---

## 5. Network & Gameplay Structure

### **A. NetworkManager Setup**
*   **Location:** `Boot.unity` (Persistent "Services" GameObject).
*   **Protocol:** UDP (Unity Transport).
*   **Sync:** Uses `NetworkVariable<T>` for state (Health, Turn Number) and `RPC`s for actions.

### **B. Gameplay Loop (`Match.unity`)**
*   **TurnManager (NetworkBehaviour):**
    *   Manages `CurrentTurn` (Int) and `CurrentPhase` (Enum).
    *   **Registry:** Uses `CardRegistry` (ScriptableObject) to resolve Card IDs sent by clients.
    *   **Actions:**
        *   `SummonHeroServerRpc(cardId)`: Spawns unit in default zone.
        *   `PlaceHeroServerRpc(cardId, position)`: Spawns unit at specific raycast position.
*   **Deck System:**
    *   **DeckManager:** Loads the player's saved deck from `PlayerProfile` at start of match.
    *   **MatchDeckUI:** visualizes the hand. Clicking a card invokes `SummonHeroServerRpc`.
    *   **PlacementClient:** Handles raycasting for "drag & drop" or "click to place" style input.

---

## 6. DevOps: Build & Deploy Guide

### **A. Building for Edgegap**
1.  **Unity Build:**
    *   Profile: **Vidar_Server**
    *   Platform: **Linux (x86_64)**
    *   Options: **Server Build** (Checked)
    *   Output Path: `Builds/LinuxServer/`
2.  **Docker Build:**
    *   Command: `docker build -t vidar-server:v1 .`
    *   *Note: Dockerfile installs `libgtk-3-0` to support Unity headless dependencies.*
3.  **Push to Registry:**
    *   Tag: `docker tag vidar-server:v1 registry.edgegap.com/<ORG>/vidar-game:v1`
    *   Push: `docker push registry.edgegap.com/<ORG>/vidar-game:v1`

### **B. Edgegap Dashboard Config**
*   **App Name:** `vidar-game` (Must match Unity Config).
*   **Version:** `v1`.
*   **Port Mapping:** `7777` (Internal) -> UDP (Protocol).
*   **Image:** `registry.edgegap.com/<ORG>/vidar-game`.

### **C. Local Testing (ParrelSync / PlayMode)**
*   **Client Mode:** Use standard "Play". Ensure "Multiplayer PlayMode Tools" is set to Client/Host.
*   **Server Simulation:** Use `-mode server` arg or "Server" mode in tools to test `DedicatedServerManager` logic locally.

---

## 7. Troubleshooting Common Issues

*   **"Connection Failed" (Client):**
    *   Server might be binding to `localhost` instead of `0.0.0.0`. Check `DedicatedServerManager.cs`.
    *   Dashboard Port Protocol might be TCP (Must be UDP).
    *   Firewall/Anti-Virus blocking UDP packets.
*   **"NullReference in TurnManager":**
    *   `CardRegistry` field in Inspector is likely missing. Assign the SO.
*   **"Deck is Empty":**
    *   Ensure `PlayerDataManager` is in the scene (Boot) and Profile loaded.
    *   Check if `DeckManager` exists in Match scene.
*   **Server Loads Menu Scene:**
    *   `NetworkManager` is missing from `Boot` scene, causing `DedicatedServerManager` to crash/return early.
    *   Build Target in Editor was not switched back to "Client" (forces `UNITY_SERVER` define).