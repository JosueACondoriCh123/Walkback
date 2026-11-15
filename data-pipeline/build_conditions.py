# Written with Claude Code (AI), directed and tested by team MARSXR
"""Writes Site/conditions.json (ficha C): the latest REMS report, the season and daylight, the
surface radiation and the week's space weather, every value with its date and source.

Inputs (download them first, see DATA.md):
  msl_weather.json  NASA's Curiosity weather feed (REMS data produced by CAB, CSIC-INTA)
  donki_sep.json, donki_gst.json, donki_cme.json  NASA DONKI for the week

Usage: python -I build_conditions.py <cache/rems dir> <today's sol> <DONKI from> <DONKI to> <checked UTC> <out json>

Season and daylight: today's REMS report does not exist yet, so they come from the REMS record of
the same season one Mars year earlier (668.6 sols before today's sol): measured, not calculated.
"""
import json
import os
import sys

MARS_YEAR_SOLS = 668.6

src, today, d_from, d_to, checked, out = sys.argv[1], int(sys.argv[2]), sys.argv[3], sys.argv[4], sys.argv[5], sys.argv[6]
soles = json.load(open(os.path.join(src, 'msl_weather.json'), encoding='utf-8'))['soles']
by_sol = {int(s['sol']): s for s in soles if s.get('sol', '').isdigit()}


def num(s, key):
    v = s.get(key, '--')
    return float(v) if v not in ('--', '', None) else None


# 1. The latest REMS report that has temperatures and pressure.
latest = None
for sol in sorted(by_sol, reverse=True):
    s = by_sol[sol]
    if num(s, 'min_temp') is not None and num(s, 'pressure') is not None:
        latest = s
        break
age = today - int(latest['sol'])
rems = {
    'sol': int(latest['sol']),
    'earthDate': '%s, %d sols old' % (latest['terrestrial_date'], age),
    'airMinC': num(latest, 'min_temp'), 'airMaxC': num(latest, 'max_temp'),
    'groundMinC': num(latest, 'min_gts_temp'), 'groundMaxC': num(latest, 'max_gts_temp'),
    'pressurePa': num(latest, 'pressure'),
    'source': 'NASA/JPL-Caltech/CAB',
}

# 2. Same season one Mars year earlier.
target = round(today - MARS_YEAR_SOLS)
year_ago = min((sol for sol in by_sol if by_sol[sol].get('sunrise', '--') != '--'), key=lambda sol: abs(sol - target))
y = by_sol[year_ago]
ls = int(y['ls'])
season = 'early southern autumn, the low-dust season' if ls < 45 else ('southern autumn' if ls < 90 else None)
if season is None:
    raise SystemExit('season text only written for Ls 0-90; Ls is %d' % ls)

# 3. DONKI: solar energetic particles and geomagnetic storms in the week.
sep = json.load(open(os.path.join(src, 'donki_sep.json'), encoding='utf-8'))
gst = json.load(open(os.path.join(src, 'donki_gst.json'), encoding='utf-8'))
cme = json.load(open(os.path.join(src, 'donki_cme.json'), encoding='utf-8'))
events = [{'type': 'Solar particle event', 'time': e.get('eventTime', '')} for e in sep]

conditions = {
    'rems': rems,
    'season': {'value': 'Ls %d°, %s' % (ls, season), 'source': 'REMS, a Mars year ago'},
    'daylight': {'value': '%s to %s local time' % (y['sunrise'], y['sunset']), 'source': 'REMS sol %d' % year_ago},
    'radiation': {'value': '0.64 mSv/day', 'source': 'Curiosity RAD'},
    'donki': {
        'from': d_from, 'to': d_to,
        'events': events if events else 'no solar particle events',
        'checked': checked,
        'geomagneticStorms': len(gst),
        'coronalMassEjections': len(cme),
    },
    'todaySol': today,
}
json.dump(conditions, open(out, 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print(json.dumps(conditions, indent=1, ensure_ascii=False))
