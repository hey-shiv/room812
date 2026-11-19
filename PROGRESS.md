# PROGRESS.md — "812" Project Handoff

> Read this first in any new chat. It is the single source of truth for what has been built,
> what was decided, and what remains. `COORDINATION.md` contains the chronological work log.

---

## 1. Original brief (from the master prompt)

**Title:** 812 — co-op psychological/paranormal investigation horror.
**Platform:** Mobile-first (Android/iOS), 60 FPS target on mid-range/flagship devices.
**Vision:** Surpass Phasmophobia on mobile with fluid movement, true 3D binaural audio,
tactile phone hardware integration, local offline ghost AI with memory, and visceral
environmental/player damage detail.

**Initial scope:**
- One multi-story college hostel: Room 812 focal point, cramped corridors, shared washrooms,
  warden office, rooftop/stairwells, bunk beds, study desks, flickering tube lights.
- LAN / Wi-Fi Hotspot co-op, 2–4 players, zero cloud servers.
- Ghost entity modeled from the attached "MOHINI" reference poster (gaunt, long hair, hollow
  glowing eyes, jagged grin, skeletal beckoning hand, black ragged shroud).

**Execution protocol originally required:** web-verify stack → read-only environment inspection →
phased plan → approval. All of this was completed and approved early on; see §3 and §6.

---

## 2. Environment & repository

- Workspace: `/Users/shivashant/projects/room812` (git repo, branch `main`, origin
  `git@github.com:hey-shiv/room812.git`).
- Unity project: `UnityProject/`, **Unity 6000.6.9f1 (6.6)**, **URP 17.6.0**.
  Quality levels: `Mobile` (index 0) and `PC` (index 1); Mobile_RPAsset: render scale 0.8,
  MSAA off, HDR off, additional-light shadows off.
- Blender source: `room812.blend` (Blender 5.3), collections `BLENDER_DEFAULTS`, `CHAR_Mohini`,
  `CHAR_Player`, `PROPS_Hostel`, `EXPORT_TMP`.
- Unity MCP via Pipeline package (port 7800). If tools are missing, run
  `unity open /Users/shivashant/projects/room812/UnityProject --until-ready --timeout 300`.
- Blender MCP connects on `localhost:9876` when Blender is running with the add-on.
- Unity CLI version 1.0.1.
- **Commit and push after every change batch** (user instruction). Working tree currently clean.

---

## 3. Verified tech decisions (do not re-litigate without evidence)

