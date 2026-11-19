# Asset Register

Every third-party or generated asset must be listed here with its licence before it is committed.

| Asset | Type | Source / author | Licence | Location in project | Notes |
|---|---|---|---|---|---|
| TMP Essential Resources (Liberation Sans font, shaders, EmojiOne sprites) | Font / shaders / sprites | Unity Technologies (com.unity.ugui) | Liberation Sans: SIL OFL 1.1; EmojiOne: CC-BY 4.0 (attribution file included) | `Assets/TextMesh Pro/` | Default UI font with Cyrillic support until the final font is chosen. |
| Docking Bay greybox geometry & materials | Generated | Project (DockingBayBuilder) | Own work | `Assets/_Project/Art/Materials`, `Scenes/Station` | Placeholder for final art. |
| Placeholder voices (CAIRN, logs, cutscene, endings; EN + BG) | Audio (WAV) | Generated with macOS text-to-speech by `Tools/generate_voices.py` (voices Daniel, Reed, Moira, Karen, Tessa, Samantha, Daria) | Apple system voices, used as placeholders for a non-commercial diploma project; to be replaced by AI voices or the developer's recordings | `Assets/_Project/Audio/Resources/Voice/` | Same file names → drop-in replacement. |
| Ambience loops and sound effects | Audio (WAV) | Procedurally synthesised by `Tools/generate_sfx.py` | Own work | `Assets/_Project/Audio/Resources/{Ambience,Sfx}/` | Deterministic; re-run to regenerate. |
| Hero props: cryo pod base + canopy, bridge workstation, captain's chair + console, valve wheel + body, patient terminal, decode console, desk, office chair, cabinet + safe shells, pipe run | 3D models (FBX) | Built procedurally in Blender 5.1 by `Tools/blender/build_props.py` (headless, no addon, no AI generation) | Own work | `Assets/_Project/Art/Models/Props/` | Material slots remapped to station materials by `PropModels.cs`; regenerate with the command in the script header. |
