#!/usr/bin/env python3
"""
Procedural ambience and sound effects for The Last Signal (no downloads, no licences needed).

Writes 22.05 kHz mono WAVs to Assets/_Project/Audio/Resources/Sfx/ and .../Ambience/.
Ambience loops are made seamless by cross-fading their ends. Deterministic (fixed seeds), so
re-running produces identical files.  python3 Tools/generate_sfx.py
"""
import math
import os
import random
import struct
import wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets/_Project/Audio/Resources")
SR = 22050
TAU = 2 * math.pi


def write(name, samples, folder="Sfx", peak=0.85):
    m = max(1e-6, max(abs(s) for s in samples))
    k = peak / m
    path = os.path.join(OUT, folder, name + ".wav")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * k)) * 32767)) for s in samples))
    print(f"{folder}/{name}.wav  {len(samples) / SR:.2f}s")


def n(seconds):
    return int(seconds * SR)


class Noise:
    def __init__(self, seed):
        self.r = random.Random(seed)
        self.brown = 0.0
        self.lp = 0.0

    def white(self):
        return self.r.uniform(-1, 1)

    def brownish(self):
        self.brown = max(-1.0, min(1.0, self.brown + self.white() * 0.04)) * 0.998
        return self.brown

    def lowpassed(self, a):
        self.lp += a * (self.white() - self.lp)
        return self.lp


def env(i, total, attack, release):
    t = i / SR
    T = total / SR
    if t < attack:
        return t / attack
    if t > T - release:
        return max(0.0, (T - t) / release)
    return 1.0


def decay(i, tau):
    return math.exp(-(i / SR) / tau)


def loopify(samples, fade_seconds=1.5):
    """Cross-fade the end into the start so the clip loops without a click."""
    f = n(fade_seconds)
    body = samples[:-f]
    tail = samples[-f:]
    for i in range(f):
        k = i / f
        body[i] = body[i] * k + tail[i] * (1 - k)
    return body


# ------------------------------------------------------------------ ambience

def station_hum():
    nz, out = Noise(1), []
    for i in range(n(14)):
        t = i / SR
        wob = 1 + 0.15 * math.sin(TAU * 0.13 * t)
        s = (math.sin(TAU * 50 * t) * 0.5 + math.sin(TAU * 100 * t) * 0.25 + math.sin(TAU * 150.4 * t) * 0.1) * wob
        s += nz.brownish() * 0.9
        out.append(s)
    return loopify(out)


def ventilation():
    nz, out = Noise(2), []
    for i in range(n(14)):
        t = i / SR
        gust = 0.75 + 0.25 * math.sin(TAU * 0.09 * t) * math.sin(TAU * 0.31 * t + 1)
        out.append(nz.lowpassed(0.08) * gust * 2.2 + math.sin(TAU * 220 * t) * 0.01)
    return loopify(out)


def generator_running():
    nz, out = Noise(3), []
    for i in range(n(12)):
        t = i / SR
        throb = 0.6 + 0.4 * (0.5 + 0.5 * math.sin(TAU * 6 * t))
        s = (math.sin(TAU * 32 * t) * 0.6 + math.sin(TAU * 64 * t) * 0.35 + math.sin(TAU * 96.5 * t) * 0.15) * throb
        tick = (math.sin(TAU * 1800 * t) * decay(i % n(1 / 6), 0.004)) * 0.15
        s += tick + nz.lowpassed(0.2) * 0.25
        out.append(s)
    return loopify(out)


def murmur():
    """Deep slow swell every 4 s with a faint glassy shimmer — the Murmur under the ice."""
    nz, out = Noise(4), []
    period = 4.0
    for i in range(n(16)):
        t = i / SR
        x = (t % period) / period
        sw = (x / 0.25) if x < 0.25 else (1 - (x - 0.25) / 0.75) ** 2
        s = (math.sin(TAU * 38 * t) * 0.7 + math.sin(TAU * 57 * t) * 0.25) * sw
        s += math.sin(TAU * 880 * t + math.sin(TAU * 0.5 * t) * 3) * 0.03 * sw
        s += nz.brownish() * 0.3
        out.append(s)
    return loopify(out, 2.0)


# ------------------------------------------------------------------ effects

