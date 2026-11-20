# Performance pass (2026-11-20)

The user's M4 laptop ran hot. Changes, all reproducible from code:

| What | Before | After |
|---|---|---|
| Frame rate | uncapped (vSync off → 100+ fps; 120 Hz screen) | `FrameRateGovernor`: 60 fps playing/cutscene, 30 in menus/pause/terminals, 10 when the window is unfocused |
| Quality levels | "Mobile" + "PC" (everything high) | **Low / Medium / High** (`Last Signal/Setup/Build Quality Presets`), default **Medium**, button in Settings → "Graphics: …" (saved in PlayerPrefs) |
| Render scale | 1.0 | Low 0.7 · Medium 0.85 · High 1.0 |
| SSAO | on | High only |
| Shadows | 4 cascades, 50 m, 2048 maps, soft = high | Low: 1 cascade/18 m, no point-light shadows · Medium: 2 cascades/25 m, 1024 atlas, soft = low · High: 2 cascades/35 m, 2048, soft = medium |
| Opaque texture copy | every frame | off (no shader samples scene colour) |
| Shadow-casting point lights (powered) | Docking 3 (18 shadow maps — atlas overflow warning), Engineering 2 (12) | Docking 1 (6), Engineering 1 (6); test caps every sector at 2 |
| Audio import | Vorbis 100 %, decompressed on load (all 188 voice clips as PCM in RAM) | voices mono Vorbis 45 % compressed-in-memory · ambience streamed · SFX mono 60 % (`AudioImportRules`) |
| macOS build size | 134 MB | 136 MB — content is only ~35 MB; ~70 MB is the Unity engine as a universal (Intel + Apple Silicon) binary |

Static batching and the SRP batcher were already on; geometry is light (≤ 80 k triangles per sector).

## Render cost per scene (`Last Signal/Diagnostics/Report Render Cost` → `PerformanceReport.txt`)

Before:
```
scene | lights (all/active) | shadowed lights (all/active) | shadow maps (active at load) | renderers | static | triangles
Sector_DockingBay | 17/9 | 3/0 | 0 | 544 | 503 | 80148
Sector_Engineering | 13/7 | 2/0 | 0 | 226 | 135 | 21580
Sector_Medical | 10/6 | 2/1 | 6 | 174 | 137 | 58360
Sector_Research | 12/12 | 0/0 | 0 | 248 | 152 | 46476
Sector_ReactorCommand | 17/10 | 2/1 | 6 | 295 | 246 | 65464
```
After: Docking Bay and Engineering drop to 1 shadowed light each; the rest unchanged (see `PerformanceReport.txt`).

Editor frame timings are not reliable for this (the editor throttles when unfocused: GPU 11–26 ms in the
same scene), so the numbers above are counts, not milliseconds. To feel the difference, play a build on Medium and
compare with High in Settings.

Guards: `PerformanceBudgetTests` (shadow budget per sector, fps cap applied, 3 quality levels), `FrameRateGovernorTests`.
