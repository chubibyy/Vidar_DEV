# Vidar - Project Master Documentation

**Vidar** is a multiplayer turn-based strategy game built with **Unity 6 (6000.3.2f1)**. 
It uses a **Client-Hosted Relay Architecture** (via Unity Relay) orchestrated by **Sessions** (Unity Multiplayer Services SDK 2.0).

---

## 1. Project Architecture

### **A. Core Services (Backend Layer)**
*Path: `Assets/Vidar/Scripts/Core/Services/`*
*   **MatchmakingManager (`MatchmakingManager.cs`)**:
    *   **The Brain:** Handles Server Discovery and Joining.
    *   **Logic:** Uses `EdgegapService` to Deploy On-Demand Servers (`POST /deploy`). Connects via `UnityTransport` (Direct IP).
    *   **Authentication:** Requires `AuthenticationManager` login first.
*   **EdgegapService (`EdgegapService.cs`)**:
    *   **Hosting Provider:** Manages API calls to Edgegap (Deploy, Status).
    *   **Config:** Uses `EdgegapConfig` (ScriptableObject) for API Keys and App Version.
*   **DedicatedServerManager (`DedicatedServerManager.cs`)**:
    *   **Server Lifecycle:** Generic implementation for Containerized Hosting.
    *   **Startup:** Reads Port from `-port` arg or `SERVER_PORT` env var. Starts `NetworkManager` immediately (no Allocation wait).
*   **AuthenticationManager (`AuthenticationManager.cs`)**:
    *   Wraps Unity Auth. Anonymous Login is default. Persists `PlayerId`.
*   **PlayerDataManager (`PlayerDataManager.cs`)**:
    *   **Cloud Save:** Syncs `PlayerProfile` (Gold, UnlockedUnits).
    *   **Gacha:** Handles `BuyPack()` logic securely (Deduct Gold -> Roll Registry -> Save).
*   **BootController (`BootController.cs`)**:
    *   **Entry Point:** Initializes Services.
    *   **Role Detection:** Checks `#if UNITY_SERVER` or `-mode server` args to start `DedicatedServerManager`.

### **B. Data Logic (ScriptableObjects)**
*   **CardRegistry**: The "Master List" of every valid Unit in the game. Used for Gacha rolls.
*   **CardDefinition**: Static data (Stats, Prefab, Team) for each Unit.
*   **PlayerProfile**: The C# Class defining the Cloud Database Schema.

### **C. Network Layer (Gameplay)**
*   **NetworkManager**: The Unity Netcode host. Uses `UnityTransport`.
*   **UnifiedBootstrap**: *Deprecated/Legacy*. Matchmaking logic moved to `MatchmakingManager`.
*   **TurnManager**: Authoritative Gameplay Loop.

---

## 2. Directory Structure

```text
Assets/Vidar/
├── Scenes/
│   ├── Boot.unity          # Service Init (Auto-redirects)
│   ├── Menu/
│   │   ├── Menu-Sign.unity # "Welcome" Screen
│   │   └── Menu-Global.unity # Main Hub (Play, Deck, Shop)
│   └── Match.unity         # Gameplay Map
├── Scripts/
│   ├── Core/
│   │   ├── Services/       # Auth, Data, Matchmaking
│   │   └── BootController.cs
│   ├── UI/                 # LoginUI, MainMenuUI
│   └── Gameplay/           # TurnManager, UnitState
├── ScriptableObjects/
│   ├── Config/             # MainCardRegistry
│   └── Factions/           # Unit Data (Guardians/Invaders)
```

---

## 3. Game Flow

1.  **Boot Phase:**
    *   `BootController` inits Auth & Data.
    *   If **Server Build**: Calls `MatchmakingManager.StartDedicatedServer()` -> Loads Match -> Waits.
    *   If **Client**: Loads `Menu-Sign`.
2.  **Login Phase:**
    *   `Menu-Sign` displays ID. User clicks "Enter" -> Loads `Menu-Global`.
3.  **Hub Phase:**
    *   **Play:** Calls `MatchmakingManager.FindMatch()`.
        *   Finds Session? -> Joins -> Starts Client.
        *   No Session? -> Creates Session -> Starts Host.
    *   **Shop:** Calls `PlayerDataManager.BuyPack()`.
    *   **Deck:** Displays `UnlockedHeroIds` using `CardRegistry` lookups.
4.  **Match Phase:**
    *   Netcode handles sync. `TurnManager` runs the game.

---

## 4. Build & Deployment (DevOps)

We use **Unity Build Profiles** to manage Client vs Server builds.

### **How to Build**
1.  Open **File > Build Profiles**.
2.  **Server Build:**
    *   Select Profile: **Vidar_Server**.
    *   Role: **Server** (Enforces Batchmode, defines `UNITY_SERVER`).
    *   Build it.
3.  **Client Build:**
    *   Select Profile: **Vidar_Client**.
    *   Role: **Client**.
    *   Build it.

### **How to Run (Local Testing)**
1.  **Start Server:**
    *   Terminal: `./Vidar_Server.app/Contents/MacOS/Vidar -logFile server.log`
    *   *Watch Logs:* `tail -f server.log`
    *   *Success:* Look for `[Matchmaking] Dedicated Session Created`.
2.  **Start Clients:**
    *   Launch `Vidar_Client.app` (Instance 1). Click **Find Match**.
    *   Launch `Vidar_Client.app` (Instance 2). Click **Find Match**.
    *   *Result:* They will join the Server's session.

---

## 5. Troubleshooting Guide

*   **"Namespace 'Lobby' missing":** Ensure `com.unity.services.multiplayer` is installed (v1.1+). We replaced the standalone packages.
*   **"Pulled: [Empty]":** Your `CardDefinition` asset has no "Display Name".
*   **"Purchase Failed":** You have 0 Gold. Reset account or grant starter gold in code.
*   **Client Timeout:** The `MultiplayerService` might not have auto-configured `UnityTransport`. Ensure `WithRelayNetwork()` was called on creation.
