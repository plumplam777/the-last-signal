# The Last Signal — Live demo script (diploma defence)

**Length:** 12–15 minutes · **Build:** `Builds/macOS/TheLastSignal.app` (or the Unity Editor as a backup) ·
**Quality:** Medium (default) · **Languages:** EN + BG.

The demo shows: start screen → accounts and the admin panel → opening cutscene → one puzzle in every sector →
CAIRN and trust → one ending, plus switching EN ↔ BG and saving/loading. Every step has a fallback (§5).

> Puzzle solutions are in §6 (spoilers). Keep this page open on a second screen or on paper.

---

## 1. Before the defence (prepare the day before, takes ~40 min)

All player data lives in `~/Library/Application Support/LastSignal/The Last Signal/`:
`lastsignal.db` (SQLite: accounts, catalogues, progress summary) and `Saves/<UserId>/autosave.json` (the run).

1. **Fresh build.** Run `Last Signal/Build/macOS (clean)` (or use the current `Builds/macOS/TheLastSignal.app`) and
   play it for one minute. After building, check that the Cyrillic font asset did not change (`git diff --stat`, see HANDOFF §3).
2. **Administrator.** Start the game → Settings → *Administrator*. On a database without an admin, this page asks you
   to **create the first administrator** — create `admin` (password ≥ 6 characters). Log out again.
3. **Checkpoint accounts** (so the later sectors can be shown without playing the whole game live):
   1. Create a rehearsal account (`noa`) with *New game* and play through (walkthrough §6).
   2. Each time you arrive in **Medical**, **Research** and **Reactor + Command**, press Esc → *Save game*, and
      copy the save out of the game folder (game can stay open):
      ```bash
      cd ~/Library/Application\ Support/LastSignal/The\ Last\ Signal && mkdir -p ~/DemoSaves && cp Saves/<noa's id>/autosave.json ~/DemoSaves/medical.json
      ```
      (same for `research.json`, `reactor.json`; `<noa's id>` = the folder name under `Saves/`.)
   3. Create three more accounts with *New game*: `demo_med`, `demo_res`, `demo_core` (skip the cutscene, Esc →
      *Main menu*, *Log out*). Their ids are shown in the admin panel (Users tab) or with
      `sqlite3 lastsignal.db "SELECT UserId, Username FROM Users;"`.
   4. **Quit the game**, then copy each prepared save into its account (`autosave.json` is read for whoever is logged
      in; the user id inside the file is rewritten on the next save):
      ```bash
      cd ~/Library/Application\ Support/LastSignal/The\ Last\ Signal && mkdir -p Saves/<demo_med id> && cp ~/DemoSaves/medical.json Saves/<demo_med id>/autosave.json
      ```
      Repeat for `demo_res` (research.json) and `demo_core` (reactor.json). Test each with *Load game → Continue*.
4. **Make a backup** of the whole folder (`cp -R` to `~/DemoSaves/AppSupport_backup`). If anything goes wrong during
   the demo you can restore it between steps.
5. Set the system volume so voices are clearly audible, close other apps (keeps the laptop cool), turn off notifications.
6. Leave the game on the **start screen in English** before you begin.

## 2. Timeline overview

| Time | Part | What the committee sees |
|---|---|---|
| 0:00 – 1:00 | Start screen + EN/BG | Menu over the orbiting station, language switch |
| 1:00 – 3:00 | Admin panel | Admin login, CRUD on Users/Sectors/Items, Progress tab |
| 3:00 – 4:00 | New account + cutscene | Registration rules, opening cutscene "The Call" |
| 4:00 – 7:00 | Docking Bay | Interaction, inventory, fuse puzzle, power on, first CAIRN conversation (trust) |
| 7:00 – 8:30 | Save / load | Manual save, back to menu, *Load game → Continue* |
| 8:30 – 9:30 | Engineering | Generator start order + power routing (Array feed temptation) |
| 9:30 – 10:30 | Medical | Wipe Pod 3 → PIN 0417 |
| 10:30 – 11:30 | Research | Trial 7 symbol decode (mirror rule) |
| 11:30 – 13:30 | Reactor + Command | Valve balance → lift → captain's console → decision → Ending B |
| 13:30 – 15:00 | Wrap-up | Admin panel Progress tab shows the recorded decision; questions |

If time is short, skip **Engineering** (§3.6) and the Ending epilogue text (§3.10).

## 3. Script

### 3.1 Start screen (0:00)
- *Say:* "The Last Signal is a first-person narrative puzzle game in Unity 6, URP. You are Noa Varden, answering a
  distress call from Lantern Station above the ice world Nereth. The only voice on the station is its AI, CAIRN."
- Point at the four buttons: **New game / Load game / Settings / Quit**. The station orbits slowly behind the menu.
- Click the **language button** (top right) → the whole menu switches to Bulgarian; click again → English.
  *Say:* "All player text is in two string tables, `en.json` and `bg.json`; voices exist in both languages."