- **Audio:** Steam Audio 4.8.1 (Android + iOS, HRTF, occlusion, reverb). Installed manually from
  `.unitypackage` into `Assets/Plugins/SteamAudio` (`AssetDatabase.ImportPackage` no-op'd).
  Active spatializer: "Steam Audio Spatializer". Patched two null guards in
  `SteamAudioManager.ShutDown()`. Meta XR Audio rejected (no iOS); Resonance deprecated.
- **Networking:** Netcode for GameObjects **2.14.0** + Unity Transport **6.6.0** (docs quote
  3.2.1, registry latest here is 2.14.0). Host-authoritative UDP; custom UDP broadcast discovery
  on port 47777 plus direct-IP join; no cloud. Two-process join verified.
- **IK:** Animation Rigging 6.6.0 — 4 Two-Bone IK constraints + Multi-Aim head constraint.
- **Motion Matching:** JLPM22 package **removed** — it does not compile in player builds
  (`GizmosExtensions`, `FeatureDebug.Get3DValuePositionOrDirectionFeature`). User chose to keep
  the stable rig-matched Animator state machine instead. Do not reinstall without a fix.
- **Baked lighting:** attempted (settings asset + 219 meshes ContributeGI + Mixed/Baked lights),
  but Unity's bake API refused to start/produce lightmaps in this environment. Reverted to
  realtime + fog. `Assets/812/Settings/Lighting_Hostel.lighting` is retained for a retry.
- **Engine:** develop on 6.6; consider 6.3 LTS before shipping.

---

## 4. What is implemented (with file locations)

### Phase 1 — Whitebox
- `Assets/812/Scenes/Whitebox/Hostel_Whitebox.unity` — saved, in Build Settings, 3 floors +
  rooftop, Room 812, corridors, washrooms, warden office, stairs, furniture proxies.
- Layer 8 `EditorCutaway` on 84 objects (toggle off in Scene view Layers menu to see inside).
- NavMesh baked (legacy builder), 8 off-mesh links at vent/crawl markers.

### Phase 2 — Movement / Audio
- `Scripts/Core/HostelPlayerController.cs` — CharacterController, keyboard/mouse + touch + gyro
  lean, footstep NoiseBus events, haptics hooks.
- `Scripts/Core/PlayerAnimatorBridge.cs` + `Animators/AC_Player_Investigator.controller` —
  8 states (Idle/Walk/Run/CrouchWalk/TurnL/TurnR/StairsUp/StairsDown) from CHAT-B's clips.
- `Scripts/Core/MobilePerformanceBootstrap.cs` — 60 FPS, VSync off, Mobile quality on device.
- `Scripts/Audio/` — `AcousticZone.cs` (20 zones), `SteamAudioZoneDriver.cs`, `MicListener.cs`,
  `PlayerAudioFeedback.cs`, `AmbientAudioStarter.cs`.
- `Assets/812/Audio/` — 8 procedural WAV clips (footstep, door slam, tube flicker, ghost whisper,
  heartbeat, breaker, hostel drone, tube hum); 7 AudioSources, 4 SteamAudioSources.
- `Assets/Plugins/iOS/Room812CoreHaptics.mm` + `Scripts/Core/NativeHaptics.cs` — iOS Core Haptics,
  Android `Handheld.Vibrate`; heartbeat pulses + EMF spikes.

### Phase 3 — Ghost AI
- `Scripts/AI/GhostBlackboard.cs`, `GhostBrain.cs`, `HidingSpot.cs`, `GhostAnimatorBridge.cs`.
- Utility AI: Dormant/Patrol/Investigate/Hunt/Manifest/Retreat; sanity + Room-812 proximity drive
  aggression; hears NoiseBus and mic; remembers hiding spots and blood/wet decals.
- `Animators/AC_Mohini.controller` — 5 states from CHAT-B's clips, driven by `GhostState` int.

### Phase 4 — Polish / Damage
- `Scripts/FX/DecalSpawner.cs` (with `RecentMarks` for AI tracking), `DamageSystem.cs`,
  `BloodTrail.cs`, `WetSurface.cs`, `TubeLightFlicker.cs`, `PoltergeistPhysics.cs`,
  `PoltergeistEvents.cs`, `GlassShatter.cs`, `FrostCreeper.cs`.
- `Scripts/Core/CharacterDamageMask.cs` — clothes redden with injuries.
- `Scripts/UI/DiegeticTablet.cs`, `FloorplanTracker.cs`, `PlayerVitalsUI.cs` — world-space tablet
  with journal, 3-floor floorplan + live player dot, sanity/HR readout.
- `Assets/812/Resources/Decals/` — 5 textured decal materials; equipment prefabs in
  `Prefabs/Gear/` (flashlight, EMF reader, tablet, smartwatch) attached to hand IK targets;
  detail props in `Prefabs/Props/PF_Detail_*`.
- URP horror volume: `Assets/812/Settings/Volume_Horror_Mobile.asset` (Kawase bloom, vignette,
  grade, film grain, ACES); post-processing enabled on camera.

### Phase 5 — LAN co-op
- `Scripts/Network/LanLobby.cs`, `LanDiscovery.cs`, `NetworkedInvestigator.cs`,
  `NetworkedGhost.cs`, `WalkieTalkie.cs`.
- PTT walkie-talkie on V over NGO custom named messages; no external voice server.

### Assets (from CHAT-B, all imported/configured)
- `Art/Characters/Ghost_Mohini/`: revised FBX, 2K textures, Humanoid avatar (explicit
  53-bone HumanDescription), 6 URP materials, 5 animation clips.
- `Art/Characters/Player/`: rigged investigator FBX, 8 locomotion clips.
- `Art/Environment/Hostel/`: props, gear, detail props, decal textures.
- `Prefabs/Characters/PF_Mohini.prefab`, `PF_Player_Investigator.prefab`.

---

## 5. Build status (all verified)

| Target | Result | Output |
|---|---|---|
| macOS release | Succeeded, 0 errors | `Builds/812_Final.app` |
| iOS IL2CPP | Succeeded, 0 errors | `Builds/812_iOS/` |
| Android APK | Succeeded | `Builds/812_Android.apk` |

Play Mode regression tests after every batch: zero console errors. Two-process LAN join verified
(`ConnectedClients.Count = 2`).

---

## 6. Remaining (deliberate or external)

1. **Baked GI lighting** — refused by the local Unity bake API; realtime + fog is the working
   fallback. Retry on another machine with `Lighting_Hostel.lighting` if desired.
2. **True Motion Matching** — intentionally replaced by the stable state-machine Animator.
   A compatible fork would need to be build-tested before adoption.
3. **Physical device tests** — need an iPhone/Android device + Xcode for final validation.

No code work is pending. Git is clean and pushed (`ee2e068`).

---

## 7. How to resume

1. `cd /Users/shivashant/projects/room812 && git pull` and read this file + `COORDINATION.md`.
2. Launch Unity: `unity open /Users/shivashant/projects/room812/UnityProject --until-ready`.
3. Open `Assets/812/Scenes/Whitebox/Hostel_Whitebox.unity`.
4. Use Unity MCP tools via `tools.unity.*` inside `execute`; verify a Play → Stop cycle and a
   player build after any change, then `git add -A && git commit && git push origin main`.

**Conventions:** `MAT_` prefixes for materials, `SM_`/`SK_` for meshes, `TEX_` for textures,
`PF_` for prefabs, `ANIM_` for clips; 1 Unity unit = 1 metre; forward -Z / up Y FBX exports;
mobile poly budgets (Mohini 4,121 tris, player 3,710 tris); ASTC 6x6 textures.
