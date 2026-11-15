// Written with Claude Code (AI), directed and tested by team MARSXR
// Checks content.js before the defense. Run from pitch/presentacion:   node tools/check-content.js
// It fails (exit code 1) when: a {key} does not exist; the seconds do not add up to 180; the demo is not at
// 1:05-1:50 (moved from 1:00-1:45 on 15 Nov, 03:13, with the shorter text); a slide asks for more than 2.6 words per second; Spanish and English do not have the same fields;
// a figure of the plan is typed by hand on a slide instead of {key}; a figure said in the notes is not the model's;
// a phrase the team's rules forbid is on a slide; or the title slide lacks the team or the challenge.
// It only warns (it does not fail) while the title slide's "members" is empty: the names come from Daniel.
'use strict';
var path = require('path');
var dir = process.argv[2] || path.join(__dirname, '..');
global.window = {};
require(path.join(dir, 'data', 'gale.js'));
require(path.join(dir, 'data', 'stops.js'));
require(path.join(dir, 'content.js'));
var M = require(path.join(dir, 'walkback-model.js'));
var PH = require(path.join(dir, 'placeholders.js'));
var C = global.window.WALKBACK_CONTENT, failed = 0, warned = 0;
function check(name, ok, detail) { console.log((ok ? 'PASS  ' : 'FAIL  ') + name + (detail ? '  [' + detail + ']' : '')); if (!ok) failed++; }
function warn(text) { console.log('WARN  ' + text); warned++; }

var comp = PH.computed(M, global.window.GALE, global.window.GALE_STOPS);
var map = { es: PH.values(M, comp, C.FACTS, 'es'), en: PH.values(M, comp, C.FACTS, 'en') };

// 1. every {key} exists
var missing = { es: [], en: [] }, filled = { es: [], en: [] };
C.slides.forEach(function (s) { ['es', 'en'].forEach(function (l) { filled[l].push(PH.fillDeep(s[l], map[l], missing[l])); }); });
var backup = { es: PH.fillDeep(C.backup.es, map.es, missing.es), en: PH.fillDeep(C.backup.en, map.en, missing.en) };
check('every {key} used in Spanish exists', missing.es.length === 0, missing.es.join(', '));
check('every {key} used in English exists', missing.en.length === 0, missing.en.join(', '));

// 2. time
var total = 0, at = 0, demoStart = -1, demoSeconds = 0;
C.slides.forEach(function (s) { if (s.id === 'demo') { demoStart = at; demoSeconds = s.segundos; } at += s.segundos; total += s.segundos; });
check('the seconds add up to 180', total === 180, total + ' s');
check('the live demo is at 1:05-1:50', demoStart === 65 && demoSeconds === 45, 'starts at ' + demoStart + ' s, lasts ' + demoSeconds + ' s');
check('seven slides counting the title (NASA: 7 at most)', C.slides.length === 7 && C.slides[0].id === 'title' && C.slides[0].segundos === 0, C.slides.length + ' slides');

// 3. pace of what Daniel says (text in round brackets is a stage note, not spoken)
var MAX_WORDS_PER_SECOND = 2.6, slow = [];
C.slides.forEach(function (s) {
  if (!s.segundos) return;
  var words = String(s.es.dice || '').replace(/\([^)]*\)/g, ' ').split(/\s+/).filter(function (w) { return /[A-Za-zÁÉÍÓÚÜÑáéíóúüñ0-9]/.test(w); }).length;
  var spoken = s.id === 'closing' ? s.segundos - 5 : s.segundos;          // the last 5 s of the closing are silent (QR)
  var pace = words / spoken;
  console.log('INFO  ' + (s.id + '        ').slice(0, 9) + String(s.segundos).padStart(3) + ' s  ' + String(words).padStart(3) + ' words  ' + pace.toFixed(2) + ' words/s');
  if (pace > MAX_WORDS_PER_SECOND) slow.push(s.id + ' ' + pace.toFixed(2));
  if (!words) slow.push(s.id + ' has nothing to say');
});
check('no slide asks for more than ' + MAX_WORDS_PER_SECOND + ' words per second', slow.length === 0, slow.join('; '));