- Open **Settings**: mouse sensitivity, invert Y, **Graphics** (Low / Medium / High), language. Back.

### 3.2 Administrator and database (1:00)
- Settings → **Administrator** (small link at the bottom) → log in as `admin` → **Admin panel**.
- *Say:* "Accounts are stored in SQLite. Passwords are hashed with PBKDF2-SHA256, 60 000 iterations, random salt per
  user. Only an administrator can open this panel, and every action checks the role again."
- **Users** tab: the accounts created for the demo. Select `demo_med` → show the role switch. Mention the rules: the
  last administrator cannot be deleted or demoted, you cannot delete yourself.
- **Sectors** tab: *New* → id `test`, name `Test sector` → *Save* → it appears as "(custom)"; select it → *Delete*
  (click twice). Point out that built-in sectors cannot be deleted. (Optional: tick *Unlocked for everyone (demo override)* on a sector = demo
  override that opens its hatch for everyone.)
- **Items** tab: the item catalogue seeded from the game content.
- **Progress** tab: one summary row per player (sector, objective, logs, playtime, trust, decisions).
  *Say:* "The JSON save is the real game state; the database keeps a summary that is updated after every save."
- *Close* → *Log out* → *Back* to the main page.

### 3.3 New account and opening cutscene (3:00)
- **New game** → registration page. Show a validation error first: user name `ab` → error (3–20 letters, digits,
  `_ - .`), then create `jury` with a 6+ character password → **Create account and start**.
