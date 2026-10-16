#!/usr/bin/env python3
"""Synthesize every sound effect and music loop. Output goes to Assets/_Project/Resources/Audio/.

Everything is generated from oscillators, filtered noise and simple reverb, with fixed random
seeds, so reruns produce identical files. Edit a recipe and rerun to retune a sound. Requires numpy.
Sound effects are 44.1 kHz mono; music is 22.05 kHz mono (smaller files, fine for loops).
"""
import sys
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1] / "Assets/_Project/Resources/Audio"
SR = 44100
MSR = 22050


# ---------------------------------------------------------------- building blocks

class Sig(np.ndarray):
    """A numpy array that pads the shorter operand with silence, so layered sounds of different
    lengths can simply be added. Every building block returns one."""

    def __new__(cls, data):
        return np.asarray(data, dtype=float).view(cls)

    def __array_finalize__(self, obj):
        pass

    @staticmethod
    def _pair(a, b):
        a = np.asarray(a, dtype=float)
        b = np.asarray(b, dtype=float)
        if a.ndim == 0 or b.ndim == 0 or a.shape == b.shape:
            return a, b
        n = max(a.shape[0], b.shape[0])
        pa = np.zeros(n); pa[: a.shape[0]] = a
        pb = np.zeros(n); pb[: b.shape[0]] = b
        return pa, pb

    def __add__(self, other):
        a, b = Sig._pair(self, other)
        return Sig(a + b)

    __radd__ = __add__

    def __sub__(self, other):
        a, b = Sig._pair(self, other)
        return Sig(a - b)

    def __mul__(self, other):
        a = np.asarray(self, dtype=float)
        b = np.asarray(other, dtype=float)
        if a.ndim == 0 or b.ndim == 0 or a.shape == b.shape:
            return Sig(a * b)
        # Envelopes shorter than the sound fade to silence; longer ones are cut
        n = min(a.shape[0], b.shape[0])
        return Sig(a[:n] * b[:n])

    __rmul__ = __mul__

    def __getitem__(self, key):
        r = np.ndarray.__getitem__(self, key)
        return Sig(r) if isinstance(r, np.ndarray) and r.ndim else r

def rng(seed):
    return np.random.default_rng(seed)


def tt(duration, sr=SR):
    return np.arange(int(duration * sr)) / sr


def white(duration, seed=0, sr=SR):
    return Sig(rng(seed).uniform(-1, 1, int(duration * sr)))


def spectral(x, mask_fn, sr=SR):
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1.0 / sr)
    return Sig(np.fft.irfft(spec * mask_fn(np.maximum(f, 1e-3)), len(x)))


def lowpass(x, fc, sr=SR, order=4):
    return spectral(x, lambda f: 1.0 / (1.0 + (f / fc) ** (2 * order)), sr)


def highpass(x, fc, sr=SR, order=4):
    return spectral(x, lambda f: 1.0 / (1.0 + (fc / f) ** (2 * order)), sr)


def bandpass(x, lo, hi, sr=SR):
    return highpass(lowpass(x, hi, sr), lo, sr)


def env(n, attack=0.005, decay=0.1, sustain=0.0, release=0.0, sr=SR):
    """Attack-decay(-sustain-release) envelope over n samples (times in seconds)."""
    e = np.zeros(n)
    a, d, r = int(attack * sr), int(decay * sr), int(release * sr)
    a = max(a, 1)
    e[:a] = np.linspace(0, 1, a)
    end_d = min(n, a + d)
    if end_d > a:
        e[a:end_d] = np.linspace(1, sustain, end_d - a) if sustain > 0 else np.exp(-np.linspace(0, 6, end_d - a))
    if end_d < n:
        e[end_d:] = sustain
    if r > 0 and r < n:
        e[-r:] *= np.linspace(1, 0, r)
    return Sig(e)


def decay_env(n, tau, sr=SR, attack=0.002):
    t = np.arange(n) / sr
    a = np.minimum(t / max(attack, 1e-4), 1.0)
    return Sig(a * np.exp(-t / tau))


def sine(freq, duration, sr=SR, phase=0.0):
    return Sig(np.sin(2 * np.pi * freq * tt(duration, sr) + phase))


