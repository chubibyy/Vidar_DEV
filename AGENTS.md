# Vidar - Agent Guidelines

**Vidar** est un jeu de stratégie tour par tour multijoueur (1v1) construit avec **Unity 6**, utilisant un modèle client-serveur centralisé.

---

## 🎮 Contexte du Projet

- **Architecture:** Modèle "Server-Authoritative" (serveur gère l'état de jeu, clients envoient les actions)
- **Mode de jeu:** 1v1 réseau avec dédié server headless
- **Moteur:** Unity 6 (6000.3.2f1) avec NGO v2.7.0 (Netcode for GameObjects)
- **Hébergement:** Edgegap (Distributed Cloud pour conteneurs Docker)

---

## 🏗️ Architecture Globale

### Modèle de Jeu
```
┌─────────────────────────────────────────────────────┐
│                   CLIENT (Unity)                    │
│  ┌─────────────┐  ┌─────────────┐  ┌───────────┐   │
│  │   UI Layer  │  │ Input Layer │  │   Camera  │   │
│  └─────────────┘  └─────────────┘  └───────────┘   │
│  ┌─────────────┐  ┌─────────────┐                 │
│  │ Deck System │  │Matchmaking  │─────────────────┘
│  │   Client    │  │Edgegap API  │                 │
│  └─────────────┘  └─────────────┘                 │
└─────────────────────────────────────────────────────┘
                           │ UDP (UnityTransport)
                           │ Port: 7777 (serveur), 7979 (local dev)
                           ▼
┌─────────────────────────────────────────────────────┐
│                SERVER (Docker + Edgegap)           │
│  ┌───────────────────────────────────────────┐     │
│  │         Game Loop (Server Authority)     │     │
│  │  ┌────────────┐  ┌──────────────┐        │     │
│  │  │TurnManager │  │  DeckSystem  │        │     │
│  │  └────────────┘  └──────────────┘        │     │
│  └───────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────┘
```

### Règles d'Or du Networking
1. **Toute action affectant l'état de jeu passe par le serveur** (RPC Server, validation serveur)
2. **Le client gère uniquement:** Input visuel, UI, animation, caméra
3. **Le serveur gère:** Validation, synchronisation d'état, conditions de victoire
4. **Tous les comportements réseau hérent de `NetworkBehaviour`**

---

## 📁 Structure du Projet

```
Assets/Vidar/
├── Scripts/
│   ├── Core/                # Services & Persistent Data
│   │   ├── Services/        # Edgegap, Matchmaking, Server Manager
│   │   └── Data/            # PlayerProfile, CardRegistry
│   ├── Gameplay/            # Logique de match
│   │   ├── TurnManager.cs       # Boucle de tour, validation
│   │   ├── Cards/               # Deck, CardDefinition
│   │   └── Placements/          # Raycast pour placement
│   └── UI/                   # Interfaces
│       ├── MainMenuUI.cs        # Hub menu
│       ├── MatchUI.cs           # HUD de match
│       └── MatchDeckUI.cs       # Main du joueur
├── Scenes/
│   ├── Boot.unity           # Initialisation Services + NetworkManager
│   ├── Menu/                # Login & Hub
│   └── Match.unity          # Arena (charge sur serveur)
├── ScriptableObjects/
│   └── Data/Cards/          # CardDefinition assets
├── Prefabs/                 # Game objects pré-faits
└── Resources/Config/        # Edgegap credentials
```

---

## 🔧 Scénarios de Développement

### Mode Production (Edgegap)
1. **Client:** Lance l'app → Connecte via Edgegap API
2. **Edgegap:** Déploie un conteneur Docker serveur près du joueur
3. **Server:** Docker container → Unity headless → Netcode sur 0.0.0.0:7777
4. **Match:** Client ↔ Edgegap Server ↔ Game Loop
5. **Fin:** `Application.Quit()` pour libérer le conteneur

### Mode Local (Dev)
1. **Unity Play Mode:** `Window > Multiplayer > Multiplayer Play Mode`
2. **Première instance:** Mode Server (port 7979)
3. **Instances suivantes:** Mode Client
4. **Alternative:** `-mode server` flag + parrelSync pour tests async

---

## 🎯 Patterns Clés

### Networking (Unity NGO)
- `NetworkBehaviour` pour tous les composants réseau
- `NetworkVariable<T>` pour synchronisation d'état simple
- `[ServerRpc(RequireOwnership = false)]` pour actions multi-clients validées serveur
- `NetworkList<ulong>` pour tracking clients connectés
- **Jamais** d'état critique en RPC Client

### Gestion des Cartes
- `CardDefinition` (ScriptableObject): ID, nom, stats (mana, health, prefab)
- `CardRegistry`: Database centrale de toutes les définitions
- `PlayerProfile`: Deck du joueur (JSON cloud save)
- `DeckManager`: Resolution runtime des IDs vers prefabs

### Input & Placement
- Support Input System (nouveau) + Legacy Input System
- `PlacementClient`: Raycast pour placement click/drag
- `CameraRig`: Dual mode (Master isométrique / TPS suit hero)
- `HeroController`: Input local du hero spawné

---

## ⚠️ Pièges Élevés

1. **Binding serveur:** DOIT être `0.0.0.0:7777` (pas localhost) pour Edgegap
2. **CardRegistry:** Assigner au composant serveur, sinon NRE sur resolution IDs
3. **NetworkManager:** DOIT être présent dans Boot scene, sinon serveur crash
4. **Port conflict:** Local dev utilise 7979, Production utilise 7777
5. **Build Server:** Profiler "Vidar_Server", Platform Linux x86_64, Server Build ON
6. **Container cleanup:** Appeler `Application.Quit()` à fin de match pour Edgegap

---

## 🛠️ Commandes Utiles

### Docker
```bash
# Build local
docker build -t vidar-server:v1 .

# Test Run
docker run --rm -p 7777:7777 vidar-server:v1 -mode server -batchmode

# Push Edgegap
docker tag vidar-server:v1 registry.edgegap.com/<ORG>/vidar-game:v1
docker push registry.edgegap.com/<ORG>/vidar-game:v1
```

### Unity
```bash
# Build Server (Linux)
Build Settings → Vidar_Server Profile
Platform → Linux x86_64
Server Build → Checked

# Build Client (Windows/Mac)
Build Settings → [Votre OS]
Server Build → Unchecked
```

---

## 📊 Donnée Persistante

**PlayerProfile** (Cloud Save via Unity Cloud Save):
```json
{
  "gold": 100,
  "unlockedCardIds": ["card_001", "card_003"],
  "currentDeckIds": ["card_001", "card_003", "card_007"]
}
```

**Flow:**
1. Login → Load profile from cloud
2. Menu → Builder débloque/enregistre le deck
3. Match → Deck current envoyé serveur via RPC
4. Défaite/Win → Save profile (gold, unlocks)

---

## 🚀 Workflow de Dév Typique

1. **Édition Unity:** Modifier CardDefinition SO, rebuilder prefabs si needed
2. **Test Local:** Multiplayer Play Mode (Hot Reload ON)
3. **Build:** Editor →/Linux/Build Server
4. **Test Docker:** `docker run` local, vérifier binding 0.0.0.0:7777
5. **Test Edgegap:** Dashboard → Deploy → Test connection via URL retournée
6. **Bug Fix:** Isoler sur Local → Fix → Rebuild → Test Docker/Edgegap

---

## 🔍 Debugging Rapide

| Symptôme | Vérification |
|----------|-------------|
| "Connection Failed" | Binding serveur: 0.0.0.0? Firewall UDP? |
| NullRef TurnManager | CardRegistry assignée à l'inspector? |
| Serveur charge Menu | NetworkManager dans Boot scene? |
| Deck empty | PlayerDataManager dans Boot? DeckManager dans Match? |
| Port mismatch | Edgegap Dashboard = 7777 (serveur), pas 7979 |

---

> **Note:** Tous les changements affectant le networking DOIVENT être testés sur le serveur headless, pas seulement en Play Mode Unity.
