#!/usr/bin/env python3
"""
Placeholder voice generator for The Last Signal (macOS text-to-speech, offline, free).

Reads the localization tables and writes 22.05 kHz mono WAVs to
Assets/_Project/Audio/Resources/Voice/<lang>/<key with dots as underscores>.wav
plus voice_manifest.json with per-line timings for audio logs, so subtitles stay in sync.

Bulgarian has a single system voice (Daria), so characters are told apart by speed and a
simple pitch shift (resampling). Captain Dunn's distress-call fragments get a crude radio
filter. Re-run any time the text changes:  python3 Tools/generate_voices.py
Replace with real recordings later: drop WAVs with the same names into the same folders.
"""
import json
import math
import os
import random
import re
import struct
import subprocess
import sys
import tempfile
import wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOC = os.path.join(ROOT, "Assets/_Project/Localization/Resources/Localization")
OUT = os.path.join(ROOT, "Assets/_Project/Audio/Resources/Voice")
RATE = 22050
LINE_GAP = 0.45  # seconds of silence between audio-log lines

# speaker -> per-language (voice, words per minute, pitch factor, radio filter)
CAST = {
    "cairn": {"en": ("Daniel", 158, 0.96, False), "bg": ("Daria", 150, 0.84, False)},
    "jonah": {"en": ("Reed (English (US))", 185, 1.0, False), "bg": ("Daria", 175, 0.9, False)},
    "rhee":  {"en": ("Moira", 165, 1.0, False), "bg": ("Daria", 160, 0.98, False)},
    "kaye":  {"en": ("Karen", 182, 1.0, False), "bg": ("Daria", 178, 1.05, False)},
    "dunn":  {"en": ("Tessa", 150, 1.0, True), "bg": ("Daria", 145, 0.95, True)},
    "noa":   {"en": ("Samantha", 172, 1.0, False), "bg": ("Daria", 168, 1.02, False)},
}

LOG_SPEAKERS = {
    "log_dock_01": "jonah", "log_dock_02": "rhee",
    "log_eng_01": "kaye", "log_eng_02": "kaye", "log_eng_03": "kaye",
}

CUTSCENE_SPEAKERS = {
    "cut.open.02": "dunn", "cut.open.03": "dunn", "cut.open.08": "dunn",
    "cut.open.04": "noa", "cut.open.07": "noa", "cut.open.09": "noa",
}


def clean(text):
    text = re.sub(r"<[^>]+>", "", text)            # rich-text tags
    text = re.sub(r"^\s*(RADIO|NOA|РАДИО|НОА)\s+", "", text)  # speaker label
    return text.replace("...", "… ").strip()


def say(text, voice, wpm):
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        path = tmp.name
    subprocess.run(["say", "-v", voice, "-r", str(wpm), "-o", path,
                    "--file-format=WAVE", f"--data-format=LEI16@{RATE}", text], check=True)
    with wave.open(path, "rb") as w:
        frames = w.readframes(w.getnframes())
    os.unlink(path)
    return list(struct.unpack(f"<{len(frames) // 2}h", frames))


def pitch(samples, factor):
    """factor < 1 lowers pitch (and slows down) by resampling."""
    if abs(factor - 1.0) < 1e-3 or not samples:
        return samples
    n = int(len(samples) / factor)
    out = []
    for i in range(n):
        x = i * factor
        j = int(x)
        f = x - j
        a = samples[j] if j < len(samples) else 0
        b = samples[j + 1] if j + 1 < len(samples) else a
        out.append(int(a + (b - a) * f))
    return out


def radio(samples):
    """Band-limited, slightly crushed and noisy: a weak transmission."""
    rnd = random.Random(7)
    out, lp, hp_prev_in, hp_prev_out = [], 0.0, 0.0, 0.0
    for s in samples:
        x = s / 32768.0
        # high-pass ~300 Hz
        hp = 0.92 * (hp_prev_out + x - hp_prev_in)
        hp_prev_in, hp_prev_out = x, hp
        # low-pass ~3 kHz
        lp += 0.55 * (hp - lp)
        y = math.tanh(lp * 2.2) * 0.7 + rnd.uniform(-0.025, 0.025)
        out.append(int(max(-1.0, min(1.0, y)) * 32767))
    return out


def write(path, samples):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(struct.pack(f"<{len(samples)}h", *[max(-32768, min(32767, s)) for s in samples]))


def render(text, speaker, lang):
    voice, wpm, factor, is_radio = CAST[speaker][lang]
    s = pitch(say(clean(text), voice, wpm), factor)
    return radio(s) if is_radio else s


def key_file(lang, key):
    return os.path.join(OUT, lang, key.replace(".", "_") + ".wav")


def main():
    only = set(sys.argv[1:])
    manifest = {"logs": {}}
    for lang in ("en", "bg"):
        table = json.load(open(os.path.join(LOC, f"{lang}.json"), encoding="utf-8"))
        jobs = []
        for key in table:
            if key.startswith("dlg.") and not re.search(r"\.c\d+$", key):
                jobs.append((key, "cairn"))
            elif key.startswith("end.") and key not in ("end.a.05", "end.b.04", "end.c.03", "end.c.06"):
                jobs.append((key, "dunn" if key == "end.a.03_low" else "cairn"))
            elif key in CUTSCENE_SPEAKERS:
                jobs.append((key, CUTSCENE_SPEAKERS[key]))
        for key, speaker in jobs:
            if only and key not in only:
                continue
            write(key_file(lang, key), render(table[key], speaker, lang))
            print(f"[{lang}] {key}")

        # audio logs: one clip per log, lines joined with short gaps; record line start times
        for log_id, speaker in LOG_SPEAKERS.items():
            if only and log_id not in only:
                continue
            n, all_samples, starts = 1, [], []
            gap = [0] * int(LINE_GAP * RATE)
            lead = [0] * int(0.6 * RATE)
            all_samples += lead
            while f"log.{log_id}.line.{n}" in table:
                starts.append(round(len(all_samples) / RATE, 3))
                all_samples += render(table[f"log.{log_id}.line.{n}"], speaker, lang) + gap
                n += 1
            write(os.path.join(OUT, lang, f"log_{log_id}.wav"), all_samples)
            manifest["logs"].setdefault(log_id, {})[lang] = {"starts": starts, "duration": round(len(all_samples) / RATE, 3)}
            print(f"[{lang}] {log_id}: {n - 1} lines, {len(all_samples) / RATE:.1f}s")

    if not only:
        with open(os.path.join(OUT, "voice_manifest.json"), "w", encoding="utf-8") as f:
            json.dump(manifest, f, indent=2)
    print("done")


if __name__ == "__main__":
    main()