def sweep(f0, f1, duration, sr=SR, shape="exp"):
    t = tt(duration, sr)
    k = t / max(duration, 1e-6)
    f = f0 * (f1 / f0) ** k if shape == "exp" else f0 + (f1 - f0) * k
    return Sig(np.sin(2 * np.pi * np.cumsum(f) / sr))


def saw(freq, duration, sr=SR):
    ph = (tt(duration, sr) * freq) % 1.0
    return Sig(2 * ph - 1)


def square(freq, duration, sr=SR, duty=0.5):
    ph = (tt(duration, sr) * freq) % 1.0
    return Sig(np.where(ph < duty, 1.0, -1.0))


def sweep_saw(f0, f1, duration, sr=SR):
    t = tt(duration, sr)
    f = f0 * (f1 / f0) ** (t / duration)
    ph = (np.cumsum(f) / sr) % 1.0
    return Sig(2 * ph - 1)


def softclip(x, drive=1.0):
    return np.tanh(x * drive)


def reverb(x, tau=0.4, wet=0.25, sr=SR, seed=7, damp=5000):
    n = int(tau * 4 * sr)
    ir = rng(seed).uniform(-1, 1, n) * np.exp(-np.arange(n) / sr / tau)
    ir = lowpass(ir, damp, sr, order=1)
    size = len(x) + n
    out = np.fft.irfft(np.fft.rfft(x, size) * np.fft.rfft(ir, size), size)
    out /= max(np.abs(out).max(), 1e-9)
    padded = np.zeros(size)
    padded[: len(x)] = x
    return Sig(padded * (1 - wet) + out * wet * max(np.abs(x).max(), 1e-9))


def echo(x, delay, feedback=0.4, taps=4, sr=SR):
    out = np.zeros(len(x) + int(delay * sr * taps) + 1)
    out[: len(x)] += x
    for i in range(1, taps + 1):
        s = int(delay * sr * i)
        out[s : s + len(x)] += x * (feedback ** i)
    return Sig(out)


def mix(*parts, length=None):
    n = length or max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        m = min(n, len(p))
        out[:m] += np.asarray(p, dtype=float)[:m]
    return Sig(out)


def place(buf, x, start, sr=SR, gain=1.0):
    s = int(start * sr)
    if s >= len(buf):
        return
    m = min(len(x), len(buf) - s)
    buf[s : s + m] += x[:m] * gain


def normalize(x, peak=0.9):
    m = np.abs(np.asarray(x)).max()
    return Sig(np.asarray(x, dtype=float) / m * peak) if m > 0 else Sig(x)


def fade_edges(x, ms=4, sr=SR):
    n = int(ms / 1000 * sr)
    if n * 2 < len(x):
        x = x.copy()
        x[:n] *= np.linspace(0, 1, n)
        x[-n:] *= np.linspace(1, 0, n)
    return x


def make_loop(x, fade=0.08, sr=SR):
    """Crossfade the end into the start so the loop point is inaudible."""
    n = int(fade * sr)
    body = x[: len(x) - n].copy()
    tail = x[len(x) - n :]
    ramp = np.linspace(0, 1, n)
    body[:n] = body[:n] * np.sqrt(ramp) + tail * np.sqrt(1 - ramp)
    return body


def fold_tail(buf, loop_len):
    """Wrap anything beyond loop_len (reverb tails, late notes) back onto the start."""
    body = buf[:loop_len].copy()
    extra = buf[loop_len:]
    body[: len(extra)] += extra[: loop_len]
    return body


def whoosh(duration, f_start, f_end, seed, width=0.9, sr=SR):
    """Noise whoosh: several band-passed copies whose centre frequency sweeps over time."""
    n = int(duration * sr)
    base = white(duration, seed, sr)
    out = np.zeros(n)
    steps = 7
    t = np.linspace(0, 1, n)
    for i in range(steps):
        k = i / (steps - 1)
        fc = f_start * (f_end / f_start) ** k
        band = bandpass(base, fc * 0.6, fc * 1.6, sr)
        out += band * np.exp(-((t - k) ** 2) / (2 * (width / steps) ** 2))
    return out


