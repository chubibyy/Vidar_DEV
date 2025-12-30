# Vidar - Project Overview

**Vidar** is a multiplayer game project built with **Unity 6 (6000.2.1f1)**. It utilizes **Unity Netcode for GameObjects (NGO)** for networking and the **Universal Render Pipeline (URP)** for rendering.

## Architecture & Structure

The core project content is isolated within `Assets/Vidar/` to keep it separate from third-party assets and packages.

### Key Directories

*   **`Assets/Vidar/Scripts/`**: Contains the source code, organized by domain:
    *   `Gameplay/`: Core game mechanics (e.g., `HeroController`, `TurnManager`, `BoardState`).
    *   `Network/`: Networking logic (`UnifiedBootstrap`, `NetSettings`).
    *   `UI/`: User interface scripts.
    *   `AI/`: Artificial Intelligence logic.
*   **`Assets/Vidar/Scenes/`**: Game scenes:
    *   `Boot`: Likely the entry point.
    *   `Menu`: Main menu.
    *   `Match`: The main gameplay loop.
    *   `Sandbox`: Testing environment.
*   **`Assets/Vidar/ScriptableObjects/`**: Data configuration files (e.g., `NetSettings`).
*   **`Assets/Vidar/Prefabs/`**: Game object prefabs (`Hero`, `NetworkPrefabs`).

## Key Technologies

*   **Engine:** Unity 6 (6000.2.1f1)
*   **Networking:** Unity Netcode for GameObjects (NGO) + Unity Transport (UTP).
    *   **Bootstrap:** `UnifiedBootstrap.cs` handles automatic server/client startup logic.
    *   **Dedicated Server:** Supported via `com.unity.dedicated-server`.
*   **Input:** Unity Input System package (`InputSystem_Actions`), with fallback support for legacy input in some scripts.
*   **Rendering:** Universal Render Pipeline (URP).
*   **Services:** Vivox (Voice Chat), Unity Multiplayer Services.

## Building and Running

1.  **Open Project:** Open the project directory in Unity 6000.2.1f1.
2.  **Entry Point:** The game likely starts from `Assets/Vidar/Scenes/Boot.unity`.
3.  **Networking:**
    *   The project uses `UnifiedBootstrap` to attempt starting as a Server.
    *   If the port is occupied, it falls back to starting as a Client.
    *   Network settings are managed via `NetSettings` ScriptableObjects.

## Development Conventions

*   **Language:** C#
*   **Namespaces:** Scripts observed so far do not use wrapping namespaces (e.g., `HeroController` is in the global namespace).
*   **Networking:**
    *   Networked components inherit from `NetworkBehaviour`.
    *   Use `IsOwner` checks for client-authoritative input (e.g., in `HeroController`).
*   **Comments:** Some code comments are in **French** (e.g., inside `UnifiedBootstrap.cs`).
*   **Input:** Prefer `UnityEngine.InputSystem` over legacy `Input`.
