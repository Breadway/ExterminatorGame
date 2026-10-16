#!/usr/bin/env python3
"""Monte Carlo full-run balance simulation. Mirrors the game's formulas:
RoomManager counts/modifiers, WaveSpawner picks, XPManager thresholds, PlayerStats damage,
armor reduction, scrap rewards. Abstractions: combat resolves via DPS vs HP pools with a
bot-skill damage-taken factor; upgrades picked greedily for DPS (paths forced at 10/20/30).

Usage: python3 tools/balance_model.py [--runs N] [--table]
FINAL TUNING (implemented in game): XP 60 x1.22, 12% level heal; rooms 5+0.8 max 55;
enemy HP +8%/room, enemy damage +3.5%/room, kill-XP ramp x(1+0.1*room); contact 0.6 x 5
attackers x skill 0.5; per-weapon exposure (melee takes more contact).
Results: ~40-65% win rate per weapon for a greedy mid-skill bot, deaths spread rooms 7-25,
level 10 ~room 10, late teens by room 25, room clears 8-15s, scrap ~600/victory.
Known model gaps (verified live instead): mop guard-spacing skill, Backdraft self-burn,
Overload/beam-failure self-damage, foam slow penalty, boss adds/volleys.
"""
import argparse
import random
import sys

# ---------------- current game numbers (mirror ContentBuilder / scripts) ----------------
ENEMIES = {
    # name: (hp, dmg, cd, xp, scrap, minWave, weight, reduction)
    "Roach":      (14, 5, 0.9, 8, 1, 1, 3.0, 0.0),
    "Ant":        (8, 4, 0.7, 5, 1, 2, 2.5, 0.0),
    "Fly":        (10, 5, 0.8, 7, 1, 2, 2.0, 0.0),
    "Scorpion":   (25, 8, 1.2, 15, 1, 1, 2.0, 0.0),
    "Larva":      (9, 4, 1.0, 4, 0, 1, 2.0, 0.0),
    "Spider":     (20, 7, 1.0, 14, 1, 3, 1.6, 0.0),
    "Wasp":       (16, 6, 1.6, 16, 2, 4, 1.4, 0.0),
    "CinderRoach":(22, 6, 1.8, 18, 2, 5, 1.2, 0.0),
    "Webspinner": (30, 5, 2.0, 24, 2, 5, 1.1, 0.0),
    "Beetle":     (70, 12, 1.4, 30, 3, 6, 1.0, 0.5),
    "StormWasp":  (20, 5, 1.1, 22, 2, 7, 1.0, 0.0),
    "AcidBeetle": (90, 14, 1.5, 40, 4, 7, 0.8, 0.4),
    "SiegeBeetle":(220, 22, 1.8, 80, 8, 9, 0.5, 0.6),
}
BOSS_MID = {"hp": 520, "dmg": 16, "cd": 1.3, "xp": 120, "scrap": 20}
BOSS_FINAL = {"hp": 1100, "dmg": 20, "cd": 1.1, "xp": 300, "scrap": 50}

BASE_HP, BASE_DMG, BASE_CRIT_C, BASE_CRIT_M, BASE_ARMOR = 100.0, 5.0, 0.08, 1.5, 5.0
RAMP_XP = True  # late rooms pay more per kill
XP_BASE, XP_GROWTH, HEAL_ON_LEVEL = 60, 1.22, 0.12
ROOM_COUNT, MID_BOSS_ROOM = 25, 12
ROOM_OVERHEAD = 6.0  # travel/positioning per room, no damage
BASE_ENEMIES, PER_ROOM, MAX_ENEMIES, HP_PER_ROOM = 5, 0.8, 55, 0.08
DMG_PER_ROOM = 0.035  # enemy damage also scales: room 25 hits 1.5x