def save(name, x, sr=SR, sub="SFX"):
    folder = ROOT / sub
    folder.mkdir(parents=True, exist_ok=True)
    x = np.asarray(x, dtype=float)
    x = x - x.mean()  # square and saw sources leave a DC offset that pops at clip edges
    x = np.clip(x, -1, 1)
    data = (x * 32767).astype(np.int16)
    with wave.open(str(folder / f"{name}.wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data.tobytes())


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def bell(freq, duration, sr=SR, tau=0.4):
    """Inharmonic partials with individual decays: reads as a struck metal bell."""
    partials = [(1.0, 1.0), (2.76, 0.5), (5.4, 0.25), (8.93, 0.12)]
    out = np.zeros(int(duration * sr))
    for ratio, amp in partials:
        out += amp * sine(freq * ratio, duration, sr) * decay_env(len(out), tau / (0.6 + ratio * 0.4), sr)
    return out


# ---------------------------------------------------------------- sound effects
def sfx():
    out = {}

    # --- weapons
    flame = bandpass(white(1.4, 1), 180, 2800) * (0.7 + 0.3 * sine(7, 1.4)) + 0.6 * lowpass(white(1.4, 2), 140)
    crackle = np.zeros(len(flame))
    r = rng(3)
    for _ in range(26):
        s = r.integers(0, len(flame) - 600)
        crackle[s : s + 500] += r.uniform(-1, 1, 500) * np.exp(-np.arange(500) / 90) * r.uniform(0.4, 1)
    out["flame_loop"] = make_loop(normalize(flame + 0.5 * crackle, 0.8), 0.15)

    out["mop_swing"] = normalize(whoosh(0.26, 500, 2600, 11) * env(int(0.26 * SR), 0.05, 0.2), 0.8)
    thump = sweep(180, 48, 0.25) * decay_env(int(0.25 * SR), 0.07)
    crack = bandpass(white(0.12, 12), 800, 5000) * decay_env(int(0.12 * SR), 0.02)
    out["mop_hit"] = normalize(mix(thump, crack * 0.8), 0.9)

    spray = bandpass(white(1.2, 21), 900, 4200) * (0.65 + 0.35 * sine(9, 1.2))
    body = lowpass(white(1.2, 22), 420) * (0.6 + 0.4 * sine(4, 1.2, phase=1))
    out["foam_loop"] = make_loop(normalize(spray + 0.9 * body, 0.55), 0.12)

    zap_n = int(0.3 * SR)
    gate = (rng(31).uniform(0, 1, zap_n // 120 + 1) > 0.45).astype(float).repeat(120)[:zap_n]
    zap = highpass(white(0.3, 30), 1800) * gate * decay_env(zap_n, 0.12) + 0.5 * sweep(2400, 220, 0.3) * decay_env(zap_n, 0.1)
    out["zap"] = normalize(zap, 0.85)

    n = int(1.6 * SR)
    charge = sweep_saw(110, 880, 1.6) * np.linspace(0.15, 1, n) * (0.7 + 0.3 * sine(14, 1.6))
    charge += 0.4 * sweep(220, 1760, 1.6) * np.linspace(0, 1, n)
    out["beam_charge"] = normalize(lowpass(charge, 4000), 0.7)
    boom = sweep(220, 30, 0.6) * decay_env(int(0.6 * SR), 0.2)
    shriek = sweep(3200, 120, 0.5) * decay_env(int(0.5 * SR), 0.12)
    out["beam_fire"] = normalize(reverb(mix(boom, shriek * 0.7, highpass(white(0.5, 41), 2000) * decay_env(int(0.5 * SR), 0.1) * 0.5), 0.3, 0.2), 0.95)
    out["beam_tap"] = normalize(sweep(1800, 420, 0.14) * decay_env(int(0.14 * SR), 0.05), 0.75)

    # --- combat feedback
    for i in range(3):
        splat = bandpass(white(0.12, 50 + i), 300 + 120 * i, 3200) * decay_env(int(0.12 * SR), 0.03 + 0.01 * i)
        blip = sweep(260 - 30 * i, 110, 0.1) * decay_env(int(0.1 * SR), 0.04)
        out[f"hit_{i}"] = normalize(mix(splat, blip * 0.8), 0.85)
        sq = lowpass(white(0.35, 60 + i), 900 + 200 * i) * decay_env(int(0.35 * SR), 0.1)
        sq *= (0.6 + 0.4 * sine(30 + 6 * i, 0.35))
        crunch = np.zeros(int(0.35 * SR))
        r = rng(70 + i)
        for _ in range(7):
            place(crunch, r.uniform(-1, 1, 160) * np.exp(-np.arange(160) / 30), r.uniform(0, 0.09), gain=0.9)
        out[f"die_{i}"] = normalize(mix(sq, crunch * 0.8, sweep(200, 70, 0.2) * decay_env(int(0.2 * SR), 0.06) * 0.7), 0.9)

    crit = mix(sine(2400, 0.12) * decay_env(int(0.12 * SR), 0.04), sine(3600, 0.12) * decay_env(int(0.12 * SR), 0.03) * 0.5)
    out["crit"] = normalize(crit, 0.6)

    hurt = mix(sweep(150, 60, 0.28) * decay_env(int(0.28 * SR), 0.09), lowpass(white(0.2, 80), 1500) * decay_env(int(0.2 * SR), 0.05) * 0.7,
               sweep_saw(310, 190, 0.22) * decay_env(int(0.22 * SR), 0.08) * 0.35)
    out["player_hurt"] = normalize(lowpass(hurt, 3500), 0.95)
    d = sweep_saw(420, 38, 1.4) * decay_env(int(1.4 * SR), 0.5) + 0.6 * lowpass(white(1.4, 81), 600) * decay_env(int(1.4 * SR), 0.4)
    out["player_death"] = normalize(reverb(lowpass(d, 2500), 0.7, 0.35), 0.95)
    out["dash"] = normalize(whoosh(0.2, 900, 3600, 82, 0.8) * env(int(0.2 * SR), 0.02, 0.15), 0.75)

    ex = lowpass(white(0.9, 83), 1400) * decay_env(int(0.9 * SR), 0.25) + sweep(110, 28, 0.8) * decay_env(int(0.8 * SR), 0.3)
    out["explosion"] = normalize(reverb(ex, 0.5, 0.3), 0.95)
    over = mix(zap_like(0.5, 84), sweep(90, 25, 0.8) * decay_env(int(0.8 * SR), 0.3))
    out["overload"] = normalize(reverb(over, 0.4, 0.25), 0.95)

    spit = mix(sweep(1100, 320, 0.16) * decay_env(int(0.16 * SR), 0.05), bandpass(white(0.1, 85), 1500, 6000) * decay_env(int(0.1 * SR), 0.03) * 0.5)
    out["spit"] = normalize(spit, 0.7)
    out["spit_hit"] = normalize(bandpass(white(0.1, 86), 400, 2500) * decay_env(int(0.1 * SR), 0.025), 0.7)

    # --- progression and UI
    notes = [60, 64, 67, 72]
    heal = np.zeros(int(0.7 * SR))
    for i, nn in enumerate(notes):
        place(heal, sine(midi(nn + 12), 0.4) * decay_env(int(0.4 * SR), 0.12), i * 0.06)
    out["heal"] = normalize(heal, 0.7)

    lvl = np.zeros(int(1.2 * SR))
    for i, nn in enumerate([60, 64, 67, 72, 76]):
        tone = (square(midi(nn), 0.35, duty=0.35) * 0.4 + sine(midi(nn), 0.35)) * decay_env(int(0.35 * SR), 0.15)
        place(lvl, tone, i * 0.075)
    out["level_up"] = normalize(echo(lvl, 0.18, 0.3, 3)[: int(1.4 * SR)], 0.8)

    clank = bell(520, 0.5, tau=0.3) * 0.7 + bell(830, 0.5, tau=0.2) * 0.4
    stamp = sweep(140, 45, 0.18) * decay_env(int(0.18 * SR), 0.05)
    out["upgrade_pick"] = normalize(mix(clank, stamp * 1.2, bandpass(white(0.05, 87), 600, 4000) * decay_env(int(0.05 * SR), 0.01)), 0.9)

    out["ui_hover"] = normalize(sine(1900, 0.035) * decay_env(int(0.035 * SR), 0.01), 0.35)
    out["ui_click"] = normalize(sweep(700, 1100, 0.07) * decay_env(int(0.07 * SR), 0.025), 0.6)
    out["ui_buy"] = normalize(mix(bell(1318, 0.5, tau=0.25), bell(1975, 0.5, tau=0.2) * 0.8), 0.8)
    out["ui_deny"] = normalize(lowpass(square(95, 0.22), 800) * decay_env(int(0.22 * SR), 0.12), 0.6)

    chime = mix(bell(midi(72), 1.0, tau=0.5), np.concatenate([np.zeros(int(0.12 * SR)), bell(midi(76), 0.9, tau=0.5)]),
                np.concatenate([np.zeros(int(0.24 * SR)), bell(midi(79), 0.8, tau=0.6)]))
    out["room_clear"] = normalize(reverb(chime, 0.6, 0.25), 0.8)

    klax = np.zeros(int(0.9 * SR))
    for i in range(3):
        place(klax, lowpass(square(midi(69) if i % 2 == 0 else midi(63), 0.24, duty=0.4), 2500) * env(int(0.24 * SR), 0.01, 0.2), i * 0.28)
    out["room_start"] = normalize(klax, 0.6)

    out["pickup"] = normalize(sweep(500, 1400, 0.16) * decay_env(int(0.16 * SR), 0.07), 0.6)
    out["scrap"] = normalize(mix(bell(1568, 0.25, tau=0.12), bell(2093, 0.25, tau=0.1) * 0.6), 0.55)
    out["second_wind"] = normalize(reverb(mix(sweep(200, 1600, 1.0) * np.linspace(0, 1, SR) * 0.5, bell(880, 1.2, tau=0.7)), 0.6, 0.3), 0.85)
    siren_n = int(0.4 * SR)
    up = lowpass(sweep_saw(220, 480, 0.4), 2200) * env(siren_n, 0.02, 0.35)
    down = lowpass(sweep_saw(480, 220, 0.4), 2200) * env(siren_n, 0.02, 0.35)
    out["boss_warning"] = normalize(np.concatenate([np.asarray(up), np.asarray(down)]), 0.6)
    roar_n = int(1.2 * SR)
    roar = softclip(sweep_saw(75, 42, 1.2) * (0.55 + 0.45 * sine(24, 1.2)) * 2.2) * env(roar_n, 0.05, 0.9)
    roar += 0.5 * bandpass(white(1.2, 88), 200, 1400) * (0.5 + 0.5 * sine(24, 1.2)) * env(roar_n, 0.05, 0.9)
    out["boss_roar"] = normalize(reverb(lowpass(roar, 3000), 0.8, 0.3), 0.95)
    out["boss_hurt"] = normalize(lowpass(sweep_saw(130, 60, 0.3), 1800) * decay_env(int(0.3 * SR), 0.1), 0.8)
    n = int(0.18 * SR)
    flicker = (rng(90).uniform(0, 1, n // 200 + 1) > 0.5).astype(float).repeat(200)[:n]
    out["spawn"] = normalize(bandpass(white(0.18, 89), 2500, 7000) * flicker * decay_env(n, 0.1), 0.4)

    for i in range(2):
        step = lowpass(white(0.09, 100 + i), 900 + 250 * i) * decay_env(int(0.09 * SR), 0.025)
        step = step + 0.5 * sweep(120 + 20 * i, 60, 0.07) * decay_env(int(0.07 * SR), 0.02)
        out[f"step_{i}"] = normalize(step, 0.3)
    return out


def zap_like(duration, seed):
    n = int(duration * SR)
    gate = (rng(seed).uniform(0, 1, n // 110 + 1) > 0.4).astype(float).repeat(110)[:n]
    return highpass(white(duration, seed), 1500) * gate * decay_env(n, duration * 0.4) + 0.5 * sweep(2000, 200, duration) * decay_env(n, duration * 0.3)


# ---------------------------------------------------------------- music
def kick(sr=MSR, strength=1.0):
    n = int(0.28 * sr)
    return sweep(150, 42, 0.28, sr) * decay_env(n, 0.09, sr) * strength


def hat(sr=MSR, open_=False, seed=0):
    d = 0.18 if open_ else 0.05
    return highpass(white(d, seed, sr), 6000, sr) * decay_env(int(d * sr), 0.05 if open_ else 0.012, sr) * 0.35


def clap(sr=MSR, seed=1):
    out = np.zeros(int(0.25 * sr))
    for k, off in enumerate((0, 0.012, 0.024)):
        place(out, bandpass(white(0.08, seed + k, sr), 900, 4200, sr) * decay_env(int(0.08 * sr), 0.03, sr), off, sr)
    out += bandpass(white(0.25, seed + 9, sr), 900, 3500, sr) * decay_env(len(out), 0.07, sr) * 0.5
    return out * 0.8


def bass_note(freq, dur, sr=MSR, drive=2.0):
    n = int(dur * sr)
    s = softclip(saw(freq, dur, sr) * 0.8 + square(freq * 0.5, dur, sr) * 0.5, drive)
    return lowpass(s, 900, sr, 2) * env(n, 0.004, dur * 0.7, 0.0, 0.02, sr) * 0.6


def pad_chord(freqs, dur, sr=MSR, cutoff=1400):
    n = int(dur * sr)
    out = np.zeros(n)
    for f in freqs:
        for detune in (0.994, 1.0, 1.006):
            out += saw(f * detune, dur, sr)
    out = lowpass(out, cutoff, sr, 2)
    return out * env(n, dur * 0.25, dur * 0.4, 0.7, dur * 0.3, sr) * 0.12


def epiano(freq, dur, sr=MSR):
    n = int(dur * sr)
    s = sine(freq, dur, sr) + 0.35 * sine(freq * 2, dur, sr) * decay_env(n, 0.25, sr) + 0.15 * sine(freq * 3.01, dur, sr) * decay_env(n, 0.12, sr)
    return s * decay_env(n, 0.5, sr) * 0.35


def music_lobby():
    bpm = 78
    beat = 60.0 / bpm
    bar = beat * 4
    bars = 8
    length = int(bar * bars * MSR)
    buf = np.zeros(length + int(3.0 * MSR))
    chords = [(57, [57, 60, 64]), (53, [53, 57, 60]), (48, [48, 52, 55]), (55, [55, 59, 62])]
    for b in range(bars):
        root, notes = chords[b % 4]
        place(buf, pad_chord([midi(n) for n in notes], bar * 1.15, cutoff=1100), b * bar, MSR)
        place(buf, bass_note(midi(root - 12), beat * 1.6, MSR, 1.0) * 0.7, b * bar, MSR)
        for i, n in enumerate(notes + [notes[1] + 12]):
            place(buf, epiano(midi(n + 12), beat * 1.4), b * bar + i * beat, MSR, 0.8)
        for i in range(8):
            place(buf, hat(MSR, False, b * 8 + i) * 0.5, b * bar + i * beat / 2, MSR)
    buf = reverb(buf, 1.0, 0.3, MSR, damp=3500)
    return normalize(fold_tail(buf, length), 0.7)


def music_combat():
    bpm = 126
    beat = 60.0 / bpm
    bar = beat * 4
    bars = 16
    length = int(bar * bars * MSR)
    buf = np.zeros(length + int(2.0 * MSR))
    prog = [45, 45, 41, 43]  # A, A, F, G
    riff = [0, None, 0, 3, None, 0, 5, None, 0, None, 7, 5, None, 3, None, 0]
    for b in range(bars):
        root = prog[(b // 2) % 4]
        for i in range(4):
            place(buf, kick(MSR), b * bar + i * beat, MSR, 1.0 if i % 2 == 0 else 0.85)
        for i in (1, 3):
            place(buf, clap(MSR, b * 3 + i), b * bar + i * beat, MSR, 0.7)
        for i in range(8):
            place(buf, hat(MSR, i % 2 == 1, b * 8 + i), b * bar + i * beat / 2, MSR, 1.0 if i % 2 else 0.6)
        for i, step in enumerate(riff):
            if step is None:
                continue
            place(buf, bass_note(midi(root + step), beat * 0.22, MSR, 2.2), b * bar + i * beat / 4, MSR, 0.9)
        if b >= 4:
            stab = lowpass(softclip(saw(midi(root + 24), beat * 0.3, MSR) + saw(midi(root + 27), beat * 0.3, MSR), 1.6), 2600, MSR, 2) * env(int(beat * 0.3 * MSR), 0.003, 0.25, 0, 0.03, MSR) * 0.22
            place(buf, stab, b * bar + beat * 1.5, MSR)
            place(buf, stab, b * bar + beat * 3.5, MSR)
        if b % 4 == 0:
            place(buf, pad_chord([midi(root + 12), midi(root + 15), midi(root + 19)], bar * 4, MSR, 900), b * bar, MSR)
    buf = reverb(buf, 0.5, 0.15, MSR, damp=3000)
    return normalize(fold_tail(buf, length), 0.8)


def music_boss():
    bpm = 148
    beat = 60.0 / bpm
    bar = beat * 4
    bars = 16
    length = int(bar * bars * MSR)
    buf = np.zeros(length + int(2.0 * MSR))
    prog = [40, 40, 41, 40, 43, 43, 41, 39]
    riff = [0, 0, None, 0, 1, None, 0, None, 6, None, 6, 5, None, 3, 1, None]
    for b in range(bars):
        root = prog[(b // 2) % len(prog)]
        for i in range(4):
            place(buf, kick(MSR, 1.1), b * bar + i * beat, MSR)
            if i % 2 == 1:
                place(buf, kick(MSR, 0.7), b * bar + i * beat + beat * 0.75, MSR)
        for i in (1, 3):
            place(buf, clap(MSR, b * 5 + i), b * bar + i * beat, MSR, 0.85)
        for i in range(16):
            place(buf, hat(MSR, False, b * 16 + i), b * bar + i * beat / 4, MSR, 0.5 if i % 2 else 0.8)
        for i, step in enumerate(riff):
            if step is None:
                continue
            place(buf, bass_note(midi(root + step), beat * 0.22, MSR, 3.0), b * bar + i * beat / 4, MSR, 1.0)
        siren = lowpass(sweep_saw(midi(root + 36), midi(root + 43), bar, MSR), 2400, MSR, 2) * env(int(bar * MSR), bar * 0.5, bar * 0.4, 0.2, bar * 0.1, MSR) * 0.07
        place(buf, siren, b * bar, MSR)
        if b % 2 == 0:
            place(buf, pad_chord([midi(root + 12), midi(root + 15), midi(root + 18)], bar * 2, MSR, 1000), b * bar, MSR, 1.3)
    buf = reverb(buf, 0.6, 0.18, MSR, damp=3000)
    return normalize(fold_tail(buf, length), 0.85)


def music_victory():
    length = int(7.0 * MSR)
    buf = np.zeros(length + int(3.0 * MSR))
    bar = 60.0 / 100 * 4
    for i, notes in enumerate([[60, 64, 67], [65, 69, 72], [67, 71, 74], [72, 76, 79]]):
        place(buf, pad_chord([midi(n) for n in notes], bar * 1.2, MSR, 2200), i * bar * 0.5, MSR, 1.5)
        for j, n in enumerate(notes):
            place(buf, epiano(midi(n + 12), 1.2), i * bar * 0.5 + j * 0.12, MSR, 1.0)
    return normalize(reverb(buf, 1.2, 0.3, MSR)[: int(9 * MSR)], 0.7)


def music_gameover():
    buf = np.zeros(int(8.0 * MSR))
    for i, notes in enumerate([[57, 60, 64], [55, 58, 62], [53, 57, 60], [52, 55, 59]]):
        place(buf, pad_chord([midi(n) for n in notes], 2.6, MSR, 700), i * 1.5, MSR, 1.4)
        place(buf, epiano(midi(notes[0] + 12), 2.0), i * 1.5, MSR, 0.9)
    return normalize(reverb(buf, 1.4, 0.35, MSR), 0.65)


def main():
    made = []
    for name, data in sfx().items():
        save(name, fade_edges(normalize(data, 0.92)), SR, "SFX")
        made.append(name)
    for name, fn in (("music_lobby", music_lobby), ("music_combat", music_combat), ("music_boss", music_boss),
                     ("music_victory", music_victory), ("music_gameover", music_gameover)):
        save(name, fn(), MSR, "Music")
        made.append(name)
    total = sum(p.stat().st_size for p in ROOT.rglob("*.wav"))
    print(f"wrote {len(made)} clips, {total / 1e6:.1f} MB total in {ROOT}")


if __name__ == "__main__":
    main()