def footstep(seed):
    nz, out = Noise(10 + seed), []
    f0 = 95 + seed * 9
    for i in range(n(0.22)):
        t = i / SR
        s = math.sin(TAU * f0 * t) * decay(i, 0.035) * 0.9 + nz.lowpassed(0.35) * decay(i, 0.025) * 1.2
        s += math.sin(TAU * (1900 + seed * 150) * t) * decay(i, 0.008) * 0.12  # metal grate ring
        out.append(s)
    return out


def door(opening):
    nz, out = Noise(20 if opening else 21), []
    total = n(1.1)
    for i in range(total):
        t = i / SR
        k = t / 1.1
        f = 140 + (260 if opening else -60) * k
        s = math.sin(TAU * f * t) * 0.35 + math.sin(TAU * f * 2.01 * t) * 0.12
        s += nz.lowpassed(0.25) * 0.5
        s *= env(i, total, 0.05, 0.25)
        out.append(s)
    # end thunk
    for i in range(n(0.25)):
        t = i / SR
        out.append(math.sin(TAU * 60 * t) * decay(i, 0.06) * (0.9 if not opening else 0.5))
    return out


def door_locked():
    out = []
    for b in range(2):
        for i in range(n(0.16)):
            t = i / SR
            out.append((math.sin(TAU * 110 * t) + 0.5 * math.copysign(1, math.sin(TAU * 220 * t))) * 0.5 * env(i, n(0.16), 0.005, 0.03))
        out += [0.0] * n(0.07)
    return out


def chime(notes, step=0.11, tail=0.6):
    out = [0.0] * n(step * len(notes) + tail)
    for k, f in enumerate(notes):
        start = n(step * k)
        for i in range(n(tail + 0.2)):
            if start + i >= len(out):
                break
            t = i / SR
            out[start + i] += (math.sin(TAU * f * t) + 0.3 * math.sin(TAU * f * 2 * t)) * decay(i, 0.25) * 0.5 * min(1, t / 0.004)
    return out


def click(f=2600, tau=0.006, body=0.0):
    nz, out = Noise(30), []
    for i in range(n(0.08)):
        t = i / SR
        out.append(math.sin(TAU * f * t) * decay(i, tau) + nz.white() * decay(i, 0.003) * 0.5 + math.sin(TAU * 150 * t) * decay(i, 0.02) * body)
    return out


def breaker():
    nz, out = Noise(40), []
    for i in range(n(0.7)):
        t = i / SR
        s = math.sin(TAU * 70 * t) * decay(i, 0.08) * 1.2 + nz.white() * decay(i, 0.015)
        # electrical zap
        if t > 0.05:
            s += math.copysign(1, math.sin(TAU * 120 * t)) * nz.white() * decay(i - n(0.05), 0.12) * 0.35
        out.append(s)
    return out


def power_up():
    out = []
    total = n(1.8)
    for i in range(total):
        t = i / SR
        f = 40 + 180 * (t / 1.8) ** 1.6
        s = math.sin(TAU * f * t) * 0.6 + math.sin(TAU * f * 2 * t) * 0.2
        out.append(s * env(i, total, 0.2, 0.5))
    return out


def generator_start():
    nz, out = Noise(50), []
    total = n(3.2)
    phase = 0.0
    for i in range(total):
        t = i / SR
        f = 8 + 26 * min(1, t / 2.6)
        phase += TAU * f / SR
        s = math.sin(phase * 4) * 0.5 + math.sin(phase * 8) * 0.25 + nz.lowpassed(0.15) * 0.4 * min(1, t / 1.5)
        out.append(s * env(i, total, 0.1, 0.6))
    return out


def buzz(seconds=0.45, f=82):
    out = []
    for i in range(n(seconds)):
        t = i / SR
        out.append((math.copysign(1, math.sin(TAU * f * t)) * 0.4 + math.sin(TAU * f * 2 * t) * 0.3) * env(i, n(seconds), 0.01, 0.08))
    return out


def beep(freqs, each=0.07):
    out = []
    for f in freqs:
        for i in range(n(each)):
            t = i / SR
            out.append(math.sin(TAU * f * t) * env(i, n(each), 0.004, 0.02) * 0.6)
    return out


def tape_start():
    nz, out = Noise(60), click(1800, 0.01, 0.6)
    for i in range(n(0.6)):
        out.append(nz.lowpassed(0.5) * 0.25 * (1 - i / n(0.6)))
    return out


def clamp():
    nz, out = Noise(70), []
    for i in range(n(1.2)):
        t = i / SR
        out.append(math.sin(TAU * 45 * t) * decay(i, 0.25) + math.sin(TAU * 1250 * t) * decay(i, 0.12) * 0.15 + nz.white() * decay(i, 0.02) * 0.6)
    return out


