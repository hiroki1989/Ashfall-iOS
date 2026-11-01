#!/usr/bin/env python3
"""Deterministic scenario estimates for the 2026-11-01 action-skill formulas.

Use only as code-based balancing evidence, not as measured combat performance.
The same 16-damage, 7.692-round/s common AR scales with level. We normalize
all damage to current weapon DPS, excluding criticals, reload, armor, enemy
resistance and elements. Turret occupancy/hit rate and grenade overlap are
assumptions, not observations. Additional passives that alter weapon DPS are
already canceled out by the normalization. Focus is optimistic at five stacks.
"""
from math import comb

DAMAGE, RATE, PELLETS = 16., 1/.13, 1
UPTIME, FALLOFF = .60, .80

# Explicit upgrade packages; middle spends 18 base ranks and 24 extra ranks,
# so at level 20 it needs 9 additional mission/collector points (42 total).
# End spends 75 base ranks and 60 extras = 135 points on each distinct tree.
SCENARIOS = [
    dict(name='early', level=1, path=0, cap=0, turret_dmg=0, turret_rate=0,
         turret_duration=0, focus=0, second=0, nade_damage=0, nade_count=0,
         nade_radius=0, medic_heal=0, medic_duration=0, medic_damage=False, probability=.85),
    dict(name='middle', level=20, path=1, cap=0, turret_dmg=.165, turret_rate=.11,
         turret_duration=2.3, focus=0, second=0, nade_damage=.165, nade_count=2,
         nade_radius=2.2, medic_heal=.19, medic_duration=3.45, medic_damage=False, probability=.65),
    dict(name='late', level=123, path=4, cap=1, turret_dmg=.445, turret_rate=.315,
         turret_duration=7.2, focus=.05, second=1, nade_damage=.525, nade_count=4,
         nade_radius=6.3, medic_heal=.60, medic_duration=11.2, medic_damage=True, probability=.50),
]

def level_steps(level):
    s=max(0,level-1)
    return s+2*s*s/122

def expected_attenuated_hits(count, probability):
    # Each hit on the same enemy is scaled by 1/(1+0.4*previousHits).
    return sum(comb(count,k)*probability**k*(1-probability)**(count-k)
               *sum(1/(1+.4*j) for j in range(k)) for k in range(count+1))

def calculate(s):
    p,c=s['path'],s['cap']
    dmg=DAMAGE*(1+.24*level_steps(s['level']))
    dps=dmg*RATE*PELLETS
    t_duration=min(22,12+p*1.5+c*5+s['turret_duration'])
    base=(.5+.07*p+.12*c)*(1+s['turret_dmg'])*(1+5*s['focus'])
    t_power=min(.95,base)+(min(.665,.7*base) if s['second'] else 0)
    t_rate=min(1.35,1+.06*p+s['turret_rate'])
    t_damage=t_duration*t_power*t_rate*UPTIME
    count=min(12,1+int(p)+3*c+s['nade_count'])
    radius=min(7,2.7+p*.35+c*1.25+s['nade_radius'])
    per_hit=max(18,dps/7.7)*(20+p+3*c)*(1+s['nade_damage'])/dps
    # Additional 25% cluster hit assumes the target is still alive.
    grenade=expected_attenuated_hits(count,s['probability'])*per_hit*FALLOFF*(1+.25*c)
    m_duration=min(24,12+p*1.5+c*5+s['medic_duration'])
    heal=min(.09,(.028+p*.006+c*.025)*(1+s['medic_heal']))
    medic_damage=m_duration*.06 if s['medic_damage'] else 0
    return dps,t_duration,t_damage,count,radius,grenade,grenade*3,m_duration,heal*100,m_duration*heal*100,medic_damage

def main():
    print('COMMON AR 16/shot, 7.692 rounds/s, 1 pellet; weapon DPS excludes reload/crit/elements.')
    print('Turret: 60% occupancy (target alive + visible + projectile hits); full focus sustained if available.')
    print('Grenade: independent per-blast hit probabilities 85%/65%/50%, mean falloff 80%, target survives cluster.')
    print('Player level 20 middle package needs 9 mission/collection points beyond level points.')
    print('Skill outputs are raw DPS-seconds/activation; enemy resist and overkill losses excluded.')
    print('scenario lvl gunDPS turret_dur turret_DPSsec nade_count radius nade_1 nade_3 medic_dur heal_pct_s heal_total_pct medic_DPSsec')
    for s in SCENARIOS:
        x=calculate(s)
        print(f"{s['name']:>8} {s['level']:3} {x[0]:8.1f} {x[1]:10.1f} {x[2]:13.2f} {x[3]:10} {x[4]:6.2f} {x[5]:6.2f} {x[6]:6.2f} {x[7]:9.2f} {x[8]:10.2f} {x[9]:14.2f} {x[10]:12.2f}")
    print('Recharge: 12.99s at 100% pellet hit fraction, 25.97s at 50%; charge bonuses excluded.')
    print('Turret duration exceeding recharge cannot be counted as overlapping: recast removes the existing device.')
    print('Medic healing assumes standing in the field for full duration and sufficient missing HP; debuff excludes cancelling an active wind-up.')
    print('Probability-independent grenade model is optimistic for clustered 3 targets, and exact for the stated independent-hit model.')
    print('For enemies alive and visible for the entire field, medic DoT has 1.5s ticks; timing may produce +/- one tick.')
    for level in (1,20,60,123):
        steps=level_steps(level);weapon=1+.24*steps;foe=1+.21*steps
        print(f'Lv{level}: common weapon x{weapon:.3f}; enemy health/attack x{foe:.3f}; relative x{weapon/foe:.3f}')

if __name__=='__main__': main()