- The opening cutscene "The Call" starts (the Tern's cockpit, the distress call, the station reveal). Let it run
  ~40 seconds, then **hold Space** to skip (the ring at the bottom fills).

### 3.4 Docking Bay — tutorial puzzle and CAIRN (4:00)
- The objective panel (top left) says *Leave the airlock and find a light source.*
- Pick up the **flashlight** (E) → press **F** (battery bar bottom left).
- Find the **30A fuse** under the tarp by the crates → **Q** opens the inventory: Items tab, rotate the fuse in 3D
  with the mouse, then the Logs tab (x/14 audio logs). Close with Esc.
- **Fuse box** → insert the fuse → **breaker** → the lights come on (power `docking`).
  *Say:* "Nothing here is wired directly. The breaker writes one fact, `power.docking = true`, into WorldState. The
  lights, the door and the objective list all listen to that fact." (Sequence diagram in TechnicalArchitecture.md.)
- CAIRN speaks for the first time (AI_DOCK_01) — choose **2: "Glad to help. What do you need?"** (+5 trust).
  *Say:* "Trust goes from 0 to 100, starting at 50. It is never shown as a number — CAIRN's eye flashes teal or red.
  It changes what CAIRN says and the ending's epilogue, not which ending you get."
- Open the **inner door** → walk through the hatch to Engineering (do not wait for the second conversation if short on time).

### 3.5 Save and load (7:00)
- **Esc** → pause menu → **Save game** (status line confirms the save). Point out the language button here too: switch to **BG**,
  show the HUD objective and the pause menu in Bulgarian, switch back.
- **Main menu** → **Load game** → (still signed in as `jury`) → **Continue** → you are back in Engineering at the
  same spot with the same items and power state.
  *Say:* "Saves are JSON per user, written atomically through a temp file, with a .bak of the previous save; a
  corrupt file falls back to the backup. Checkpoints happen automatically on entering a sector and after each objective."

### 3.6 Engineering — generator and power routing (8:30, optional)
- Replace the burnt **coupling**, then the generator panel in Kaye's order **coolant → fuel → ignition → load**.
  A wrong order resets the sequence.
- **Routing board:** plug **Medical, Life support, Lighting** (three cables). If you plug **Array feed**, CAIRN asks
  you to leave it (AI_ENG_03: +10 trust "Okay. A little while." / −5 "No. The Medical door needs it.").
  *Say:* "This is the moral temptation of the sector: helping CAIRN costs you the Medical door."
- Solving the board sets `power.medical` — the Medical hatch now opens.

### 3.7 Medical — the patient record (9:30)
- Esc → Main menu → **Log out** → **Load game** → log in as **`demo_med`** → **Continue** (prepared save).
- **Wipe the frost off Stasis Pod 3** (CAIRN asks you not to — AI_MED_01) → the nameplate reads *RECORD 0417*.
- Patient terminal → keypad **0417** → read Jonah's record → **Release secured items** → take the keycard from the cabinet.

### 3.8 Research — Trial 7 (10:30)
- Log out → *Load game* as **`demo_res`** → **Continue**.
- Read the Response Trials board: the Murmur answers every row **mirrored**. Enter Trial 7's answer on the decode
  console: **■ ● ▲ ● ■ ◆** → Submit → the safe opens → core keycard.
- (A second wrong attempt makes CAIRN give a hint — mention it.)

### 3.9 Reactor and Command — the decision (11:30)
- Log out → *Load game* as **`demo_core`** → **Continue**.
- Reactor catwalk: set the three valves to **High / Mid / Low** (all different) → output and temperature gauges
  enter the green band → reactor stable → **lift** to Command.
- Command: play the **comms log** (the captain's last call), then sit at the **captain's console** → CAIRN's final
  conversation → the **decision screen**: A *Overload the reactor*, B *Lift the lockout*, C *Silence the Array*
  (C is locked unless all 14 logs were collected).
- Choose **B — Lift the lockout. Return the station to CAIRN.** → confirm ("cannot be undone").
  *Say:* "Ending A turns into a 120-second escape back to the ship; B is the shortest to show here."

### 3.10 Ending (12:30)
- The Ending scene plays CAIRN's lines over the station exterior (lights come back on), then the epilogue (high- or
  low-trust variant) and the credits, then returns to the main menu by itself.

### 3.11 Wrap-up (13:30)
- Settings → Administrator → log in as `admin` → Admin panel → **Progress** → select `demo_core`: the
  decisions `final_decision = restore` and `ai_trust_at_end = …` are listed.
- *Say:* "Everything in the world — rooms, puzzles, props, materials — is generated by editor scripts from code, and
  the 121 C# source files are backed by by 28 EditMode and 93 PlayMode tests, including full playthroughs of
  all three endings in both languages."

## 4. What to say if asked

| Question | Short answer | Where |
|---|---|---|
| How do systems talk? | Through **WorldState** facts + Conditions/Effects; no direct references | TechnicalArchitecture §1, §5 |
| Why both SQLite and JSON? | DB = accounts, catalogues, admin overview; JSON = resumable run | DatabaseSchema, TechnicalArchitecture §5.6 |
| Password security? | PBKDF2-HMAC-SHA256, 60 000 iterations, 16-byte salt, constant-time compare, same error for unknown user/wrong password | `AuthService.cs` |
| SQL injection? | Only parameterised statements (`sqlite3_bind_*`), no string-built SQL with user input | `Sqlite.cs` |
| How was the art made? | Code-built scenes, procedural painted textures, Blender props built by a Python script | ArtDirection.md, HANDOFF §3 |
| Performance? | 60 fps cap (30 in menus, 10 unfocused), Low/Medium/High presets, one shadowed lamp per room | Performance.md |

## 5. Fallbacks — if something goes wrong live

| Problem | Fix |
|---|---|
| A puzzle will not solve / you lose your way | Esc → **Admin tools** (only when logged in as an **admin** — start the run with the `admin` account if you want this safety net): *Restore power here*, *Give all items*, *Unlock all doors (this session)*, *Go to: Docking Bay / Engineering*, *Open final decision*. |
| A prepared save does not load ("save missing/corrupt") | Quit, restore the backup folder from §1.4, start again. Or use the `-startScene` launch below. |
| Need to jump straight into a sector | Terminal: `open Builds/macOS/TheLastSignal.app --args -startScene Sector_Medical` (also `Sector_DockingBay`, `Sector_Engineering`, `Sector_Research`, `Sector_ReactorCommand`, `Ending`). This starts a fresh run as a **guest** (no saving). |
| The build crashes or will not start | Open the project in Unity, open `Assets/_Project/Scenes/Bootstrap/Bootstrap.unity` (or any sector scene directly — GameServices creates itself) and press Play. |
| In the Editor: something is stuck | Menu **`Last Signal/Debug/`**: *Restore Sector Power*, *Give All Items*, *Open Decision Screen*, *Open First Terminal*, *Open Inventory*, *Play First Audio Log* (Play Mode only). |
| The Bulgarian text is blank | The Cyrillic fallback font asset was emptied by a build — switch back to EN and continue; fix later (HANDOFF §3). |
| Audio missing | Every voice line also has subtitles; continue. Check the system volume. |
| Laptop gets hot / low fps | Settings → Graphics → **Low**. |

## 6. Spoiler walkthrough (complete run, ~25 min)

- **Docking Bay:** flashlight → 30A fuse under the tarp → fuse box → breaker (power on) → inner door → hatch to Engineering.
- **Engineering:** coupling → generator **coolant → fuel → ignition → load** → routing board: **Medical, Life support,
  Lighting** (leave *Array feed* empty, or plug it for CAIRN at the cost of the Medical door).
- **Medical:** wipe Pod 3 → patient terminal PIN **0417** → *Release secured items* → research keycard.
- **Research:** Trial 7 = **■ ● ▲ ● ■ ◆** → safe → core keycard.
- **Reactor:** valves **High / Mid / Low** (all different) → lift → **Command:** comms log → captain's console → decision.
- **Endings:** A destroy (120 s escape to the Tern; at trust ≥ 50 CAIRN opens the service tunnel, below 50 use the
  manual crank), B restore, C silence (needs all 14 audio logs).