# weapon: single-target DPS as multiple of stats damage per second (measured from code)
WEAPONS = {
    # single-target DPS as multiple of stats damage/sec, plus typical simultaneous targets
    "flamethrower": (6.67, 2.0),  # full damage per 0.15s tick, cone
    "mop": (3.0, 4.2),           # full damage per 0.55s swing, wide arc + 3x sweet spot
    "cables": (4.55, 2.3),        # full damage per 0.22s + 40% chains
    "foam": (5.0, 2.0),           # 0.7x per 0.14s cone + slow + armor
    "beam": (6.0, 1.5),           # mixed taps + piercing full charges
    "darts": (4.0, 2.0),          # marks + bee swarm
}
# bot realism knobs
FIRING_UPTIME = 0.5    # fraction of clear time actually dealing damage
CONTACT_FRACTION = 0.6  # x0.85 enemy dmg folded into BOT_SKILL below  # fraction of time the max attacker pack is on the player
MAX_ATTACKERS = 5
BOT_SKILL = 0.5  # multiplies incoming damage (kiting, dash i-frames, knockback)

UPGRADES = [
    # id, weight-class, stacks, dpsGain(dmgMult, dmgAdd, critC, critM, atkMult), hpGain, notes
    ("attack_damage_20", 70, 0, (1.2, 0, 0, 0, 1.0), 0),
    ("sharpened_claws", 70, 0, (1.0, 5, 0, 0, 1.0), 0),
    ("heavy_caliber", 70, 2, (1.0, 10, 0, 0, 0.95), 0),
    ("precision_strike", 70, 3, (1.0, 0, 0.1, 0, 1.0), 0),
    ("quick_step", 70, 3, (1.0, 0, 0, 0, 1.0), 0),
    ("reinforced_suit", 70, 3, (1.0, 0, 0, 0, 1.0), 0),  # armor 10
    ("thick_skin", 70, 5, (1.0, 0, 0, 0, 1.0), 15),
    ("trigger_happy", 70, 3, (1.0, 0, 0, 0, 1.2), 0),
    ("vitality_boost", 70, 3, (1.0, 0, 0, 0, 1.0), 0),  # hpMult 1.15
    ("tactical_retreat", 70, 1, (1.0, 0, 0, 0, 1.0), 0),
    ("iron_will", 25, 2, (1.0, 0, 0, 0, 1.0), 25),  # +5 armor
    ("exoskeleton", 25, 2, (1.0, 0, 0, 0, 1.0), 10),  # +20 armor
    ("combat_stimulants", 25, 2, (1.0, 0, 0, 0, 1.15), 0),
    ("devastating_blows", 25, 2, (1.0, 0, 0, 0.5, 1.0), 0),
    ("efficient_systems", 70, 3, (1.0, 0, 0, 0, 1.0), 0),
    ("extended_magazine", 70, 2, (1.0, 0, 0, 0, 1.1), 0),
    ("overcharge", 25, 2, (1.25, 0, 0, 0, 1.0), 0),
    ("scavenger", 25, 2, (1.0, 0, 0, 0, 1.0), 0),
    ("adrenaline_surge", 25, 1, (1.0, 0, 0, 0, 1.0), 0),
    ("emergency_protocols", 25, 1, (1.0, 0, 0, 0, 1.0), 20),
    ("venom_rounds", 25, 1, (1.1, 8, 0, 0, 1.0), 0),
    ("momentum", 25, 1, (1.0, 0, 0, 0, 1.05), 0),
    ("berserker_rage", 5, 1, (1.15, 15, 0.05, 0.25, 1.1), 0),
    ("chain_lightning", 5, 0, (1.0, 0, 0, 0, 1.0), 0),
    ("executioner", 5, 1, (1.1, 0, 0.15, 0.75, 1.0), 0),
    ("explosive_finale", 5, 1, (1.0, 0, 0, 0, 1.0), 0),
    ("glass_cannon", 5, 1, (1.5, 0, 0.1, 0, 1.15), -25),
    ("juggernaut", 5, 1, (1.0, 0, 0, 0, 1.0), 50),  # hpMult 1.2 + armor
    ("life_leech", 5, 1, (1.0, 0, 0, 0, 1.0), 0),
    ("second_wind", 5, 1, (1.0, 0, 0, 0, 1.0), 0),
    ("time_dilation_field", 5, 1, (1.0, 0, 0, 0, 1.25), 0),
]


