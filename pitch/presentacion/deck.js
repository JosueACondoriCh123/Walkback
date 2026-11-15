// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch · the deck: builds every slide from content.js, fills every {key} with placeholders.js (the app's
// own rule, walkback-model.js), runs one 3D scene at a time, and handles the keys.
// Keys: arrows / space / click = next · N notes · T clock (it starts by itself when leaving the title; T restarts it)
//       F full screen · A annex · R demo backup (slide DEMO) · V local video (slide DEMO) · L Spanish / English
//       P print to PDF (7 pages, the demo in its final WHAT IF state) · Home / End
// URL:  ?lang=en opens in English · ?print=1 lays the 7 print pages out at load (for --print-to-pdf) · #N opens slide N
(function () {
  'use strict';
  var C = window.WALKBACK_CONTENT, M = window.WalkbackModel, S = window.WalkbackScenes, PH = window.WalkbackPlaceholders;
  var WG = window.WalkbackGale;
  var reduced = !!(window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches);
  var params = new URLSearchParams(location.search);
  var printMode = params.get('print') === '1';
  var lang = params.get('lang') === 'en' ? 'en' : (C.lang || 'es');
  var root = document.documentElement;
  if (printMode) root.classList.add('print-mode');

  // ------------------------------------------------------------------ the model (same numbers as the app), once
  var comp = PH.computed(M, window.GALE, window.GALE_STOPS);          // { site, slope, back, plan, plan15, plan8, w8, w15, full }
  var map = PH.values(M, comp, C.FACTS, lang);
  window.deck = { model: comp };

  // Defense laptop only: an AI illustration kept OUTSIDE the repository (img/local/ is in .gitignore). When the file
  // is there, the Spanish closing shows it (marked and credited, see templates.closing); when it is not, nothing
  // changes. The repository, the English deck and the PDF for NASA never carry it.
  var ASTRO_LOCAL = 'img/local/astronaut-realistic.png', astroLocal = { ok: false };
  if (!printMode) {
    var astroProbe = new Image();
    astroProbe.onload = function () {
      astroLocal.ok = true;
      if (lang === 'es' && slideEls.length) { build(); printBuilt = false; show(cur, { snap: true }); }
    };
    astroProbe.src = ASTRO_LOCAL;
  }

  function setLang(l) { lang = l; map = PH.values(M, comp, C.FACTS, lang); window.deck.values = map; }
  function fill(s) { return s == null ? '' : PH.fill(s, map); }
  function esc(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
  function T(s) { return esc(fill(s)); }
  function ui(key) { var u = (C.ui && (C.ui[lang] || C.ui.es)) || {}; return u[key] == null ? '' : u[key]; }
  function num(x, n) { return PH.fmt(x, n, lang); }

  // The plan's route does not depend on the tank, only its oxygen bookkeeping does (walkback-model.js, plan()):
  // the plan is VIABLE exactly when the tank holds what its most demanding check needs. Same checks, same order.
  var viableFromLiters = (function (P) {
    var used = 0, need = 0;
    P.legs.forEach(function (leg) {
      used += leg.minutes * M.WALK_LITERS_PER_MINUTE;
      if (leg.toStop < 0) return;
      var s = comp.site.stops[leg.toStop], back = comp.back[M.nodeOf(comp.site, s.x, s.y)] * M.WALK_LITERS_PER_MINUTE;
      need = Math.max(need, used + back);                                  // on arrival
      used += leg.workMinutesAtEnd * M.DESIGN_LITERS_PER_MINUTE;
      need = Math.max(need, used + back);                                  // after the work
    });
    return Math.max(need, used) + M.RESERVE_LITERS;
  })(comp.plan);
  function viableAt(percent) { return comp.plan.hasRoute && M.TANK_LITERS * percent / 100 >= viableFromLiters; }

  /** Names of the stops farther back than `reach` minutes (the ones the oxygen no longer covers). */
  function stopsBeyond(reach) {
    return comp.site.stops.filter(function (s) { return comp.back[M.nodeOf(comp.site, s.x, s.y)] > reach; }).map(function (s) { return s.name; });
  }
  function listNames(names) {
    var and = lang === 'es' ? 'y' : 'and';
    return names.length < 2 ? names.join('') : names.slice(0, -1).join(', ') + ' ' + and + ' ' + names[names.length - 1];
  }

  // ------------------------------------------------------------------ slides
  var slides = C.slides.slice();
  var annex = { id: 'annex', scene: 'gale', pose: 'evidence', segundos: 0, annex: true };
  slides.push(annex);
  var mainCount = C.slides.length;

  function titleHtml(t, oneLine) {
    return '<div class="title' + (oneLine ? ' one' : '') + '">' + T(t.title) +
      (t.titleAccent ? (oneLine ? ' ' : '') + '<span class="accent">' + T(t.titleAccent) + '</span>' : '') + '</div>';
  }
  function bigsHtml(list) {
    return (list || []).map(function (b) { return '<div class="big"><b>' + T(b[0]) + '</b><span>' + T(b[1]) + '</span></div>'; }).join('');
  }
  function pillHtml(text) { return '<span class="pill">' + T(text) + '</span>'; }
  /** "ESTIMADO · plan B sobre el relieve · …" -> the pill (first part) and its qualifier under it. */
  function estimateHtml(text) {
    var s = fill(text || ui('estimate')), cut = s.indexOf(' · ');
    var head = cut > 0 ? s.slice(0, cut) : s, rest = cut > 0 ? s.slice(cut + 3) : '';
    return '<div class="est">' + '<span class="pill">' + esc(head) + '</span>' + (rest ? '<span class="qual">' + esc(rest) + '</span>' : '') + '</div>';
  }
  // The legend's colour swatch is chosen by the first word of each entry (the text stays in content.js).
  var SWATCH = [[/^(ámbar|amber)/i, 'route'], [/^(rayado|hatched)/i, 'slope'], [/^(verde|green)/i, 'habitat'],
    [/^(rojo|red)/i, 'red'], [/^(gris|gr[ae]y)/i, 'gray'], [/^(anillos|rings)/i, 'ring'], [/^(celeste|light blue)/i, 'dots']];
  function legendHtml(list, extra, cls) {
    var items = (list || []).map(function (text) {
      var f = fill(text), kind = '';
      SWATCH.some(function (w) { if (w[0].test(f)) { kind = w[1]; return true; } return false; });
      return '<span class="item">' + (kind ? '<i class="sw sw-' + kind + '"></i>' : '') + esc(f) + '</span>';
    });
    if (extra) items.push('<span class="item"><i class="sw sw-stop"></i>' + T(extra) + '</span>');
    return items.length ? '<div class="legend ' + (cls || 'line') + '">' + items.join('') + '</div>' : '';
  }
  function credit(t) { return '<div class="credit">' + T(t.credit) + '</div>'; }

  var templates = {
    title: function (t) {
      var members = fill(t.members).trim();
      return '<div class="scrim-left"></div><div class="col"><div class="tag">' + T(t.tag) + '</div><div class="brand">' + T(t.title) +
        '</div><div class="motto">' + T(t.sub) + '</div>' + (t.challenge ? '<div class="challenge">' + T(t.challenge) + '</div>' : '') +
        (members ? '<div class="members">' + esc(members) + '</div>' : '') + '</div>' + credit(t);
    },
    problem: function (t) {
      return '<div class="scrim-bottom"></div><div class="q"><div class="title">' + T(t.title) + '<span class="accent">' + T(t.titleAccent) +
        '</span></div>' + ((t.lines || []).length ? '<div class="body">' + t.lines.map(T).join('<br>') + '</div>' : '') + '</div>' + credit(t);
    },
    // table pose: the map runs across the middle (y 211..847): title above it, figures in a row under it
    solution: function (t) {
      return '<div class="top"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t, true) + '</div>' + legendHtml(t.legend, t.list) +
        '<div class="figs">' + bigsHtml(t.big) + estimateHtml(t.estimate) + '</div>' + credit(t);
    },
    // board pose: the map on the left half (x 81..947); the board takes the right half (x > 980)
    verdict: function (t) {
      return '<div class="top"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t) + '</div>' +
        '<div class="est-corner">' + pillHtml(ui('estimate')) + '</div><div class="board-box" data-board="100"></div>' + credit(t);
    },
    // whatif pose: the map on the left two thirds (x 123..1208); everything else in a column on the right (x > 1290)
    demo: function (t) {
      var acts = t.acts || [];
      return '<div class="side">' +
          '<div class="live"><div class="head"><div class="dot"></div><h1>' + T(t.title) + '</h1></div><div class="sub">' + T(t.sub) + '</div>' +
            '<div class="acts">' + acts.map(function (a, i) { return '<div class="act" data-act="' + (i + 1) + '"><b>' + T(a[0]) + '</b><span>' + T(a[1]) + '</span></div>'; }).join('') + '</div>' +
            (t.hint ? '<div class="hint">' + T(t.hint) + '</div>' : '') + '</div>' +
          '<div class="what">' + (acts[1] ? '<div class="act-name">' + T(acts[1][0]) + '</div>' : '') +
            '<div class="tank"><span>' + esc(ui('tank')) + '</span><b data-tank>100 %</b></div><div class="reach" data-reach></div>' +
            '<div class="verdict" data-verdict></div><div class="summary" data-summary></div>' +
            '<div class="red" data-red></div><div class="stops" data-stops></div>' +
            '<div class="est">' + pillHtml(ui('estimate')) + '</div>' + legendHtml(t.legend, null, 'stacked') + '</div>' +
        '</div>' +
        '<div class="plan-head">' + (acts[0] ? '<span class="act-name">' + T(acts[0][0]) + '</span>' : '') + pillHtml(ui('estimate')) + '</div>' +
        '<div class="board-box" data-board="demo"></div>' +
        '<video playsinline controls></video>' + credit(t);
    },
    // evidence pose: seen from the east (x 412..1650, y 211..882): title above, the three lines on the left, figures under it
    evidence: function (t) {
      return '<div class="top"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t, true) + '</div>' + legendHtml(t.legend) +
        '<div class="lines">' + (t.lines || []).map(function (l) { return '<p>' + T(l) + '</p>'; }).join('') + '</div>' +
        '<div class="figs">' + bigsHtml(t.big) + '</div>' + credit(t);
    },
    // closing pose: low over Campo Marte looking home along the route; the terrain fills the lower half, text on top
    closing: function (t) {
      // Defense laptop only, Spanish only: the AI illustration kept outside the repository (img/local/, in .gitignore),
      // marked on the image itself, with the app's visor texts and the plan's numbers drawn over it. Without the file,
      // or in English (the version for NASA), the closing is the Gale scene alone.
      var local = lang === 'es' && astroLocal.ok, v = map, hud = '';
      if (local) {
        hud = '<div class="astro-wrap"><img class="astro-real" src="' + ASTRO_LOCAL + '" alt=""><span class="ai-mark">Ilustración hecha con IA</span></div>' +
          '<div class="astro-shadow"></div>' +
          '<div class="hud">' +
            '<div class="hud-card hud-verdict"><b>&#10003; VIABLE</b><span>' + esc(v.total) + ' · ' + esc(v.km) + '</span></div>' +
            '<div class="hud-card"><span class="k">O2</span><div class="bar"><i style="width:' + esc(v.usedPct) + '%"></i><em style="left:' + (100 - 100 * M.RESERVE_LITERS / M.TANK_LITERS).toFixed(1) + '%"></em></div>' +
              '<span>' + esc(v.usedPct) + ' % del tanque · spare O2 ' + esc(v.spareL) + ' L</span></div>' +
            '<div class="hud-card"><span class="k">back to habitat (estimate)</span><b class="ok">' + esc(v.farStopMin) + ' min</b><span>desde la parada más lejana</span></div>' +
            '<div class="hud-card"><span class="k">NEXT</span><b><i class="dot"></i>Habitat</b><span>la luz verde</span></div>' +
            '<div class="hud-foot">ESTIMATE · not the walking model yet</div>' +
          '</div>';
      }
      return '<div class="scrim-top"></div><div class="scrim-foot"></div>' + hud + '<div class="top"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t) +
        '<div class="brandline"><span class="brand">' + T(t.brand) + '</span><span class="motto">' + T(t.sub) + '</span></div>' +
        (t.next ? '<div class="next">' + T(t.next) + '</div>' : '') + '</div>' +
        '<div class="repo"><img src="img/qr.png" alt="QR"><span>' + esc(ui('project')) + '<br><b>' + T(t.qr) + '</b></span></div>' +
        '<div class="credit' + (local ? ' credit-local' : '') + '">' + T(local && t.creditLocal ? t.creditLocal : t.credit) + '</div>';
    },
    annex: function () {
      var b = C.backup[lang] || C.backup.es;
      return '<div class="scrim-left" style="background:rgba(11,13,15,.88)"></div><div class="col"><div class="tag">' + esc(ui('annexTag')) +
        '</div><div class="title">' + esc(ui('annexTitle')) + '</div></div>' +
        '<div class="qa"><div>' + T(b.realtime) + '</div><div>' + T(b.fps) + '</div></div>';
    }
  };

  var stage = document.getElementById('stage'), slideRoot = document.getElementById('slides'), layerRoot = document.getElementById('layers');
  var hud = document.getElementById('hud'), notes = document.getElementById('notes'), progress = document.getElementById('progress');
  var slideEls = [], boards = {};

  function build() {
    slideRoot.innerHTML = '';
    slideEls = slides.map(function (s) {
      var el = document.createElement('section');
      el.className = 'slide s-' + s.id + (s.accent ? ' accent-' + s.accent : '');
      var t = s.annex ? {} : (s[lang] || s.es);
      if (lang === 'en' && s.en) { t = Object.assign({}, s.es, s.en); }
      el.innerHTML = templates[s.id](t);
      slideRoot.appendChild(el);
      return el;
    });
    root.lang = lang;
    boards = {};
    slideEls.forEach(function (el) {
      el.querySelectorAll('[data-board]').forEach(function (box) {
        if (!window.WalkbackBoard) return;
        try { boards[box.getAttribute('data-board')] = window.WalkbackBoard.create(box); } catch (e) { console.error('board', e); }
      });
    });
    var g = layers.gale;
    if (g && g.scene && g.scene.action) g.scene.action('habitat', ui('habitat'));
    noteWebgl();
  }
  function renderBoard(key, P, animate) {
    var b = boards[key];
    if (!b) return;
    try { b.render(P, comp.site, M); if (animate && !reduced) b.animateIn(); } catch (e) { console.error('board render', e); }
  }

  // ------------------------------------------------------------------ scene layers
  // If WebGL fails, each layer shows a real photo instead (data, not screenshots); the Gale photo sits where the map would.
  var FALLBACK = { globe: 'img/mars_global.jpg', panorama: 'img/sajama_crop.jpg', gale: 'img/ortho.jpg' };
  var FB_BOX = {          // where the HiRISE photo goes when the 3D map cannot be drawn: inside the pose's map area (x, y, side in px)
    table: [590, 205, 600], board: [190, 242, 642], whatif: [215, 70, 900], evidence: [760, 205, 620], closing: [730, 445, 470]
  };
  function noteStyle(b) { return 'display:block;left:' + b[0] + 'px;top:' + (b[1] + b[2] + 8) + 'px;width:' + b[2] + 'px;right:auto'; }
  var layers = {};

  // Print waits for every texture three.js is still decoding (the textures are data URLs: no network).
  var texStarted = false, texDone = null;
  var texturesReady = new Promise(function (resolve) { texDone = resolve; setTimeout(resolve, 15000); });
  if (window.THREE && THREE.DefaultLoadingManager) {
    THREE.DefaultLoadingManager.onStart = function () { texStarted = true; };
    THREE.DefaultLoadingManager.onLoad = function () { texDone(); };
  } else texDone();

  function makeLayer(name) {
    var div = document.createElement('div'); div.className = 'layer';
    var cv = document.createElement('canvas'); cv.width = 1920; cv.height = 1080;
    var img = document.createElement('img'); img.className = 'fb'; img.alt = ''; img.src = FALLBACK[name] || '';
    div.appendChild(cv); div.appendChild(img); layerRoot.appendChild(div);
    var scene = null, f = S.factory(name);
    try {
      if (f && window.THREE) scene = f(cv, { THREE: window.THREE, reduced: reduced, C: S.C, model: comp, habitatName: ui('habitat'),
        makeRenderer: S.helpers.makeRenderer, tex: S.helpers.tex, pillar: S.helpers.pillar });
    } catch (e) { console.error('scene ' + name, e); scene = null; }
    if (!scene) div.classList.add('fallback');
    cv.addEventListener('webglcontextlost', function (e) { e.preventDefault(); div.classList.add('fallback'); layers[name].scene = null; noteWebgl(); });
    layers[name] = { div: div, canvas: cv, img: img, scene: scene };
  }
  ['globe', 'panorama', 'gale'].forEach(makeLayer);
  if (!texStarted) texDone();                                            // no scene loaded a texture (no WebGL): nothing to wait for

  function placeFallback(pose) {
    var b = FB_BOX[pose], im = layers.gale.img;
    if (!b) { im.style.cssText = ''; return; }
    im.style.cssText = 'left:' + b[0] + 'px;top:' + b[1] + 'px;width:' + b[2] + 'px;height:' + b[2] + 'px;object-fit:cover;border-radius:6px';
  }
  var webglNote = null;
  function noteWebgl() {
    if (!webglNote) { webglNote = document.createElement('div'); webglNote.className = 'webgl-note'; stage.appendChild(webglNote); }
    webglNote.textContent = ui('noWebgl');
    var s = slides[cur] || {}, b = FB_BOX[s.pose || 'evidence'];
    var on = layers.gale && !layers.gale.scene && s.scene === 'gale' && !s.annex;
    webglNote.style.cssText = on && b ? noteStyle(b) : 'display:none';
  }

  // ------------------------------------------------------------------ navigation
  var cur = Math.max(0, Math.min(slides.length - 1, (parseInt(location.hash.slice(1), 10) || 1) - 1));
  var activeLayer = null, clockStart = null, demo = { on: false, t0: 0, act: 0, done: false };

  function plannedWindow(i) {
    var a = 0; for (var k = 0; k < i; k++) a += slides[k].segundos || 0;
    var b = a + (slides[i].segundos || 0);
    function f(s) { return Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0'); }
    return slides[i].annex ? ui('annex') : (slides[i].segundos ? f(a) + '–' + f(b) : ui('beforeClock'));
  }

  function show(i, opts) {
    opts = opts || {};
    var from = cur;
    cur = Math.max(0, Math.min(slides.length - 1, i));
    var s = slides[cur], now = performance.now();
    // the clock starts by itself the first time the title slide is left (T restarts it)
    if (!opts.print && clockStart === null && from === 0 && cur > 0 && cur < mainCount) clockStart = now;
    slideEls.forEach(function (el, j) { el.classList.toggle('on', j === cur); });
    if (!opts.print) history.replaceState(null, '', location.search + '#' + (cur + 1));
    var want = s.scene;
    Object.keys(layers).forEach(function (n) { layers[n].div.classList.toggle('on', n === want); });
    var L = layers[want];
    if (want === 'gale') placeFallback(s.pose || 'evidence');
    if (L && L.scene) {
      if (activeLayer !== want && activeLayer && layers[activeLayer].scene && layers[activeLayer].scene.leave) layers[activeLayer].scene.leave();
      L.scene.enter(s, now, { snap: activeLayer !== want || opts.snap, final: opts.final });
    }
    activeLayer = want;
    stopDemo();
    if (s.id === 'verdict') renderBoard('100', comp.plan, !opts.print);
    notes.innerHTML = '<h4>' + esc(plannedWindow(cur)) + ' · ' + esc(s.id) + '</h4>' + T((s.es && s.es.dice) || '') +
      (lang === 'en' && s.en && s.en.dice ? '<h4>EN</h4>' + T(s.en.dice) : '');
    noteWebgl();
    tick();
  }
  function next() { show(cur >= mainCount ? Math.min(cur + 1, slides.length - 1) : Math.min(cur + 1, mainCount - 1)); }
  function prev() { show(cur - 1); }

  // ------------------------------------------------------------------ demo backup (key R): the app's rule, animated
  // Act 1 (0-15 s): the plan on the board. Act 2: the WHAT IF, tank 100 -> 15 % fast, 15 -> 8 % slow (no red above
  // about 12.5 %, as in the app), the red from the app's own minutes back. Act 3 (the visor) is not built.
  var ACT1 = 15, MOVE = 1.5;
  var timeline = WG ? WG.whatIfSteps(M, [[100, 0], [15, 2], [8, 6]]) : function (t) { var p = t >= 6 ? 8 : 100; return { percent: p, reach: M.reachMinutes(p) }; };
  function demoIndex() { return slides.findIndex(function (s) { return s.id === 'demo'; }); }
  function demoEl() { return slideEls[demoIndex()]; }
  function galeAction(name, v, o) { var g = layers.gale; if (g && g.scene && g.scene.action) g.scene.action(name, v, o); }

  function reachText(minutes) { return minutes >= 60 ? String(Math.round(minutes)) : num(minutes, 1); }
  /** The right column of the WHAT IF for a tank level: counter, minutes covered, verdict, red share, stops in the red. */
  function setWhatIf(percent, final) {
    var el = demoEl(); if (!el) return;
    var shown = Math.round(percent), w = M.whatIf(comp.back, percent), ok = viableAt(shown);
    el.querySelector('[data-tank]').textContent = shown + ' %';
    el.querySelector('[data-reach]').textContent = PH.fill(ui('covers'), { reach: reachText(w.reachMinutes) });
    var v = el.querySelector('[data-verdict]');
    v.textContent = ok ? 'VIABLE' : 'NOT VIABLE';
    v.className = 'verdict ' + (ok ? 'ok' : 'no');
    var P = final ? comp.plan8 : (shown >= 100 ? comp.plan : null);
    el.querySelector('[data-summary]').textContent = P ? M.describe(P).summary : '';
    el.querySelector('[data-red]').textContent = w.beyond > 0 ? PH.fill(ui('redShare'), { red: num(w.redPercent, 1) }) : ui('noRed');
    var names = stopsBeyond(w.reachMinutes);
    el.querySelector('[data-stops]').textContent = names.length ? PH.fill(ui('redStops'), { stops: listNames(names) }) : '';
    galeAction('reach', percent >= 99.95 ? Infinity : w.reachMinutes);
  }
  function stopDemo() {
    var el = demoEl(); if (!el) return;
    var wasOn = demo.on || el.classList.contains('backup');
    demo.on = false; demo.done = false; demo.act = 0;
    el.classList.remove('backup', 'act1', 'act2', 'done');
    el.querySelectorAll('.act').forEach(function (a) { a.classList.remove('on'); });
    galeAction('reach', Infinity);
    if (wasOn && slides[cur].id === 'demo') galeAction('pose', 'whatif');
    layers.gale.div.classList.toggle('dim', slides[cur].id === 'demo');
  }
  function startDemo() {
    var el = demoEl(); if (!el) return;
    stopDemo();
    demo.on = true; demo.t0 = performance.now();
    el.classList.add('backup');
    layers.gale.div.classList.remove('dim');
    setWhatIf(100);
  }
  /** The demo slide in the final state of the WHAT IF (tank 8 %, NOT VIABLE, the red): what the PDF shows. */
  function demoFinal() {
    var el = demoEl(); if (!el) return;
    demo.on = false; demo.done = true;
    el.classList.remove('act1'); el.classList.add('backup', 'act2', 'done');
    el.querySelectorAll('.act').forEach(function (a) { a.classList.toggle('on', a.getAttribute('data-act') === '2'); });
    layers.gale.div.classList.remove('dim');
    galeAction('pose', 'whatif', { snap: true }); galeAction('final');
    setWhatIf(8, true);
  }
  function demoFrame(now) {
    if (!demo.on) return;
    var el = demoEl(), sec = (now - demo.t0) / 1000;
    var act = sec < ACT1 ? 1 : 2;
    if (act !== demo.act) {
      demo.act = act;
      el.classList.toggle('act1', act === 1); el.classList.toggle('act2', act === 2);
      el.querySelectorAll('.act').forEach(function (a) { a.classList.toggle('on', +a.getAttribute('data-act') === act); });
      if (act === 1) { renderBoard('demo', comp.plan, true); galeAction('pose', 'board'); }
      if (act === 2) { galeAction('pose', 'whatif'); setWhatIf(100); }
    }
    if (act === 2) {
      var t = sec - ACT1 - MOVE;                     // the WHAT IF starts once the camera is above the map
      if (t >= 0 && !demo.done) {
        var v = timeline(t);
        setWhatIf(v.percent, t >= 6);
        if (t >= 6) { demo.done = true; el.classList.add('done'); }
      }
    }
  }

  // ------------------------------------------------------------------ clock, hud
  function tick() {
    var sec = clockStart === null ? 0 : Math.floor((performance.now() - clockStart) / 1000);
    var label = cur < mainCount ? (cur + 1) + '/' + mainCount : ui('annex');
    var clock = clockStart === null ? (cur === 0 ? ui('clockArmed') : ui('clockOff')) : Math.floor(sec / 60) + ':' + String(sec % 60).padStart(2, '0');
    hud.textContent = label + ' · ' + plannedWindow(cur) + ' · ' + clock + ' · ' + ui('clock');
    hud.style.color = sec >= 180 ? '#F23829' : '';
    progress.style.width = (clockStart === null ? 0 : Math.min(sec / 180, 1) * 100) + '%';
  }

  // ------------------------------------------------------------------ render loop (only the visible scene draws)
  var frames = 0;
  function loop(now) {
    requestAnimationFrame(loop);
    if (window.deck.paused) return;                                      // for checks that drive the frames themselves
    var L = activeLayer && layers[activeLayer];
    if (L && L.scene) { try { L.scene.frame(now); } catch (e) { console.error('frame', e); L.div.classList.add('fallback'); L.scene = null; noteWebgl(); } }
    demoFrame(now);
    frames++;
  }

  // ------------------------------------------------------------------ print: the 7 slides of the 3 minutes, one page each
  // Each page: the slide's scene drawn in its final state as an image (or the layer's real photo when WebGL is not there)
  // and the slide's own text over it. The annex is not printed. The demo slide is printed in the final WHAT IF state.
  var printRoot = document.getElementById('print'), printBuilt = false;
  function assetsReady() {
    var fonts = document.fonts && document.fonts.load ? Promise.all(['400 40px Inter', '600 26px Inter', '700 96px Inter', '800 92px Inter', '900 78px Inter']
      .map(function (f) { return document.fonts.load(f).catch(function () {}); })).then(function () { return document.fonts.ready; }) : Promise.resolve();
    var imgs = [FALLBACK.globe, FALLBACK.panorama, FALLBACK.gale, 'img/qr.png'].map(function (src) {
      var im = new Image(); im.src = src; return im.decode ? im.decode().catch(function () {}) : Promise.resolve();
    });
    return Promise.all([fonts, texturesReady].concat(imgs));
  }
  function buildPrint() {
    var keep = cur, keepClock = clockStart;
    var g = layers.gale;
    if (g.scene && g.scene.gale) g.scene.gale.refreshLabels();
    printRoot.innerHTML = '';
    for (var i = 0; i < mainCount; i++) {
      var s = slides[i];
      show(i, { snap: true, final: true, print: true });
      if (s.id === 'demo') demoFinal();
      var L = layers[s.scene], page = document.createElement('div'), src = null;
      var b = s.scene === 'gale' ? FB_BOX[s.pose || 'evidence'] : null;     // the photo's place if WebGL is not there
      page.className = 'page';
      if (L && L.scene) {
        try { L.scene.frame(performance.now()); L.scene.frame(performance.now() + 1); src = L.canvas.toDataURL('image/jpeg', 0.92); }
        catch (e) { console.error('print ' + s.id, e); src = null; }
      }
      var img = document.createElement('img'); img.className = 'bg'; img.alt = '';
      if (src) img.src = src;
      else {
        img.src = FALLBACK[s.scene] || '';
        if (b) img.style.cssText = 'left:' + b[0] + 'px;top:' + b[1] + 'px;width:' + b[2] + 'px;height:' + b[2] + 'px;right:auto;bottom:auto';
      }
      page.appendChild(img);
      var clone = slideEls[i].cloneNode(true);
      clone.classList.add('on'); clone.querySelectorAll('video').forEach(function (v) { v.remove(); });
      page.appendChild(clone);
      if (!src && b) { var n = document.createElement('div'); n.className = 'webgl-note'; n.style.cssText = noteStyle(b); n.textContent = ui('noWebgl'); page.appendChild(n); }
      printRoot.appendChild(page);
    }
    stopDemo();
    show(keep, { snap: true, print: true });
    clockStart = keepClock;
    printBuilt = true;
    return Promise.all(Array.prototype.map.call(printRoot.querySelectorAll('img'), function (im) { return im.decode ? im.decode().catch(function () {}) : Promise.resolve(); }));
  }
  function printDeck() {
    assetsReady().then(buildPrint).then(function () { setTimeout(function () { window.print(); }, 100); });
  }
  addEventListener('beforeprint', function () { if (!printBuilt) buildPrint(); });   // Ctrl+P without P still prints the 7 pages

  // ------------------------------------------------------------------ input
  var videoFile = document.getElementById('video-file'), videoURL = null;
  videoFile.addEventListener('change', function () {
    var f = videoFile.files[0], el = demoEl(); if (!f || !el) return;
    if (videoURL) URL.revokeObjectURL(videoURL);
    videoURL = URL.createObjectURL(f);
    var v = el.querySelector('video'); v.src = videoURL; el.classList.add('video'); v.play().catch(function () {});
  });
  addEventListener('keydown', function (e) {
    if (e.target.closest && e.target.closest('input,video,button')) return;
    if (e.ctrlKey || e.metaKey || e.altKey) return;                     // leave Ctrl+P and the browser's own keys alone
    var k = e.key, lk = k.toLowerCase();
    if (['ArrowRight', 'PageDown', ' ', 'Enter'].indexOf(k) >= 0) { e.preventDefault(); next(); }
    else if (['ArrowLeft', 'PageUp', 'Backspace'].indexOf(k) >= 0) { e.preventDefault(); prev(); }
    else if (k === 'Home') show(0);
    else if (k === 'End') show(mainCount - 1);
    else if (lk === 'a') show(cur < mainCount ? mainCount : 0);
    else if (lk === 'n') notes.classList.toggle('on');
    else if (lk === 't') { clockStart = cur === 0 ? null : performance.now(); tick(); }   // on the title: starts when leaving it
    else if (lk === 'f') { if (document.fullscreenElement) document.exitFullscreen(); else document.documentElement.requestFullscreen(); }
    else if (lk === 'r' && slides[cur].id === 'demo') { if (demo.on || demo.done) stopDemo(); else startDemo(); }
    else if (lk === 'v' && slides[cur].id === 'demo') videoFile.click();
    else if (lk === 'l') { setLang(lang === 'es' ? 'en' : 'es'); build(); printBuilt = false; show(cur, { snap: true }); }
    else if (lk === 'p') printDeck();
  });
  addEventListener('click', function (e) {
    if (printMode || e.target.closest('a,button,input,video') || notes.contains(e.target)) return;
    if (e.clientX < innerWidth / 3) prev(); else next();
  });
  function fit() { stage.style.transform = 'translate(-50%,-50%) scale(' + Math.min(innerWidth / 1920, innerHeight / 1080) + ')'; }
  addEventListener('resize', fit);

  fit(); build(); show(cur, { snap: true });
  setInterval(tick, 250);
  requestAnimationFrame(loop);
  window.deck.show = show; window.deck.startDemo = startDemo; window.deck.demoFinal = demoFinal; window.deck.values = map;
  window.deck.fps = function () { return frames; };
  window.deck.layers = layers; window.deck.viableFromLiters = viableFromLiters;
  window.deck.buildPrint = function () { return assetsReady().then(buildPrint); };
  window.deck.ready = assetsReady;
  if (printMode) {
    assetsReady().then(buildPrint).then(function () { window.__printReady = true; root.classList.add('print-ready'); });
  }
})();
