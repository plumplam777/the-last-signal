"""
The Last Signal — loudness pass for all game audio (Phase 7 mix).

    python3 Tools/normalize_audio.py            # normalise everything in place
    python3 Tools/normalize_audio.py --report   # only print the levels

Each WAV is scaled to a target RMS level (dBFS); the gain is capped so peaks stay at or below -1 dBFS.
Idempotent: running it twice changes nothing. generate_voices.py and generate_sfx.py call it at the end,
so regenerated audio is always mixed the same way.

Mix logic (what the player hears = file level x in-game volume):
  * voices (CAIRN, logs, cutscene, endings; EN and BG alike) -17 dBFS (EN lands ~-18: peak-limited) — always the loudest bed
  * ambience loops -20 dBFS; the per-sector volumes in the builders (0.2–0.6) put them ~10 dB under the voices,
    and AudioManager ducks them further while someone speaks
  * effects: per-clip targets below (alarms loud, UI clicks soft, frequent sounds moderate)
"""
import audioop
import glob
import math
import os
import sys
import wave

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
AUDIO = os.path.join(ROOT, "Assets", "_Project", "Audio", "Resources")
PEAK_CEILING = -1.0
TOLERANCE = 0.3  # dB; closer than this counts as already normalised

VOICE_TARGET = -17.0
AMBIENCE_TARGET = -20.0
SFX_TARGETS = {
    "sfx_alarm": -13, "sfx_power_up": -15, "sfx_generator_start": -14, "sfx_lift": -16,
    "sfx_door_open": -16, "sfx_door_close": -16, "sfx_door_locked": -17, "sfx_docking_clamp": -16,
    "sfx_breaker": -17, "sfx_error": -18, "sfx_terminal_error": -19, "sfx_terminal_ok": -19,
    "sfx_terminal_open": -19, "sfx_terminal_key": -21, "sfx_pickup": -18, "sfx_objective": -18,
    "sfx_log_start": -22, "sfx_cairn": -20, "sfx_valve": -18, "sfx_crank": -19, "sfx_switch": -19,
    "sfx_fuse_insert": -18, "sfx_cable_plug": -18, "sfx_click": -22, "sfx_flashlight": -22, "sfx_ui": -24,
    "sfx_footstep_1": -21, "sfx_footstep_2": -21, "sfx_footstep_3": -21, "sfx_footstep_4": -21,
}
SFX_DEFAULT = -18.0


def db(v, full):
    return 20 * math.log10(v / full) if v > 0 else -120.0


def measure(path):
    with wave.open(path, "rb") as w:
        params = w.getparams()
        frames = w.readframes(w.getnframes())
    width = params.sampwidth
    mono = audioop.tomono(frames, width, 0.5, 0.5) if params.nchannels == 2 else frames
    full = 2 ** (8 * width - 1)
    return params, frames, db(audioop.rms(mono, width), full), db(audioop.max(frames, width), full)


def target_for(path):
    rel = os.path.relpath(path, AUDIO).replace(os.sep, "/")
    name = os.path.splitext(os.path.basename(path))[0]
    if rel.startswith("Voice/"):
        return VOICE_TARGET
    if rel.startswith("Ambience/"):
        return AMBIENCE_TARGET
    return float(SFX_TARGETS.get(name, SFX_DEFAULT))


def normalise(path, report_only=False):
    params, frames, rms, peak = measure(path)
    target = target_for(path)
    gain_db = min(target - rms, PEAK_CEILING - peak)
    if report_only or abs(gain_db) < TOLERANCE or rms < -90:
        return rms, peak, 0.0
    factor = 10 ** (gain_db / 20)
    scaled = audioop.mul(frames, params.sampwidth, factor)
    with wave.open(path, "wb") as w:
        w.setparams(params)
        w.writeframes(scaled)
    return rms, peak, gain_db


def files():
    for sub in ("Voice/en", "Voice/bg", "Ambience", "Sfx"):
        yield from sorted(glob.glob(os.path.join(AUDIO, sub, "*.wav")))


def main(report_only=False):
    changed = 0
    for path in files():
        rms, peak, gain = normalise(path, report_only)
        if gain:
            changed += 1
        if report_only or not path.split(os.sep)[-2] in ("en", "bg"):
            print(f"{os.path.relpath(path, AUDIO):42s} rms {rms:6.1f} peak {peak:6.1f} target {target_for(path):6.1f}"
                  + (f"  gain {gain:+5.1f} dB" if gain else ""))
    print(f"normalize_audio: {changed} file(s) changed")


if __name__ == "__main__":
    main("--report" in sys.argv)