def armor_reduction(armor):
    return armor / (armor + 100.0)


def pick_table(wave):
    pool = [(n, w) for n, (hp, dmg, cd, xp, sc, mw, w, red) in ENEMIES.items() if mw <= wave]
    total = sum(w for _, w in pool)
    return pool, total


def room_count(index):
    return max(1, min(MAX_ENEMIES, round(BASE_ENEMIES + index * PER_ROOM)))


CHAR_HP = {"flamethrower": 100, "mop": 150, "cables": 100, "foam": 150, "beam": 85, "darts": 50}
EXPOSURE = {"flamethrower": 0.85, "mop": 1.05, "cables": 0.6, "foam": 0.7, "beam": 0.6, "darts": 0.5}


def simulate_run(seed, weapon="flamethrower", verbose=False):
    rng = random.Random(seed)
    char_hp = CHAR_HP[weapon]
    # player state: [dmgMult, dmgAdd, critC, critM, atkMult, hpMult, hpAdd, armor]
    mult, add, cc, cm, atk, hpm, hpa, armor = 1.0, 0.0, BASE_CRIT_C, BASE_CRIT_M, 1.0, 1.0, 0.0, BASE_ARMOR
    maxhp = float(char_hp)
    hp = maxhp
    level, xp, need = 1, 0, XP_BASE
    taken_stacks = {}
    scrap = 0
    kills = 0
    log = []
    wcoef, multi = WEAPONS[weapon]

    def dps():
        d = (BASE_DMG * mult + add) * atk  # attack speed now scales (after fix)
        return d * (1 + cc * (cm - 1)) * wcoef * multi * FIRING_UPTIME

    def apply(u):
        nonlocal mult, add, cc, cm, atk, hpm, hpa, armor, maxhp, hp
        uid, _, _, (dm, da, c, m, a), h = u
        mult *= dm
        add += da
        cc = min(1.0, cc + c)
        cm += m
        atk *= a
        if uid == "vitality_boost":
            hpm *= 1.15
        if uid == "juggernaut":
            hpm *= 1.2
            armor += 25
        if uid == "reinforced_suit":
            armor += 10
        if uid == "iron_will":
            armor += 5
        if uid == "exoskeleton":
            armor += 20
        if uid == "glass_cannon":
            armor -= 5
        hpa += h
        maxhp = max(1.0, (BASE_HP + hpa) * hpm)
        hp = min(hp + max(0, h), maxhp)

    def gain_xp(amount):
        nonlocal level, xp, need, hp, mult, atk
        xp += amount
        while xp >= need:
            xp -= need
            level += 1
            need = round(need * XP_GROWTH)
            hp = min(maxhp, hp + maxhp * HEAL_ON_LEVEL)
            if level in (10, 20):
                mult *= 1.3  # path spike
                atk *= 1.1
            else:
                offer()

    def offer(path_tier=0):
        nonlocal hp
        if path_tier:
            # path: fixed meaningful spike
            nonlocal mult, atk
            mult *= 1.3
            atk *= 1.1
            return
        pool = [u for u in UPGRADES if taken_stacks.get(u[0], 0) < u[2] or u[2] == 0]
        # weight by rarity class
        bag = []
        for u in pool:
            bag += [u] * (u[1] if u[1] > 0 else 1)
        choices = []
        for _ in range(min(3, len(pool))):
            c = rng.choice(bag)
            choices.append(c)
            bag = [x for x in bag if x != c]
        # greedy DPS pick
        def score(u):
            dm, da, c, m, a = u[3]
            base = BASE_DMG * mult + add
            newd = (BASE_DMG * mult * dm + add + da)
            return newd * dm * a / max(base, 0.01)
        best = max(choices, key=score)
        taken_stacks[best[0]] = taken_stacks.get(best[0], 0) + 1
        apply(best)

    for index in range(ROOM_COUNT):
        number = index + 1
        is_boss = number in (MID_BOSS_ROOM, ROOM_COUNT)
        hp_mult = 1 + index * HP_PER_ROOM
        # room type: 20% shop (heal, no fight), 25% elite of the rest, bosses fixed
        rtype = "combat"
        if not is_boss:
            if rng.random() < 0.2 and 3 < number < ROOM_COUNT - 1 and number not in (MID_BOSS_ROOM - 1, MID_BOSS_ROOM + 1):
                rtype = "shop"
            elif rng.random() < 0.25 and number > 2:
                rtype = "elite"
        if rtype == "shop":
            hp = min(maxhp, hp + maxhp * 0.4)
            log.append((number, level, 0, round(hp), 0))
            continue
        elite = 1.6 if rtype == "elite" else 1.0
        if is_boss:
            boss = BOSS_MID if number == MID_BOSS_ROOM else BOSS_FINAL
            ehp = boss["hp"] * hp_mult
            edps = boss["dmg"] / boss["cd"] * (1 + index * DMG_PER_ROOM)
            exv, esc = boss["xp"] * 1.5, boss["scrap"]
            count = 1
        else:
            count = room_count(index)
            # average enemy at this wave
            pool, total = pick_table(number)
            ehp = sum(ENEMIES[n][0] * (w / total) for n, w in pool) * hp_mult
            edps = sum(ENEMIES[n][1] / ENEMIES[n][2] * (w / total) for n, w in pool) * (1 + index * DMG_PER_ROOM)
            exv = sum(ENEMIES[n][3] * (1.0 + index * 0.1) * (w / total) for n, w in pool)
            esc = sum(ENEMIES[n][4] * (w / total) for n, w in pool)
        total_hp = ehp * count * elite
        clear_t = total_hp / max(dps(), 0.01) + ROOM_OVERHEAD
        # incoming: up to MAX_ATTACKERS in contact part of the time
        incoming = edps * min(count, MAX_ATTACKERS) * CONTACT_FRACTION * (1 - armor_reduction(armor)) * BOT_SKILL * EXPOSURE[weapon]
        hp -= incoming * (clear_t - ROOM_OVERHEAD)
        if rtype == "elite":
            hp = min(maxhp, hp + maxhp * 0.25)
        if hp <= 0:
            return {"died": number, "level": level, "clear_t": clear_t, "scrap": scrap, "log": log}
        gain_xp(exv * count)
        scrap += esc * count + 6
        if is_boss:
            scrap += 40
        log.append((number, level, round(clear_t, 1), round(hp), count))
    scrap += 80
    return {"died": 0, "level": level, "scrap": scrap, "log": log}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--runs", type=int, default=12)
    ap.add_argument("--table", action="store_true")
    args = ap.parse_args()
    for weapon in WEAPONS:
        deaths, levels, scraps = [], [], []
        for s in range(args.runs):
            r = simulate_run(1000 + s, weapon)
            deaths.append(r["died"])
            levels.append(r["level"])
            scraps.append(r["scrap"])
        import statistics as st
        print(f"{weapon:12} deaths={sum(1 for d in deaths if d)}/{args.runs} "
              f"dieRooms={sorted(set(deaths))} finalLvl={st.mean(levels):.1f} scrap={st.mean(scraps):.0f}")
        if args.table and weapon == "flamethrower":
            r = simulate_run(1000, weapon)
            print(" room lvl clearT hp")
            for row in r["log"][::3]:
                print(" ", row)


if __name__ == "__main__":
    main()