def alarm():
    """Two-tone reactor klaxon (overshoot, overload countdown)."""
    out = []
    for b in range(3):
        for f in (620, 440):
            total = n(0.32)
            for i in range(total):
                t = i / SR
                s = math.copysign(1, math.sin(TAU * f * t)) * 0.35 + math.sin(TAU * f * t) * 0.4
                out.append(s * env(i, total, 0.01, 0.05))
    return out


def valve():
    """Metal wheel turning a step: squeal + clunk."""
    nz, out = Noise(80), []
    total = n(0.6)
    for i in range(total):
        t = i / SR
        f = 520 + 140 * math.sin(TAU * 3 * t)
        s = math.sin(TAU * f * t) * 0.25 * env(i, total, 0.05, 0.2) + nz.lowpassed(0.3) * 0.3 * env(i, total, 0.02, 0.3)
        out.append(s)
    for i in range(n(0.2)):
        t = i / SR
        out.append(math.sin(TAU * 75 * t) * decay(i, 0.05))
    return out


def lift():
    """Lift motor run-up, travel hum, stop thunk."""
    out = []
    total = n(1.8)
    for i in range(total):
        t = i / SR
        f = 55 + 25 * min(1, t / 0.4)
        s = math.sin(TAU * f * t) * 0.5 + math.sin(TAU * f * 3 * t) * 0.12
        out.append(s * env(i, total, 0.3, 0.3))
    for i in range(n(0.3)):
        t = i / SR
        out.append(math.sin(TAU * 60 * t) * decay(i, 0.07) * 0.9)
    return out


def crank():
    """Ratchet: a few quick clicks over a creak."""
    nz, out = Noise(90), []
    total = n(0.5)
    for i in range(total):
        t = i / SR
        s = math.sin(TAU * 300 * t) * 0.15 * env(i, total, 0.05, 0.2)
        for k in range(4):
            j = i - n(0.1 * k + 0.03)
            if j >= 0:
                s += nz.white() * decay(j, 0.004) * 0.8 + math.sin(TAU * 1800 * j / SR) * decay(j, 0.006) * 0.4
        out.append(s)
    return out


def main():
    write("amb_station_hum", station_hum(), "Ambience", 0.6)
    write("amb_ventilation", ventilation(), "Ambience", 0.5)
    write("amb_generator", generator_running(), "Ambience", 0.7)
    write("amb_murmur", murmur(), "Ambience", 0.6)
    for k in range(4):
        write(f"sfx_footstep_{k + 1}", footstep(k), peak=0.5)
    write("sfx_door_open", door(True))
    write("sfx_door_close", door(False))
    write("sfx_door_locked", door_locked(), peak=0.6)
    write("sfx_pickup", chime([660, 990]), peak=0.55)
    write("sfx_objective", chime([523, 659, 784], 0.13, 0.9), peak=0.5)
    write("sfx_cairn", chime([392, 587], 0.18, 0.9), peak=0.35)
    write("sfx_click", click(), peak=0.5)
    write("sfx_switch", click(1400, 0.012, 0.8), peak=0.6)
    write("sfx_flashlight", click(3200, 0.004, 0.2), peak=0.45)
    write("sfx_breaker", breaker())
    write("sfx_fuse_insert", click(900, 0.02, 1.0) + click(2200, 0.004))
    write("sfx_cable_plug", click(600, 0.03, 1.2))
    write("sfx_power_up", power_up(), peak=0.7)
    write("sfx_generator_start", generator_start(), peak=0.8)
    write("sfx_error", buzz(), peak=0.45)
    write("sfx_terminal_open", beep([880, 1320]), peak=0.4)
    write("sfx_terminal_key", beep([1500], 0.03), peak=0.3)
    write("sfx_terminal_ok", beep([880, 1175, 1760]), peak=0.4)
    write("sfx_terminal_error", buzz(0.3, 120), peak=0.4)
    write("sfx_log_start", tape_start(), peak=0.5)
    write("sfx_docking_clamp", clamp())
    write("sfx_ui", click(2000, 0.004), peak=0.25)
    write("sfx_alarm", alarm(), peak=0.55)
    write("sfx_valve", valve(), peak=0.6)
    write("sfx_lift", lift(), peak=0.6)
    write("sfx_crank", crank(), peak=0.6)


if __name__ == "__main__":
    main()