// 4. Spanish and English have the same fields (creditLocal is Spanish only on purpose: the defense laptop's
//    local AI illustration on the closing slide, never in the English version for NASA)
var ES_ONLY = { creditLocal: true };
var uneven = [];
C.slides.forEach(function (s) {
  var esKeys = Object.keys(s.es).filter(function (k) { return !ES_ONLY[k]; });
  var a = esKeys.sort().join(','), b = Object.keys(s.en).sort().join(',');
  var onlyEs = esKeys.filter(function (k) { return !(k in s.en); }), onlyEn = Object.keys(s.en).filter(function (k) { return !(k in s.es); });
  if (a !== b) uneven.push(s.id + (onlyEs.length ? ' only es: ' + onlyEs.join('/') : '') + (onlyEn.length ? ' only en: ' + onlyEn.join('/') : ''));
});
check('Spanish and English slides have the same fields', uneven.length === 0, uneven.join('; '));
var emptyEn = C.slides.filter(function (s) { return s.segundos && !String(s.en.dice || '').trim(); }).map(function (s) { return s.id; });
if (emptyEn.length) warn('English speaker notes are empty on: ' + emptyEn.join(', ') + ' (fine for a PDF; write them before recording a video)');

// NASA asks the slides for the team's name, its members' names and the challenge: the title slide carries them.
var title = C.slides[0] || { es: {}, en: {} };
check('the title slide names the team (MARSXR) and the challenge in both languages',
  ['es', 'en'].every(function (l) { return /MARSXR/.test(title[l].tag || '') && /Interplanetary Survival Guide: Martian Map/.test(title[l].challenge || ''); }));
['es', 'en'].forEach(function (l) {
  if (!String(title[l].members || '').trim()) warn('title slide (' + l + '): "members" is empty, so no names show; NASA asks for the members\' names on the slides (Daniel gives them; never invent them)');
});

var ui = C.ui || { es: {}, en: {} };
check('the interface texts (ui) exist in both languages with the same fields', Object.keys(ui.es).length > 0 && Object.keys(ui.es).sort().join(',') === Object.keys(ui.en).sort().join(','),
  Object.keys(ui.es).length + ' texts');
// {reach}, {red} and {stops} inside ui are filled by the deck while the WHAT IF runs: they are not placeholders.js keys.

// 5. figures of the plan typed by hand on a slide (everything shown except the speaker notes)
function shown(block) { var out = []; (function walk(n, key) { if (typeof n === 'string') { if (key !== 'dice') out.push(n); } else if (Array.isArray(n)) n.forEach(function (x) { walk(x, key); }); else if (n && typeof n === 'object') Object.keys(n).forEach(function (k) { walk(n[k], k); }); })(block, ''); return out; }
var planFigures = [map.es.km, map.en.km, map.es.meters, map.es.walk, map.es.work, map.es.total, map.es.usedL + ' L', map.es.spareL + ' L', map.es.usedPct + ' %',
  map.es.red8, map.en.red8, map.es.reach8, map.en.reach8, map.es.farStopMin, map.en.farStopMin];
var typed = [];
C.slides.forEach(function (s) { ['es', 'en'].forEach(function (l) { shown(s[l]).forEach(function (text) {
  planFigures.forEach(function (f) { if (text.indexOf(f) >= 0) typed.push(s.id + '/' + l + ': "' + f + '"'); }); }); }); });
check('no figure of the plan is typed by hand on a slide (use {key})', typed.length === 0, typed.join('; '));

// 6. figures written as digits in the speaker notes must be the model's
var wrong = [];
C.slides.forEach(function (s) { ['es', 'en'].forEach(function (l) {
  var t = String(s[l].dice || ''), m;
  var re = /(\d+[.,]\d+) km|(\d+ h \d+ min)|(\d+) % del oxígeno|(\d+) % of the oxygen/g;
  while ((m = re.exec(t))) {
    if (m[1] && m[1] + ' km' !== map.es.km && m[1] + ' km' !== map.en.km) wrong.push(s.id + '/' + l + ': ' + m[0]);
    if (m[2] && m[2] !== map.es.total && m[2] !== map.es.work) wrong.push(s.id + '/' + l + ': ' + m[0]);
    if ((m[3] || m[4]) && (m[3] || m[4]) !== map.es.usedPct) wrong.push(s.id + '/' + l + ': ' + m[0]);
  }
}); });
check('the figures written as digits in the speaker notes are the model\'s', wrong.length === 0, wrong.join('; '));

