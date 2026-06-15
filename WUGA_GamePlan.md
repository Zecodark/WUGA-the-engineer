# WUGA — Game Design Document & Technical Plan

> **Last Updated:** Session 2026-06-15
> **Unity Version:** 6000.3.9f1 (Unity 6)
> **Render Pipeline:** URP 17.3.0
> **Genre:** 3D Puzzle + Melee Combat
> **Art Style:** Low-Poly / Stylized
> **Camera:** Third-Person

---

## Table of Contents
1. [Game Overview](#1-game-overview)
2. [Story & Narrative](#2-story--narrative)
3. [Character Design](#3-character-design)
4. [Level Design (1-5)](#4-level-design-1-5)
5. [Scene Architecture](#5-scene-architecture)
6. [Quest System](#6-quest-system)
7. [Combat System](#7-combat-system)
8. [Enemy AI Design](#8-enemy-ai-design)
9. [NPC Robot System](#9-npc-robot-system)
10. [Item Collection System](#10-item-collection-system)
11. [Animation Plan](#11-animation-plan)
12. [UI System](#12-ui-system)
13. [Shader & VFX Plan](#13-shader--vfx-plan)
14. [Technical Architecture](#14-technical-architecture)
15. [Development Roadmap](#15-development-roadmap)

---

## 1. Game Overview

### Concept
WUGA adalah game 3D puzzle-combat di mana pemain (mahasiswa informatika) tersedot ke dalam dunia digital setelah menggunakan cheatcode. Di dunia ini, pemain harus mengumpulkan komponen komputer, melawan malware, dan menyelesaikan puzzle untuk kembali ke dunia nyata.

### Core Loop
```
Explore -> Grab Komponen -> Fight Malware -> Solve Puzzle -> Complete Quest -> Next Area
```

### Key Features
- 5 Level dengan kesulitan progresif
- Sistem quest dari NPC Robot
- **Grab mechanic** — tekan G untuk mengambil komponen, dengan animasi per item
- Melee combat melawan malware (Virus, Trojan, Worm, dll)
- Puzzle berbasis komponen komputer
- Environment digital/cyber dengan Shader Graph effects
- Story-driven progression

---

## 2. Story & Narrative

### Prolog (Level 1 — Tutorial)

**Setting:** Ruangan praktikum kampus, malam hari.

**Story Flow:**
```
SCENE 1: Ruangan Kelas
- WUGA (mahasiswa) sedang mengerjakan tugas berat sendirian
- Monitor komputer menyala terang
- WUGA frustasi, mengetik cheatcode
- Monitor berkedip, glitch effect muncul
- Layar TV error / static noise
- WUGA tersedot ke dalam monitor!

TRANSISI: Animasi Jatuh
- Kamera berputar (Cinemachine dolly)
- Background: Matrix-like falling code
- Efek glitch + chromatic aberration
- WUGA jatuh dari ketinggian
- Landing di dunia digital

SCENE 2: Dunia Digital (Level 1 Start)
- WUGA terbangun di environment cyber
- Robot NPC pertama muncul (tutorial guide)
- Robot menjelaskan situasi: "Kamu berada di dalam CPU"
- Quest pertama: grab 5 komponen
- Tutorial: berjalan, lompat, interact, grab (press G)
```

### Narasi Level 2-5
- **Level 2:** Malware pertama muncul, robot NPC menjelaskan ancaman
- **Level 3:** Area lebih kompleks, boss pertama (Trojan King)
- **Level 4:** Puzzle multi-step, kombinasi komponen
- **Level 5:** Final boss (Ransomware Lord), WUGA kembali ke dunia nyata

---

## 3. Character Design

### WUGA (Player Character)
- **Visual:** Mahasiswa, low-poly stylized, kaos + celana jeans
- **Source:** AI-Generated (Meshy.ai / CSM) -> import FBX
- **Animations needed:**
  - Idle, Walk, Run, Jump
  - Attack (punch/swing)
  - Grab Small (one-hand: RAM, CPU, Cable)
  - Grab Medium (two-hand: VGA, SSD)
  - Grab Large (bend down + two-hand: Casing, Motherboard, PSU)
  - Interact (press E untuk talk/activate)
  - Hit reaction, Death
  - Fall animation (transisi prolog)

### NPC Robot
- **Visual:** Robot kecil, friendly, floating/hovering, LED eyes
- **Source:** AI-Generated + custom touch-up
- **Variants:**
  - Tutorial Robot (Level 1) — biru, ramah
  - Quest Robot (Level 2-5) — oranye, informative
  - Shop Robot (optional) — hijau
- **Animations:** Idle hover, Talk (mouth LED blink), Point direction, Celebrate

### Enemies (Malware)
| Enemy | Visual | Behavior | Level |
|-------|--------|----------|-------|
| **Virus** | Bola berduri, merah, kecil | Chase player, swarm | 2+ |
| **Trojan** | Kuda trojan mini, armor | Patrol area, ambush | 2+ |
| **Worm** | Ular digital, panjang | Tunnel underground, pop-up | 3+ |
| **Spyware** | Mata terbang, transparan | Stalk player, steal items | 3+ |
| **Ransomware** | Boss, besar, teks merah | Multi-phase attack | 5 |
| **Trojan King** | Boss, armor tebal | Area control, summon minions | 3 |

---

## 4. Level Design (1-5)

### Level 1: Tutorial — "Inside the Machine"
**Objective:** Learn controls + grab 5 basic components
**Environment:** Digital floor grid, floating platforms, circuit board walls
**Layout:**
```
[Start] -> [Robot NPC] -> [Walking Tutorial Area]
    -> [Jump Tutorial — floating platforms]
    -> [Grab Tutorial — 5 items visible di meja/rak/lantai, press G untuk grab]
    -> [Interact Tutorial — activate terminal]
    -> [Exit Portal]
```
**Items to grab:** RAM Stick, VGA Card, CPU Chip, Casing Panel, Power Cable
**Enemies:** None
**Puzzles:** Simple platforming, items terlihat jelas di environment

### Level 2: First Threat — "Virus Alert"
**Objective:** Grab 7 components while avoiding/fighting malware
**Environment:** Digital cityscape, neon lights, data streams
**Layout:**
```
[Robot NPC — quest briefing] -> [City Area 1 — safe zone]
    -> [Virus Swarm Area — stealth/combat]
    -> [Puzzle Gate — arrange RAM sticks]
    -> [City Area 2 — Trojan patrol]
    -> [Boss Arena — Mini Trojan]
    -> [Exit Portal]
```
**Items:** 7 komponen tersebar visible di environment (meja, rak, lantai)
**Enemies:** Virus (swarm), Trojan (patrol)
**Puzzles:** Arrange components in correct order, timing-based stealth

### Level 3: Deep Web — "The Undernet"
**Objective:** Grab 10 components, defeat Trojan King
**Environment:** Dark web aesthetic, glitchy, corrupted textures
**Layout:**
```
[Robot NPC] -> [Corridor — Worm tunnels]
    -> [Puzzle Room 1 — redirect data flow]
    -> [Spyware Area — avoid detection]
    -> [Puzzle Room 2 — circuit completion]
    -> [Boss Arena — Trojan King]
    -> [Exit Portal]
```
**Enemies:** Worm, Spyware, Trojan King (boss)
**Puzzles:** Data flow redirection, circuit board puzzle

### Level 4: System Core — "The Kernel"
**Objective:** Grab 12 components, solve multi-step puzzles
**Environment:** Core system, massive CPU architecture, heat vents
**Layout:**
```
[Robot NPC] -> [Memory Banks — RAM puzzle]
    -> [GPU Zone — visual puzzle dengan rendering]
    -> [CPU Core — timing puzzle dengan clock cycles]
    -> [Multi-enemy arena]
    -> [Exit Portal]
```
**Enemies:** All types, increased difficulty
**Puzzles:** Multi-step, combining components, timing-based

### Level 5: Final Boss — "The Corruption"
**Objective:** Defeat Ransomware Lord, grab final components
**Environment:** Corrupted system, red/black, chaotic
**Layout:**
```
[Robot NPC — final briefing] -> [Gauntlet — all enemy types]
    -> [Puzzle Rush — timed puzzles]
    -> [Boss Arena — Ransomware Lord]
    -> [Victory Scene — WUGA returns to real world]
    -> [Epilogue — back in classroom, changed]
```
**Boss:** Ransomware Lord (multi-phase: shield phase, summon phase, attack phase)

---

## 5. Scene Architecture

### Unity Scenes
```
Assets/Scenes/
- MainMenu.unity              — Title screen, start game
- Prolog_Classroom.unity      — Ruangan kelas (cutscene + gameplay)
- Transition_Fall.unity       — Animasi jatuh ke dunia digital
- Level1_Tutorial.unity       — Tutorial level
- Level2_VirusAlert.unity     — Level 2
- Level3_Undernet.unity       — Level 3
- Level4_Kernel.unity         — Level 4
- Level5_Corruption.unity     — Level 5 + final boss
- UI_Overlay.unity            — Persistent UI (additive scene)
- TestSandbox.unity           — Testing ground
```

### Scene Flow
```
MainMenu -> Prolog_Classroom -> Transition_Fall -> Level1_Tutorial
    -> Level2_VirusAlert -> Level3_Undernet -> Level4_Kernel
    -> Level5_Corruption -> Ending_Cutscene -> MainMenu
```

### Scene Loading Strategy
- Use `SceneManager.LoadSceneAsync()` dengan loading screen
- Persistent `GameManager` object (DontDestroyOnLoad) untuk state
- Additive UI scene untuk HUD yang persist antar scene

---

## 6. Quest System

### Quest Types
1. **Grab Quest** — Grab X komponen di area (press G, play grab animation)
2. **Defeat Quest** — Kalahkan X enemy
3. **Puzzle Quest** — Selesaikan puzzle tertentu
4. **Boss Quest** — Kalahkan boss tertentu

### Grab Mechanic (Quest Core)
- **Tidak ada collectible item** (coins, powerups, dll) — DIHAPUS
- **Fokus ke komponen quest saja** (RAM, VGA, CPU, Casing, Power Cable)
- **Press G** untuk grab item (bukan auto-pickup / trigger)
- **Proximity check** — player harus dekat item (range ~2m) baru bisa grab
- **Animation varies by item size:**
  - Kecil (RAM, CPU Chip) → one-hand grab, ringan
  - Sedang (VGA Card) → two-hand grab
  - Besar (Casing Panel) → bend down + two-hand grab
- **Visible placement** — item terlihat jelas di atas meja, rak, lantai
- **Tidak ada hidden items** — tantangan = mencapai lokasi + hindari/melawan musuh

### Quest Data Structure
```csharp
[CreateAssetMenu(fileName = "NewQuest", menuName = "WUGA/Quest")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string questName;
    public string description;
    public QuestType type;
    public List<QuestObjective> objectives;
    public List<QuestReward> rewards;
    public QuestData nextQuest; // chain quest
}

[System.Serializable]
public class QuestObjective
{
    public string description;
    public ObjectiveType type; // Grab, Defeat, Reach, Interact
    public string targetId; // item ID / enemy ID
    public int requiredAmount;
    public int currentAmount;
}

[System.Serializable]
public class QuestReward
{
    public RewardType type; // Unlock, Story
    public string rewardId;
    public int amount;
}
```

### Quest Flow
```
1. Player approaches Robot NPC
2. Tekan E untuk talk → NPC shows quest dialog (UI popup)
3. Player accepts quest
4. Quest objectives appear in HUD (e.g., "Grab RAM: 0/3")
5. Player navigate ke lokasi komponen, hindari/melawan musuh
6. Tekan G saat dekat komponen → grab animation → item masuk inventory
7. Semua komponen tergrab → quest complete
8. Kembali ke NPC (atau auto-complete untuk area quests)
9. Reward granted → next quest unlocks
```

### Quest Manager
```csharp
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    public List<QuestData> allQuests;
    public QuestData activeQuest;
    public List<QuestData> completedQuests;
    
    public void AcceptQuest(QuestData quest) { ... }
    public void UpdateObjective(ObjectiveType type, string targetId, int amount) { ... }
    public void CompleteQuest(QuestData quest) { ... }
    public bool IsQuestComplete(QuestData quest) { ... }
}
```

---

## 7. Combat System

### Melee Combat Mechanics
- **Light Attack:** Quick punch/swing (0.3s cooldown)
- **Heavy Attack:** Slower, more damage (0.8s cooldown)
- **Combo:** 3-hit combo (Light -> Light -> Heavy)
- **Dodge:** Quick dash (0.5s invincibility frames)
- **Block/Parry:** Timing-based damage reduction

### Combat Data
```csharp
public class CombatSystem : MonoBehaviour
{
    public int lightDamage = 10;
    public int heavyDamage = 25;
    public float attackRange = 2f;
    public float comboWindow = 0.5f;
    public LayerMask enemyLayer;
    
    public void LightAttack() { ... }
    public void HeavyAttack() { ... }
    public void Dodge() { ... }
    public void CheckCombo() { ... }
}
```

### Damage System
```csharp
public class HealthSystem : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public bool isInvincible;
    
    public void TakeDamage(int damage) { ... }
    public void Heal(int amount) { ... }
    public void Die() { ... }
}
```

### Weapon System (Optional)
- Komponen yang dikumpulkan bisa jadi senjata
- RAM -> Shield (block)
- CPU -> Speed boost
- VGA -> Visual attack (ranged)

---

## 8. Enemy AI Design

### AI Architecture (Finite State Machine)
```
Enemy States:
- Idle — berdiam, tidak aware player
- Patrol — jalan rute tetap
- Chase — kejar player saat terdeteksi
- Attack — serang player saat dalam range
- Flee — kabur saat HP rendah (beberapa enemy)
- Stun — terkena serangan, tidak bergerak
- Dead — mati, play death anim, destroy
```

### Virus AI
```csharp
public class VirusAI : MonoBehaviour
{
    public float detectionRange = 10f;
    public float attackRange = 1.5f;
    public float moveSpeed = 4f;
    public int damage = 5;
    
    enum State { Idle, Chase, Attack, Dead }
    State currentState = State.Idle;
    
    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                if (PlayerInRange(detectionRange))
                    currentState = State.Chase;
                break;
            case State.Chase:
                MoveTowards(player.position);
                if (PlayerInRange(attackRange))
                    currentState = State.Attack;
                break;
            case State.Attack:
                AttackPlayer();
                break;
        }
    }
}
```

### Trojan AI
```csharp
public class TrojanAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float chargeSpeed = 8f;
    public float chargeRange = 5f;
    
    // States: Patrol -> Alert -> Charge -> Attack -> Patrol
}
```

### Worm AI
```csharp
public class WormAI : MonoBehaviour
{
    public float tunnelDuration = 3f;
    public float surfaceDuration = 2f;
    
    // States: Tunneling -> Surface -> Attack -> Tunneling
}
```

### Enemy Spawner
```csharp
public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public float spawnInterval = 5f;
    public int maxEnemies = 10;
    
    void SpawnEnemy() { ... }
}
```

---

## 9. NPC Robot System

### Robot Behavior
```csharp
public class RobotNPC : MonoBehaviour
{
    public QuestData[] availableQuests;
    public DialogData[] dialogLines;
    public float hoverHeight = 1f;
    public float hoverSpeed = 2f;
    
    void Update()
    {
        transform.position += Vector3.up * Mathf.Sin(Time.time * hoverSpeed) * 0.01f;
    }
    
    public void OnInteract()
    {
        if (HasAvailableQuest())
            ShowQuestDialog();
        else
            ShowDefaultDialog();
    }
}
```

### Dialog System
```csharp
[CreateAssetMenu(fileName = "NewDialog", menuName = "WUGA/Dialog")]
public class DialogData : ScriptableObject
{
    public string speakerName;
    [TextArea(3, 10)]
    public string[] lines;
    public DialogChoice[] choices;
}

[System.Serializable]
public class DialogChoice
{
    public string choiceText;
    public int nextLineIndex;
    public QuestData questToGive;
}
```

### Robot Visual States
- **Idle:** Hovering, LED eyes dim
- **Talking:** LED eyes bright, mouth LED blink
- **Quest Available:** Exclamation mark above head
- **Quest Complete:** Checkmark above head
- **Alert:** Red LED, pointing direction

---

## 10. Item Grab System (Komponen Quest)

### Design Philosophy
- **Tidak ada collectible item** (coins, powerups, dll) — fokus ke komponen quest saja
- **Grab mechanic** — player harus press G untuk mengambil item, bukan auto-pickup
- **Animasi varies by item size** — memberi feel berbeda tiap komponen
- **Visible placement** — semua item terlihat jelas di environment, tidak ada hidden items
- **Tantangan = mencapai lokasi** — platforming, enemies, puzzles menghalangi jalan

### Komponen yang Bisa Di-grab
| Komponen | Size | Grab Animation | Level |
|----------|------|----------------|-------|
| RAM Stick | Kecil | One-hand grab (ringan) | 1+ |
| CPU Chip | Kecil | One-hand grab (ringan) | 1+ |
| Power Cable | Kecil | One-hand grab (gulung kabel) | 1+ |
| VGA Card | Sedang | Two-hand grab | 1+ |
| Casing Panel | Besar | Bend down + two-hand grab | 1+ |
| SSD/HDD | Sedang | Two-hand grab | 2+ |
| Motherboard | Besar | Bend down + two-hand grab | 3+ |
| PSU | Besar | Bend down + two-hand grab | 4+ |
| GPU (High-End) | Sedang | Two-hand grab + kagum | 5 |

### Item ScriptableObject
```csharp
[CreateAssetMenu(fileName = "NewItem", menuName = "WUGA/Item")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public Sprite icon;
    public ItemSize size; // Small, Medium, Large
    public string description;
    public GameObject worldPrefab; // 3D model di world
    public AnimationClip grabAnimation; // animasi grab spesifik
}
```

### GrabInteraction Script
```csharp
public class GrabInteraction : MonoBehaviour
{
    public ItemData itemData;
    public float grabRange = 2f;
    public LayerMask playerLayer;
    public GameObject interactionPrompt; // "Press G to grab" UI

    private bool playerInRange = false;
    private bool isGrabbed = false;

    void Update()
    {
        if (isGrabbed) return;

        // Check player proximity
        playerInRange = CheckPlayerProximity();
        
        // Show/hide interaction prompt
        if (interactionPrompt != null)
            interactionPrompt.SetActive(playerInRange);

        // Handle grab input
        if (playerInRange && Input.GetKeyDown(KeyCode.G))
        {
            StartGrab();
        }
    }

    void StartGrab()
    {
        isGrabbed = true;
        
        // Hide prompt
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // Notify PlayerAnimation to play grab animation
        PlayerAnimation playerAnim = FindPlayerAnimation();
        if (playerAnim != null)
        {
            playerAnim.PlayGrabAnimation(itemData.size, OnGrabComplete);
        }
    }

    void OnGrabComplete()
    {
        // Add item to inventory
        InventoryManager.Instance.AddItem(itemData);
        
        // Update quest objective
        QuestManager.Instance.UpdateObjective(
            ObjectiveType.Grab, 
            itemData.itemId, 
            1
        );

        // Play VFX
        PlayGrabVFX();

        // Destroy item from world
        Destroy(gameObject);
    }

    bool CheckPlayerProximity()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position, grabRange, playerLayer
        );
        return colliders.Length > 0;
    }
}
```

### Placement Guidelines
- **Meja/Rak:** Item ditempatkan di atas surface, sejajar mata player
- **Lantai:** Item ditempatkan di floor, player harus approach
- **Platform:** Item di floating platform, harus loncat dulu
- **Near Enemies:** Item dekat patrol route musuh, harus timing grab
- **Post-Puzzle:** Item muncul setelah puzzle selesai

### Item VFX
- **Idle:** Subtle glow ring di base item
- **In Range:** Glow lebih terang + particle sparkle
- **Grabbing:** Item terangkat ke tangan player
- **Grabbed:** Flash effect + item masuk inventory (UI animation)

### Inventory System (Simplified)
```csharp
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public List<ItemData> grabbedItems = new List<ItemData>();
    
    public void AddItem(ItemData item)
    {
        grabbedItems.Add(item);
        // Update UI
        UIManager.Instance.UpdateInventoryUI();
    }
    
    public bool HasItem(string itemId)
    {
        return grabbedItems.Exists(i => i.itemId == itemId);
    }
    
    public int GetItemCount(string itemId)
    {
        return grabbedItems.Count(i => i.itemId == itemId);
    }
}
```

---

## 11. Animation Plan

### Animation Controller Structure

#### WUGA Animator Controller
```
States:
- Idle (default)
- Walk (blend tree: Forward, Backward, Left, Right)
- Run (blend tree)
- Jump_Start -> Jump_Loop -> Jump_Land
- Attack_Light -> Attack_Heavy
- Combo_1 -> Combo_2 -> Combo_3
- Dodge
- Grab_Small (one-hand grab untuk RAM, CPU, Cable)
- Grab_Medium (two-hand grab untuk VGA, SSD)
- Grab_Large (bend down + two-hand grab untuk Casing, Motherboard, PSU)
- Interact (press E untuk talk/activate)
- Hit_Reaction
- Death
- Fall_Transition (prolog only)

Transitions:
- Idle <-> Walk (parameter: Speed)
- Walk <-> Run (parameter: Sprint)
- Any -> Jump_Start (parameter: IsJumping)
- Idle/Walk -> Attack_Light (parameter: Attack)
- Attack_Light -> Combo_2 (parameter: Combo, exit time)
- Any -> Dodge (parameter: Dodge)
- Any -> Grab_Small (trigger: GrabSmall)
- Any -> Grab_Medium (trigger: GrabMedium)
- Any -> Grab_Large (trigger: GrabLarge)
- Any -> Interact (trigger: Interact)
- Any -> Hit_Reaction (trigger: Hit)
- Any -> Death (trigger: Die)
```

### Grab Animation Details
- **Grab_Small (0.5s):** Reach forward with one hand, grab, pull back to body
- **Grab_Medium (0.8s):** Reach forward with two hands, grab sides, lift to chest
- **Grab_Large (1.2s):** Bend down, two hands grip bottom, lift heavy item to chest, stand up
- Semua grab animation: player tidak bisa move/attack selama animasi berlangsung

#### Robot NPC Animator Controller
```
States:
- Hover_Idle
- Hover_Talk
- Point_Direction
- Celebrate
- Alert

Transitions:
- Idle <-> Talk (parameter: IsTalking)
- Any -> Point (trigger: Point)
- Any -> Celebrate (trigger: QuestComplete)
```

#### Enemy Animator Controllers (per enemy type)
```
Virus: Idle, Chase, Attack, Die
Trojan: Patrol, Alert, Charge, Attack, Die
Worm: Tunnel, Surface, Attack, Die
```

### Animation Sources
1. **Mixamo** (free) — base humanoid animations (walk, run, jump, attack)
2. **AI-Generated** — custom animations for specific actions
3. **Custom** — simple animations di Unity Animator (item bob, robot hover)

---

## 12. UI System

### UI Screens
```
1. Main Menu
   - Title
   - Start Game
   - Settings
   - Quit

2. HUD (In-Game)
   - Health Bar (top-left)
   - Quest Tracker (top-right)
   - **Grab Prompt** (center) — "Press G to grab [Item Name]" saat dekat item
   - Dialog Box (bottom-center)
   - Interaction Prompt (center) — "Press E to talk" saat dekat NPC
   - Minimap (top-right corner, optional)

3. Pause Menu
   - Resume
   - Settings
   - Inventory (full)
   - Quest Log
   - Main Menu

4. Dialog System
   - Speaker name
   - Dialog text
   - Choice buttons (if branching)
   - Continue/Close button

5. Quest Complete Popup
   - Quest name
   - Rewards
   - Continue button

6. Loading Screen
   - Level name
   - Progress bar
   - Tips
```

### UI Implementation (uGUI)
- Use **uGUI** (Canvas-based) untuk HUD
- Responsive scaling dengan Canvas Scaler

---

## 13. Shader & VFX Plan

### Custom Shaders (Shader Graph)

#### 1. Digital Grid Floor
- Grid pattern bergerak (Tron-like)
- URP Unlit + Custom
- Grid lines (UV-based), scroll animation, glow effect
- Color: Cyan/Blue

#### 2. Glitch Effect
- Distorsi layar, chromatic aberration
- Fullscreen Shader Graph
- UV displacement (noise-based), color channel split, scanlines
- Triggered pada transisi / boss attack

#### 3. Data Stream Effect
- Aliran data seperti matrix rain
- Particle Shader / Billboard
- Falling characters, color fade, random speed

#### 4. Hologram Effect
- Untuk NPC Robot dan item langka
- URP Lit + Fresnel
- Fresnel glow, scanlines, transparency, flicker

#### 5. Corruption Effect
- Untuk enemy dan area corrupted
- URP Lit + Dissolve
- Noise-based dissolve, edge glow (red/orange)

### VFX (Particle Systems)
- **Item Grab:** Sparkle + light burst saat grab berhasil
- **Attack Hit:** Impact sparks
- **Enemy Death:** Digital disintegration
- **Portal Effect:** Swirling energy ring
- **Level Transition:** Screen wipe / glitch
- **Boss Aura:** Pulsing energy field

---

## 14. Technical Architecture

### Script Architecture
```
Assets/
- Script/
  - Player/
    - PlayerController.cs
    - PlayerCombat.cs
    - PlayerHealth.cs
    - PlayerAnimation.cs
  - Enemy/
    - EnemyBase.cs
    - VirusAI.cs
    - TrojanAI.cs
    - WormAI.cs
    - EnemySpawner.cs
    - BossAI.cs
  - NPC/
    - RobotNPC.cs
    - DialogSystem.cs
    - QuestGiver.cs
  - Quest/
    - QuestManager.cs
    - QuestData.cs
    - QuestObjective.cs
    - QuestUI.cs
  - Items/
    - ItemData.cs                    — Item ScriptableObject (komponen quest)
    - GrabInteraction.cs             — Press G grab mechanic + proximity check
    - InventoryManager.cs            — Track grabbed items
    - InventoryUI.cs                 — Inventory display (simplified)
  - Combat/
    - HealthSystem.cs
    - DamageDealer.cs
    - HitEffect.cs
  - System/
    - GameManager.cs
    - SceneLoader.cs
    - SaveSystem.cs (future)
    - AudioManager.cs (future)
    - UIManager.cs
  - Camera/
    - CameraController.cs
    - CameraEffects.cs
  - Puzzle/
    - PuzzleBase.cs
    - CollectPuzzle.cs
    - ArrangePuzzle.cs
    - CircuitPuzzle.cs
    - TimingPuzzle.cs
- ScriptableObjects/
  - Quests/
  - Items/
  - Dialogs/
  - EnemyData/
- Prefabs/
  - Player/
  - Enemies/
  - NPC/
  - Items/
  - Environment/
  - VFX/
  - UI/
- Materials/
- Shaders/
- Animations/
- Audio/ (future)
- Scenes/
- Settings/
```

### Key Design Patterns
1. **Singleton** — GameManager, QuestManager, InventoryManager
2. **ScriptableObject** — Quest, Item, Dialog data (data-driven)
3. **State Machine** — Enemy AI, Player states
4. **Observer Pattern** — Quest events, UI updates
5. **Object Pooling** — Enemy spawning, VFX particles

### Packages Already Installed (No extra needed)
- `com.unity.cinemachine` 3.1.7 — Camera system
- `com.unity.ai.navigation` 2.0.10 — NavMesh for enemy pathfinding
- `com.unity.probuilder` 6.1.2 — Level blockout
- `com.unity.timeline` 1.8.10 — Cutscenes
- `com.unity.inputsystem` 1.18.0 — Input handling
- `com.unity.render-pipelines.universal` 17.3.0 — Rendering

---

## 15. Development Roadmap

### Phase 1: Foundation (Week 1-2)
**Goal:** Basic playable character + scene setup

- [ ] Setup folder structure
- [ ] Create Player prefab (WUGA)
  - [ ] Import/create character model (AI-generated)
  - [ ] Setup CharacterController
  - [ ] Refactor ThirdPersonMovement.cs -> PlayerController.cs
  - [ ] Add Cinemachine camera rig
  - [ ] Bind Input System actions
- [ ] Create basic TestSandbox scene
- [ ] Setup GameManager (DontDestroyOnLoad)

### Phase 2: Level 1 Blockout (Week 2-3)
**Goal:** Playable tutorial level

- [ ] Create Level1_Tutorial scene
- [ ] Blockout level with Probuilder
  - [ ] Walking area
  - [ ] Jump platforms
  - [ ] Item placement (meja, rak, lantai)
  - [ ] Exit portal
- [ ] Create digital grid shader
- [ ] Create basic environment materials
- [ ] Implement GrabInteraction.cs (press G, proximity check)
- [ ] Implement grab animations (Small, Medium, Large)
- [ ] Create 5 basic item prefabs (RAM, VGA, CPU, Casing, Cable)

### Phase 3: NPC & Quest System (Week 3-4)
**Goal:** Robot NPC + quest mechanics

- [ ] Create Robot NPC model/animation
- [ ] Implement DialogSystem.cs
- [ ] Implement QuestManager.cs
- [ ] Create QuestData ScriptableObjects for Level 1
- [ ] Create interaction system (press E to talk to NPC)
- [ ] Create grab prompt UI ("Press G to grab [Item]")
- [ ] Create quest UI (tracker, popup)

### Phase 4: Combat System (Week 4-5)
**Goal:** Melee combat + enemy AI

- [ ] Implement PlayerCombat.cs (light, heavy, combo)
- [ ] Implement HealthSystem.cs
- [ ] Create Virus enemy prefab + AI
- [ ] Create Trojan enemy prefab + AI
- [ ] Implement enemy spawner
- [ ] Create hit effects VFX
- [ ] Test combat in sandbox

### Phase 5: Level 2 + Enemies (Week 5-6)
**Goal:** First real level with combat

- [ ] Create Level2_VirusAlert scene
- [ ] Blockout level with Probuilder
- [ ] Place enemies + spawners
- [ ] Create quest data for Level 2
- [ ] Implement stealth/detection mechanics
- [ ] Add more item types

### Phase 6: Levels 3-5 (Week 6-8)
**Goal:** Complete all levels

- [ ] Level 3: Undernet
  - [ ] Worm + Spyware AI
  - [ ] Trojan King boss
  - [ ] Advanced puzzles
- [ ] Level 4: Kernel
  - [ ] Multi-step puzzles
  - [ ] All enemy types
- [ ] Level 5: Corruption
  - [ ] Ransomware Lord boss (multi-phase)
  - [ ] Final gauntlet
  - [ ] Ending sequence

### Phase 7: Prolog & Transitions (Week 8-9)
**Goal:** Story scenes + polish

- [ ] Create Prolog_Classroom scene
- [ ] Implement transition animation (fall)
- [ ] Create glitch shader for transition
- [ ] Add Timeline cutscenes
- [ ] Create MainMenu scene
- [ ] Implement scene loading system

### Phase 8: Polish & VFX (Week 9-10)
**Goal:** Visual polish + effects

- [ ] All custom shaders (DigitalGrid, Glitch, Hologram, Dissolve)
- [ ] Particle effects (pickup, hit, death, portal)
- [ ] Post-processing (bloom, vignette, chromatic aberration)
- [ ] UI polish (animations, transitions)
- [ ] Bug fixes + optimization

---

## Current Status
- [x] Project setup (Unity 6, URP, packages)
- [x] Basic ThirdPersonMovement.cs
- [x] InputSystem_Actions configured
- [x] Cinemachine camera binding (Look action)
- [ ] Everything else — starting from Phase 1

---

## Notes for Next Session
- Start with Phase 1: Foundation
- Import/create WUGA character model first
- Refactor ThirdPersonMovement.cs into proper PlayerController.cs
- Create folder structure as defined above
- User's friend has additional visual ideas (not yet shared)
- **Grab mechanic:** Press G, animation varies by item size, no collectible items
- **Focus:** Komponen quest only (RAM, VGA, CPU, Casing, Cable, dll)
