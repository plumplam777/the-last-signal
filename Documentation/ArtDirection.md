# Art Direction — "Brass & Signal"

References (in `ArtReference/`): **A** stylized brass/teal temple (`ref_stylized_brass_temple.webp`) — the look we love;
**B** dark sci-fi console room (`ref_scifi_console_room.png`) — the setting we need.
Games: **BioShock Infinite** (painterly, ornate, crafted retro-futurism — primary), **The Planet Crafter**
(readable stylised sci-fi modules — secondary, but toned down: its colours are too bright for us).

## The mix in one sentence
A sci-fi research station that was *built with craftsmanship*: dark painted-metal rooms (B) trimmed with
polished brass, teal crystal-glass and terracotta enamel panels (A), lit like a painting — warm amber pools
against cold teal shadow, with clean emissive signal lines running through it.

It also fits the story: the Murmur is a crystal mirror, so teal crystal is the station's "signature" material,
and the ornate brass reflects the crew's pride in Lantern Station — which now sits dark and abandoned.

## Shape language
- Chunky, slightly exaggerated forms with **big soft bevels** (no razor-sharp sci-fi edges). Readable silhouettes.
- Arches, domes and rings (A) reinterpreted as **airlock frames, ring-shaped door surrounds, domed ceilings, gyroscope-like machines**.
- Ornament is **functional**: brass ribs, rivet rows, meander-like trim bands, round gauges, ring handles.
- Props are few but characterful (like the armillary sphere in A): each sector gets one "hero machine".

## Palette
| Role | Colour | Hex | Use |
|---|---|---|---|
| Base dark | Deep navy | `#141B26` | walls, floors in shadow |
| Base mid | Charcoal slate | `#2A313B` | panels, consoles |
| Metal | Warm grey stone/metal | `#8A8F93` | structural frames |
| **Brass** | Polished brass | `#C9A14A` | trims, rings, handles, gauges |
| **Teal crystal** | Teal glass | `#3FB8B0` → `#7FE3D8` | glass, crystals, anomaly, hologram accents |
| **Terracotta** | Enamel terracotta | `#B8664A` | painted panels, sector colour bands, signage |
| Signal cyan | Emissive cyan | `#5FE6FF` | UI, powered strips, interactables |
| Warm light | Amber | `#FFB36B` | practical lamps, safe areas |
| Danger | Emergency red | `#E0453A` | unpowered/alarm state only |

Saturation stays **medium** (painterly, not neon). Darks never go pure black — they lean navy.
Bright colour is a *reward*: restoring power turns red emergency gloom into amber + teal.

## Materials
- URP Lit, mostly **flat/hand-painted albedo with soft gradients** (A), low-to-mid smoothness; brass and glass are the only shiny things.
- Subtle edge-wear on brass, soft grime in corners; no photoreal noise textures.
- Emission only for signal lines, screens, crystals and lamps.

## Lighting & post
- Warm key / cool fill. Each room: 1–2 warm practicals, cool ambient, coloured accent.
- Unpowered: dim navy ambient + red emergency lights + flashlight. Powered: amber + teal.
- Fog for depth, floating dust, moderate bloom on emissives, ACES tonemapping, slight warm-teal split grade, soft vignette.

## Per-sector identity
| Sector | Accent | Hero machine |
|---|---|---|
| Docking Bay | Terracotta + amber, ring airlock | Ring-shaped docking clamp |
| Engineering | Brass + orange heat | Big brass generator with spinning governor |
| Medical Bay | Soft teal + white enamel | Stasis pods with glass domes |
| Research Lab | Teal crystal | Armillary-like containment rig around a crystal shard |
| Reactor & Command | Cold cyan core vs. red alarms | Gyroscopic reactor core; CAIRN's "eye" |

## UI
Thin brass-coloured frames, teal/cyan text on translucent navy panels, rounded corners, ring motifs
(CAIRN's eye). Font: geometric sans with full Cyrillic support (to be picked; candidates: Exo 2, Jura).

## Asset sourcing
Stylised modular kits (CC0/royalty-free, listed in `AssetRegister.md`) + custom Blender pieces for trims, rings,
hero machines. Everything is re-materialed to this palette so packs from different sources match.