// 7. the team's rules, on everything shown and said
var forbidden = [
  [/compatible/i, 'says "compatible" (rule 4: not "compatible with NASA SUITS")'],
  [/PLACEHOLDER/, 'says PLACEHOLDER (the app says ESTIMATE)'],
  [/TEST VALUES|valores de prueba/i, 'says test values'],
  [/ilustraci[oó]n IA|AI-generated|generad[oa] con IA/i, 'credits an AI-made image (rule 3: none on the slides)'],
  [/del mapa (queda |est[aá] )?en rojo|of the map (is|turns) red/i, 'says "of the map" for the red share (it is of the ground that has a way back)']
];
var broken = [];
function everything(block) { var out = []; (function walk(n) { if (typeof n === 'string') out.push(n); else if (Array.isArray(n)) n.forEach(walk); else if (n && typeof n === 'object') Object.keys(n).forEach(function (k) { walk(n[k]); }); })(block); return out; }
C.slides.forEach(function (s) { ['es', 'en'].forEach(function (l) { everything(s[l]).forEach(function (text) {
  forbidden.forEach(function (f) { if (f[0].test(text)) broken.push(s.id + '/' + l + ' ' + f[1]); }); }); }); });
check('nothing the rules forbid is on a slide or in the notes', broken.length === 0, broken.join('; '));
// The red share is "of the ground with a way back on foot" (CLAUDE.md, phrasing that must stay right).
var notOnFoot = [];
function redTexts(block, l, where) { everything(block).forEach(function (text) {
  if (/\{(red8|red8En|red8Es|red)\}/.test(text) && !(l === 'es' ? /con vuelta a pie/ : /with a way back on foot/).test(text)) notOnFoot.push(where + '/' + l); }); }
C.slides.forEach(function (s) { ['es', 'en'].forEach(function (l) { redTexts(s[l], l, s.id); }); });
['es', 'en'].forEach(function (l) { redTexts((C.ui || {})[l] || {}, l, 'ui'); });
check('the red share always says "of the ground with a way back on foot" (never "of the map")', notOnFoot.length === 0, notOnFoot.join(', '));
var realtime = [];
C.slides.forEach(function (s) { ['es', 'en'].forEach(function (l) { everything(s[l]).forEach(function (text) { if (/tiempo real|real[- ]time/i.test(text)) realtime.push(s.id + '/' + l); }); }); });
check('"real time" is not claimed on any slide (only the backup answer explains DONKI)', realtime.length === 0, realtime.join(', '));
check('the backup answer about real time names DONKI, (live) and (stored in the app), and says it does not feed the oxygen',
  ['es', 'en'].every(function (l) { var t = backup[l].realtime; return /DONKI/.test(t) && /\(live\)/.test(t) && /\(stored in the app\)/.test(t) && /(oxígeno|oxygen)/.test(t); }));

// 8. every figure of the plan on a slide stands next to ESTIMADO / ESTIMATE on the same slide
var noLabel = [];
C.slides.forEach(function (s) { ['es', 'en'].forEach(function (l) {
  var raw = shown(s[l]).join(' | ');
  var usesPlan = /\{(km|kmEn|meters|walk|work|total|usedL|usedPct|spareL|red8|red8En|reach8|farStopMin)\}/.test(raw);
  if (usesPlan && !/ESTIMAD[OA]|ESTIMATE/.test(raw)) noLabel.push(s.id + '/' + l);
}); });
check('every slide that shows a figure of the plan says ESTIMADO / ESTIMATE', noLabel.length === 0, noLabel.join(', '));

// 9. the numbers themselves
check('the model gives the app\'s numbers (1.8 km, 2 h 55 min, 36 %, 285 L, 57.5 % red at 8 %)', map.en.km === '1.8 km' && map.es.km === '1,8 km' && map.es.total === '2 h 55 min' &&
  map.es.usedPct === '36' && map.es.spareL === '285' && map.es.red8 === '57,5' && map.en.red8 === '57.5' && map.es.verdict === 'VIABLE' && map.es.verdict8 === 'NOT VIABLE',
  map.es.km + ' · ' + map.es.total + ' · ' + map.es.usedPct + ' % · ' + map.es.spareL + ' L · ' + map.es.red8 + ' %');

console.log('INFO  keys available: ' + Object.keys(map.es).sort().join(' '));
console.log(failed === 0 ? 'ALL PASSED' + (warned ? ' (' + warned + ' warning' + (warned > 1 ? 's' : '') + ')' : '') : failed + ' FAILED');
process.exit(failed === 0 ? 0 : 1);
