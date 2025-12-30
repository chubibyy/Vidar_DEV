# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Vidar is a **Unity 6 (6000.3.2f1) multiplayer turn-based game** featuring:
- 1v1 networked gameplay with dedicated server architecture
- Card-based hero summoning system
- Server-authoritative game loop using Unity Netcode for GameObjects (NGO) v2.7.0

## Development Commands

This is a Unity project - development is done through the Unity Editor. No CLI build commands available.

**Testing network locally:**
1. Open project in Unity Editor
2. First instance auto-starts as Server (port 7979)
3. Additional instances connect as Clients
4. Use Multiplayer Play Mode (Window > Multiplayer > Multiplayer Play Mode) for virtual clients

## Architecture

### Network Layer (`Assets/Vidar/Scripts/Network/`)
- **UnifiedBootstrap.cs** - Entry point; tries Server first, falls back to Client if port occupied
- **NetSettings.asset** - ScriptableObject config (address: 127.0.0.1, port: 7979)
- Server-authoritative model: clients send ServerRpc, server validates and broadcasts via ClientRpc

### Gameplay (`Assets/Vidar/Scripts/Gameplay/`)
- **TurnManager.cs** - Core networked component managing BoardState (turnIndex, activePlayer, move counts)
  - Validates player actions with IsMyTurn() checks
  - Handles hero spawning (SummonHero for random placement, PlaceHero for click placement)
  - State serialized as JSON bytes for network sync
- **HeroController.cs** - Client-side input handling for spawned heroes
- **Cards/CardDefinition.cs** - ScriptableObject defining cardId, displayName, heroPrefab
- **PlayerDeck.cs** - 5-card hand system referencing CardDefinitions

### Camera System (`Assets/Vidar/Scripts/Gameplay/Camera/`)
- **CameraRig.cs** - Dual-mode: Master (isometric board view, WASD/mouse control) and TPS (follows spawned hero)

### UI (`Assets/Vidar/Scripts/UI/`)
- **MatchUI.cs** - Turn info display, Make Move/End Turn buttons, conditional interactability
- **DeckInvokeUI.cs** - Card selection UI with visual feedback

## Key Patterns

**Networking:**
- All networked components inherit `NetworkBehaviour`
- Use `IsOwner` for client-authoritative input checks
- Use `[ServerRpc(RequireOwnership = false)]` with manual validation for server authority
- `NetworkList<ulong>` tracks connected clients

**Input System:**
- Dual support via `#if ENABLE_INPUT_SYSTEM` - prefers new Input System, falls back to legacy
- Input handled in `HeroController` and `CameraRig`

**Scene Flow:**
- Boot.unity → MVP.unity (loaded after network connection established)

## Project Structure

```
Assets/Vidar/
├── Scripts/
│   ├── Gameplay/     # TurnManager, HeroController, Cards, Camera
│   ├── Network/      # UnifiedBootstrap, NetSettings, NetDebug
│   └── UI/           # MatchUI, DeckInvokeUI, PlacementClient
├── Scenes/
│   ├── Boot/         # Network initialization scene
│   └── Sandbox/      # Main gameplay scene (MVP.unity)
├── ScriptableObjects/
│   └── Data/Cards/   # Card_*.asset definitions
└── Prefabs/          # Hero prefabs, UI elements
```

## Code Conventions

- C# without namespace wrapping
- Comments in mixed English/French
- NetworkBehaviour for any synced component
- ScriptableObjects for data definitions (cards, settings)
