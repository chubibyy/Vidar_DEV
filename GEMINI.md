# Vidar - Project Documentation

**Vidar** is a multiplayer turn-based strategy game built with **Unity 6 (6000.3.2f1)**. It features a robust **Backend-as-a-Service (BaaS)** architecture using Unity Gaming Services (UGS) for persistence, economy, and authentication.

---

## 1. Core Architecture

The project is divided into three distinct layers: **Core Services**, **Data**, and **Gameplay**.

### **A. Core Services (Backend)**
Located in `Assets/Vidar/Scripts/Core/Services/`.
*   **AuthenticationManager:** Handles user login via Unity Authentication (currently Anonymous). Persists the `PlayerId`.
*   **PlayerDataManager:** The central bridge to the Database (Unity Cloud Save).
    *   **Responsibilities:** Loads/Saves `PlayerProfile`, manages Gold currency, unlocks Heroes, and handles Gacha logic (`BuyPack`).
    *   **Singleton:** Persists across scenes (`DontDestroyOnLoad`).

### **B. Data Model**
Located in `Assets/Vidar/Scripts/Core/Data/`.
*   **PlayerProfile (Class):** The schema stored in the cloud. Contains:
    *   `Gold` (int)
    *   `UnlockedHeroIds` (List<int>)
    *   `Level` (int)
*   **CardRegistry (ScriptableObject):** The "Master List" of all existing units. Used by the Gacha system to pick random rewards.
*   **CardDefinition (ScriptableObject):** Defines a Unit's static data (Stats, Prefab, Icon, Team).

### **C. Gameplay Loop**
Located in `Assets/Vidar/Scripts/Gameplay/` and `Network/`.
*   **TurnManager:** The authoritative game controller. Manages turns, valid moves, and unit spawning.
*   **UnitState:** Networked component on Unit Prefabs. Syncs Health and Team ID to all clients.
*   **UnifiedBootstrap:** Handles the connection to the Match (Host/Client).

---

## 2. Scene Flow & User Experience

The game follows a strict linear flow to ensure data integrity.

1.  **Boot Scene (`Assets/Vidar/Scenes/Boot.unity`)**
    *   **Logic:** `BootController.cs`.
    *   **Action:** Initializes Authentication and loads Cloud Data.
    *   **Transition:** Auto-loads `Menu-Sign` upon success.
    *   **Dependencies:** Must contain `Services` object with Auth & Data managers.

2.  **Login Scene (`Assets/Vidar/Scenes/Menu-Sign.unity`)**
    *   **Logic:** `LoginUI.cs`.
    *   **Action:** Displays "Welcome [PlayerID]". Waits for user input.
    *   **Transition:** Loads `Menu-Global` when "Enter" is clicked.

3.  **Hub Scene (`Assets/Vidar/Scenes/Menu-Global.unity`)**
    *   **Logic:** `MainMenuUI.cs`.
    *   **Features:**
        *   **Play Tab:** Starts a Match (Host/Client logic via `UnifiedBootstrap`).
        *   **Deck Tab:** Displays unlocked units using `deckGridContainer` and `cardUiPrefab`.
        *   **Shop Tab:** Gacha system (`BuyPack`) consumes Gold to unlock units.
    *   **Dependencies:** Needs `NetworkManager` object for eventual game start.

4.  **Match Scene (`Assets/Vidar/Scenes/Match.unity`)**
    *   **Logic:** `TurnManager.cs`.
    *   **Action:** The actual gameplay.

---

## 3. Developer Guides

### **How to Add a New Unit**
1.  **Create Prefab:**
    *   Duplicate `Assets/Vidar/Prefabs/Units/Base/Hero.prefab`.
    *   Modify visual model.
    *   Ensure it has `UnitState` and `NetworkObject` components.
    *   Save to `Assets/Vidar/Prefabs/Units/[Faction]/`.
2.  **Create Data:**
    *   Go to `Assets/Vidar/ScriptableObjects/Factions/[Faction]/`.
    *   Right-Click > **Create > Vidar > Unit Card Definition**.
    *   Assign a **Unique ID**, Stats, and the **Prefab** you just made.
3.  **Register:**
    *   Select `Assets/Vidar/ScriptableObjects/MainCardRegistry`.
    *   Add the new Data asset to the list.
    *   *(Also add to `TurnManager`'s list in the Match scene if manual spawning is used)*.

### **How to Reset Player Data**
Since data is stored in the Cloud:
*   **Option A:** In Unity Editor, **Edit > Clear All PlayerPrefs** (Resets the Anonymous Login ID, creating a new account).
*   **Option B:** In Unity Dashboard, go to **Cloud Save > Data Explorer**, search for the Player ID, and delete the `player_profile` key.

### **Troubleshooting**
*   **"Authentication Failed":** Check `Project Settings > Services` links. Ensure Internet is active.
*   **"Missing Reference" Errors:** Restart Unity (Editor issue).
*   **"NetworkVariable written during shutdown":** Fixed in `TurnManager.cs` (removed `_players.Clear()` from Despawn).

---

## 4. Key Technologies
*   **Engine:** Unity 6 (6000.3.2f1)
*   **Networking:** Unity Netcode for GameObjects (NGO) + Unity Transport (UTP).
*   **Backend:** Unity Authentication + Unity Cloud Save.
*   **Rendering:** Universal Render Pipeline (URP).
