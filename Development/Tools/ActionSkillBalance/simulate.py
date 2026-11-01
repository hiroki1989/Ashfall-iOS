#!/usr/bin/env python3
"""Deterministic action-skill estimate, not a substitute for playtesting.

Equations mirror ActionSkillCombat.cs as inspected on 2026-11-01.  Reload,
critical hits, enemy resistance, weapon/element affixes and later additions are
excluded. Input DPS assumes that every projectile connects. Uptime and blast
coverage are explicit estimates and are not measured gameplay values.
"""
from dataclasses import dataclass

@dataclass(frozen=True)
class Build:
    name: str
    path: float
    cap: int
    turrets: int
    grenade_hit_probability: float

BUILDS = [Build('early', 0, 0, 1, .85), Build('middle', 1, 0, 1, .65), Build('late', 4, 1, 2, .50)]
DAMAGE, RATE, PELLETS = 16.0, 1/.13, 1
UPTIME, FALLOFF = .60, .80

def level_steps(level):
    n = max(0, level - 1)
    return n + 2*n*n/122

def level_power(level): return 1 + .24*level_steps(level)
def enemy_scale(level): return 1 + .21*level_steps(level)

def metrics(level, b):
    damage = DAMAGE*level_power(level)
    dps = damage*RATE*PELLETS
    duration = 12+b.path*1.5+b.cap*5
    turret_shot = .5+b.path*.13+b.cap*.55
    turret_rate = 1+b.path*.10
    turret_raw = duration*b.turrets*turret_shot*turret_rate
    turret_adjusted = turret_raw*UPTIME
    count = min(12, 1+int(b.path)+3*b.cap)
    grenade_damage = max(18, dps/7.7)*(9+b.path+3*b.cap)
    grenade_single = count*grenade_damage*(1+.25*b.cap)*b.grenade_hit_probability*FALLOFF/dps
    heal_percent = (0.028+b.path*.006+b.cap*.025)*duration*100
    field_dps_percent = .02/RATE*100
    return damage,dps,duration,turret_raw,turret_adjusted,count,grenade_single,grenade_single*3,heal_percent,field_dps_percent

def proposed_maxed():
    # Full 3-branch build, with all primary extra ranks, before passives from
    # other trees. These totals were enumerated from the 2026-11-01 catalog.
    turret_damage_extra, turret_rate_extra, turret_duration_extra = .445, .315, 7.2
    grenade_damage_extra, grenade_count_extra = .525, 4
    medic_heal_extra, medic_duration_extra = .60, 11.2
    focus = 1.25  # optimistic five consecutive shots against the same target
    duration = 12 + 4*1.5 + 5 + turret_duration_extra
    # Proposed values, not an assertion that they are already implemented.
    shot = min(1.10, (.5 + 4*.07 + .12) * (1+turret_damage_extra) * focus)
    rate = min(1.35, 1+4*.06+turret_rate_extra)
    turret_seconds = duration * shot * rate * 1.70 * UPTIME
    grenade_count = min(12, 1+4+3+grenade_count_extra)
    blast = (20+4+3)/7.7 * (1+grenade_damage_extra) * 1.25
    effective_count = 1 + (grenade_count-1)*.40
    single_blast = effective_count*blast*.50*FALLOFF
    heal_seconds = min(24, 12+4*1.5+5+medic_duration_extra)
    heal_pct = min(.08, (.028+4*.006+.025)*(1+medic_heal_extra))*heal_seconds*100
    print('\nPROPOSAL ONLY (all three branches and primary extra nodes maxed):')
    print(f'turret: {turret_seconds:.2f} DPS seconds / {duration:.1f}s (weapon coefficient cap 1.1, cadence 1.35, second turret 70%)')
    print(f'grenades: {single_blast:.2f} DPS seconds / target, {single_blast*3:.2f} / three targets (12 total; later grenades 40%, hit 50%)')
    print(f'medic: {heal_pct:.1f}% HP maximum / {heal_seconds:.1f}s; field damage {heal_seconds*.06:.2f} DPS seconds / enemy (6% DPS/s)')
    print('These proposal estimates exclude element, critical effects, skill interactions and live play.')

def main():
    print('Assumptions: common AR = 16 damage, 7.692 rounds/s, 1 pellet; no reload/affixes/crit; enemy resist excluded.')
    print('Turret uptime 60%; grenade hit probability early/middle/late 85%/65%/50%; blast falloff 80%.')
    print('Late grenade assumes each affected target survives to receive the secondary hit; optimistic.')
    print('Recharge at 100%% pellet hits: 100/7.7 = %.2f s. At 50%% pellet hits: %.2f s.'%(100/7.7,200/7.7))
    print('level build  damage     DPS duration turret_raw turret_60pct grenades blast_1 blast_3 heal%% field_DPS%%')
    for level in (1, 20, 60, 123):
        for b in BUILDS:
            damage,dps,duration,tr,ta,count,g1,g3,heal,field=metrics(level,b)
            print(f'{level:>5} {b.name:>6} {damage:8.1f} {dps:8.1f} {duration:8.1f} {tr:10.2f} {ta:12.2f} {count:8} {g1:7.2f} {g3:7.2f} {heal:6.1f} {field:10.3f}')
    print('Weapon/foe level curves at common rarity (foe HP and attack use the same multiplier):')
    for level in (1,20,60,123):
        print(f'{level:3}: weapon x{level_power(level):.3f}; enemy x{enemy_scale(level):.3f}; relative x{level_power(level)/enemy_scale(level):.3f}')
    print('Notes: Turret duration can exceed recharge; recasting replaces the previous device.')
    print('Blast_3 is three times blast_1, assuming three equally covered enemies. Values are seconds of weapon DPS per activation.')
    print('The minimum grenade damage floor makes low-DPS weapons disproportionately strong.')
    print('Medics heal at max-HP percentage per second; this is maximum theoretical healing, not expected effective healing.')
    print('Area debuff interrupts telegraphed attacks every 1.5 seconds; effectiveness depends on AI timing, not quantified here.')
    proposed_maxed()

if __name__ == '__main__': main()

