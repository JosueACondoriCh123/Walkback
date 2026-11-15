// Written with Claude Code (AI), directed and tested by team MARSXR
// Checks the JavaScript port against what the app's own test reports on Gale (CLAUDE.md, "Demo, WHAT IF" notes).
// Run from pitch/presentacion:   node tools/check-model.js
// Exit code 0 = every check passed.
'use strict';
var path = require('path');
var dir = process.argv[2] || path.join(__dirname, '..');
global.window = {};
require(path.join(dir, 'data', 'gale.js'));
require(path.join(dir, 'data', 'stops.js'));
var M = require(path.join(dir, 'walkback-model.js'));
var G = global.window.GALE, STOPS = global.window.GALE_STOPS, failed = 0;
function check(name, ok, detail) { console.log((ok ? 'PASS  ' : 'FAIL  ') + name + (detail ? '  [' + detail + ']' : '')); if (!ok) failed++; }

var t0 = Date.now();
var site = M.siteFromGale(G); site.stops = STOPS; var slope = M.slopeDegrees(site), next = new Int32Array(site.n * site.n);
var back = M.returnMinutes(site, slope, next);
var ms = Date.now() - t0;
var full = M.whatIf(back, 100), w15 = M.whatIf(back, 15), w8 = M.whatIf(back, 8);
var over = 0; for (var i = 0; i < slope.length; i++) if (slope[i] > M.SLOPE_LIMIT_DEGREES) over++;

check('grid is 257 x 257 over 1000 m', site.n === 257 && site.side === 1000);
check('Tobler factor is 1 on flat ground and under 1 on any slope', Math.abs(M.slopeFactor(0) - 1) < 1e-6 && M.slopeFactor(0.2) < 1 && M.slopeFactor(-0.3) < 1);
check('reach: a full tank covers hours, 15 % about 42 min, 8 % about 8.5 min', M.reachMinutes(100) > 400 && Math.abs(M.reachMinutes(15) - 42.0) < 0.2 && Math.abs(M.reachMinutes(8) - 8.5) < 0.1,
  M.reachMinutes(15).toFixed(1) + ' / ' + M.reachMinutes(8).toFixed(1) + ' min');
var campo = STOPS.filter(function (s) { return s.id === 'campo-marte'; })[0];
check('four stops, and the farthest one (Campo Marte) is 14.7 min from the habitat (app: 14.7)', STOPS.length === 4 && !!campo &&
  Math.abs(back[M.nodeOf(site, campo.x, campo.y)] - 14.7) <= 0.15, campo ? back[M.nodeOf(site, campo.x, campo.y)].toFixed(2) + ' min' : 'no campo-marte in stops.js');
check('full tank: no red', full.beyond === 0);
check('tank at 15 %: no red (app: NO red on the map)', w15.beyond === 0, w15.redPercent.toFixed(1) + ' %');
check('tank at 8 %: 57.5 % of the ground with a way back is red, within 2 points (app: 57.5 %)', Math.abs(w8.redPercent - 57.5) <= 2, w8.redPercent.toFixed(1) + ' %');
check('the habitat itself is under a minute away and every stop has a way back', back[M.nodeOf(site, site.habitat.x, site.habitat.y)] < 1 &&
  site.stops.every(function (s) { return isFinite(back[M.nodeOf(site, s.x, s.y)]); }),
  site.stops.map(function (s) { return s.name + ' ' + back[M.nodeOf(site, s.x, s.y)].toFixed(1) + ' min'; }).join(', '));
check('the whole map is computed in under 1 s on this machine', ms < 1000, ms + ' ms');

// The plan (MissionFallback.Make): the numbers said in the defense, as the app's test checks them.
function mins(m) { m = Math.round(m); return m < 60 ? m + ' min' : Math.floor(m / 60) + ' h ' + (m % 60) + ' min'; }
var P = M.plan(site, slope, STOPS, 100), D = M.describe(P);
check('plan, full tank: 1.8 km (1812 m), 43 min walking, 2 h 12 min of work, 2 h 55 min in all', Math.round(P.walkMeters) === 1812 && mins(P.walkMinutes) === '43 min' &&
  mins(P.workMinutes) === '2 h 12 min' && mins(P.totalMinutes) === '2 h 55 min', Math.round(P.walkMeters) + ' m, ' + mins(P.walkMinutes) + ', ' + mins(P.workMinutes) + ', ' + mins(P.totalMinutes));
check('plan, full tank: 181 L = 36 % of the tank used, 285 L spare, VIABLE', Math.round(P.usedLiters) === 181 && Math.round(P.usedPercent) === 36 && Math.round(P.spareLiters) === 285 &&
  P.viable && D.headline === 'VIABLE', D.summary);
check('plan: the grid way was 1887 m before the straight shortcuts (CLAUDE.md)', Math.round(P.gridMeters) === 1887, Math.round(P.gridMeters) + ' m, ' + P.shortcuts + ' shortcuts');
check('plan: five legs, the first one is 200 m to Altadena', P.legs.length === 5 && Math.round(P.legs[0].meters) === 200 && P.legs[0].toName === 'Altadena');
var P15 = M.plan(site, slope, STOPS, 15), P8 = M.plan(site, slope, STOPS, 8);
check('WHAT IF at 15 % and at 8 %: NOT VIABLE, same route', !P15.viable && !P8.viable && M.describe(P8).headline === 'NOT VIABLE' && Math.round(P8.walkMeters) === 1812,
  '15 %: ' + M.describe(P15).summary + ' | 8 %: ' + M.describe(P8).summary);
check('WHAT IF at 8 %: Altadena, Valle de la Luna and Nevado Sajama are inside the reach, Campo Marte is in the red', STOPS.every(function (s) {
  return (back[M.nodeOf(site, s.x, s.y)] > w8.reachMinutes) === (s.id === 'campo-marte'); }));
var v1 = M.visorAtStop(site, P8, 0), v2 = M.visorAtStop(site, P8, 1), vFull = M.visorAtStop(site, P, 3);
check('visor, mission started at 8 %: no TURN BACK NOW at the first stop, TURN BACK NOW at the second (app: after the second jump)', !v1.turnBack && v2.turnBack,
  v1.stopName + ': O2 ' + v1.oxygenLeftPercent.toFixed(0) + ' %, back ' + v1.returnMinutes.toFixed(1) + ' min, margin ' + v1.marginMinutes.toFixed(1) + ' min | ' + v2.stopName + ': O2 ' + v2.oxygenLeftPercent.toFixed(0) + ' %');
check('visor, full tank at the last stop: the way back is covered (green) and it is the 14.7 min walk', !vFull.turnBack && vFull.stateName === 'covered' && Math.abs(vFull.returnMinutes - 14.7) < 0.15,
  'O2 ' + vFull.oxygenLeftPercent.toFixed(0) + ' %, next ' + vFull.nextName + ' ' + vFull.nextMeters.toFixed(0) + ' m');
var prof = M.profile(site, P);
check('height profile: one point per path point, and the route climbs tens of meters', prof.length === P.path.length && Math.max.apply(null, prof.map(function (p) { return p.height; })) > 50,
  Math.max.apply(null, prof.map(function (p) { return p.height; })).toFixed(0) + ' m between the lowest and the highest point of the route');
console.log('INFO  ' + (over / slope.length * 100).toFixed(1) + ' % of the nodes are over 20 degrees; ' + full.noWayBack + ' nodes have no way back; ' + STOPS.length + ' stops in stops.js');
console.log(failed === 0 ? 'ALL PASSED' : failed + ' FAILED');
process.exit(failed === 0 ? 0 : 1);
