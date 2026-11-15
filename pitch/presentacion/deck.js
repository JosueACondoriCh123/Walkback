// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch · the deck: builds every slide from content.js, fills the {numbers} with the app's own rule
// (walkback-model.js), runs one 3D scene at a time, and handles the keys.
// Keys: arrows / space / click = next · N notes · T clock · F full screen · A annex · R demo backup (slide DEMO)
//       V local video (slide DEMO) · L Spanish / English · P print to PDF · Home / End
(function () {
  'use strict';
  var C = window.WALKBACK_CONTENT, M = window.WalkbackModel, S = window.WalkbackScenes;
  var reduced = window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches;
  var params = new URLSearchParams(location.search);
  var lang = params.get('lang') === 'en' ? 'en' : (C.lang || 'es');

  // ------------------------------------------------------------------ the model (same numbers as the app)
  var model = (function () {
    var site = M.siteFromGale(window.GALE); site.stops = window.GALE_STOPS;
    var slope = M.slopeDegrees(site);
    var P100 = M.plan(site, slope, window.GALE_STOPS, 100);
    var P15 = M.plan(site, slope, window.GALE_STOPS, 15);
    var P8 = M.plan(site, slope, window.GALE_STOPS, 8);
    var far = 0;
    window.GALE_STOPS.forEach(function (s) { far = Math.max(far, P100.home[M.nodeOf(site, s.x, s.y)]); });
    return { M: M, site: site, slope: slope, P100: P100, P15: P15, P8: P8, w8: M.whatIf(P100.home, 8), w15: M.whatIf(P100.home, 15), farStop: far };
  })();
  window.deck = { model: model };

  function mins(m) { m = Math.round(m); return m < 60 ? m + ' min' : Math.floor(m / 60) + ' h ' + (m % 60) + ' min'; }
  function dec(x, n, l) { var s = x.toFixed(n); return l === 'es' ? s.replace('.', ',') : s; }
  function values(l) {
    var P = model.P100, F = C.FACTS;
    return {
      km: dec(P.walkMeters / 1000, 1, l) + ' km', meters: String(Math.round(P.walkMeters)),
      walk: mins(P.walkMinutes), work: mins(P.workMinutes), total: mins(P.totalMinutes),
      usedL: String(Math.round(P.usedLiters)), usedPct: String(Math.round(P.usedPercent)), spareL: String(Math.round(P.spareLiters)),
      tankL: String(M.TANK_LITERS), reserveL: String(M.RESERVE_LITERS), limit: String(M.SLOPE_LIMIT_DEGREES),
      red8: dec(model.w8.redPercent, 1, l), reach8: dec(M.reachMinutes(8), 1, l), farStopMin: dec(model.farStop, 1, l),
      demMeters: String(F.demMeters), positions: String(F.positions), photoCm: String(F.photoCm), fps: String(F.fps),
      slopeMeanDiff: F.slopeMeanDiff, slopeR: F.slopeR, slopeMax: F.slopeMax, tests: F.tests, testsEn: F.testsEn
    };
  }
  function fill(str) {
    if (str == null) return '';
    var vEs = values('es'), vEn = values('en');
    return String(str).replace(/\{(\w+)\}/g, function (all, key) {
      if (key === 'testsEn') return vEn.testsEn;
      if (key.slice(-2) === 'En' && key.slice(0, -2) in vEn) return String(vEn[key.slice(0, -2)]).replace(/(\d),(\d)/g, '$1.$2');
      var v = (lang === 'en' ? vEn : vEs)[key];
      return v == null ? all : v;
    });
  }
  function esc(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
  function T(s) { return esc(fill(s)); }

  // ------------------------------------------------------------------ slides
  var slides = C.slides.slice();
  var annex = { id: 'annex', scene: 'gale', pose: 'evidence', segundos: 0, annex: true };
  slides.push(annex);
  var mainCount = C.slides.length;

  function titleHtml(t) {
    return '<div class="title">' + T(t.title) + (t.titleAccent ? '<span class="accent">' + T(t.titleAccent) + '</span>' : '') + '</div>';
  }
  function bigs(list, three) {
    return '<div class="bigs' + (three ? ' three' : '') + '">' + (list || []).map(function (b) {
      return '<div class="big"><b>' + T(b[0]) + '</b><span>' + T(b[1]) + '</span></div>';
    }).join('') + '</div>';
  }
  function legendHtml() {
    var en = lang === 'en';
    return '<div class="legend">' +
      '<div><i style="background:var(--green)"></i>' + (en ? 'Habitat (team proposal)' : 'Hábitat (propuesta del equipo)') + '</div>' +
      '<div><i style="background:var(--amber)"></i>' + window.GALE_STOPS.map(function (s) { return esc(s.name); }).join(' · ') + '</div>' +
      '<div><i class="hatch"></i>' + (en ? 'Slope over 20°: not walked' : 'Pendiente sobre 20°: no se pisa') + '</div></div>';
  }

  var templates = {
    title: function (t) {
      return '<div class="scrim-left"></div><div class="col"><div class="tag">' + T(t.tag) + '</div><div class="brand">' + T(t.title) +
        '</div><div class="motto">' + T(t.sub) + '</div></div><div class="credit">' + T(t.credit) + '</div>';
    },
    problem: function (t) {
      return '<div class="scrim-bottom"></div><div class="q"><div class="title">' + T(t.title) + '<span class="accent">' + T(t.titleAccent) +
        '</span></div>' + ((t.lines || []).length ? '<div class="body">' + t.lines.map(T).join('<br>') + '</div>' : '') + '</div><div class="credit">' + T(t.credit) + '</div>';
    },
    solution: function (t) {
      return '<div class="scrim-left"></div><div class="col"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t) + bigs(t.big, true) +
        '<div><span class="pill">' + T(t.estimate) + '</span></div>' +
        '</div><div class="credit">' + T(t.credit) + '</div>';
    },
    verdict: function (t) {
      return '<div class="scrim-left" style="background:linear-gradient(90deg,rgba(11,13,15,.92),rgba(11,13,15,.55) 40%,rgba(11,13,15,.2) 50%,rgba(11,13,15,.85) 56%,rgba(11,13,15,.95))"></div>' +
        '<div class="col"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t) + '</div><div class="board-box" data-board="100"></div><div class="credit">' + T(t.credit) + '</div>';
    },
    demo: function (t) {
      return '<div class="live"><div class="head"><div class="dot"></div><h1>' + T(t.title) + '</h1></div><div class="body soft">' + T(t.sub) + '</div></div>' +
        '<div class="hint">' + T(t.hint) + '</div><div class="tank"><b data-tank>100 %</b><span data-reach></span></div>' +
        '<div class="board-box" data-board="demo"></div>' +
        '<div class="acts">' + (t.acts || []).map(function (a, i) { return '<div class="act" data-act="' + (i + 1) + '"><b>' + T(a[0]) + '</b><span>' + T(a[1]) + '</span></div>'; }).join('') + '</div>' +
        '<video playsinline controls></video><div class="credit">' + T(t.credit) + '</div>';
    },
    evidence: function (t) {
      return '<div class="scrim-left"></div><div class="col"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t) + bigs(t.big, true) +
        '<div class="lines">' + (t.lines || []).map(function (l) { return '<p>' + T(l) + '</p>'; }).join('') + '</div></div><div class="credit">' + T(t.credit) + '</div>';
    },
    closing: function (t) {
      var v = values(lang), hud = '';
      if (lang === 'es') {                 // defense only: AI illustration (credited) + the app's HUD texts with the plan's numbers
        hud = '<img class="astro-real" src="img/astronaut-realistic.png" alt=""><div class="astro-shadow"></div>' +
          '<div class="hud">' +
            '<div class="hud-card hud-verdict"><b>&#10003; VIABLE</b><span>' + esc(v.total) + ' · ' + esc(v.km) + '</span></div>' +
            '<div class="hud-card"><span class="k">O2</span><div class="bar"><i style="width:' + v.usedPct + '%"></i><em style="left:' + (100 - 100 * M.RESERVE_LITERS / M.TANK_LITERS).toFixed(1) + '%"></em></div>' +
              '<span>' + esc(v.usedPct) + ' % del tanque · spare O2 ' + esc(v.spareL) + ' L</span></div>' +
            '<div class="hud-card"><span class="k">back to habitat (estimate)</span><b class="ok">' + esc(v.farStopMin) + ' min</b><span>desde la parada más lejana</span></div>' +
            '<div class="hud-card"><span class="k">NEXT</span><b><i class="dot"></i>Habitat</b><span>la luz verde</span></div>' +
          '</div><div class="hud-foot">ESTIMATE · not the walking model yet</div>';
      }
      return '<div class="scrim-left"></div>' + hud + '<div class="col"><div class="tag">' + T(t.tag) + '</div>' + titleHtml(t) +
        '<div class="brand">' + T(t.brand) + '</div><div class="motto">' + T(t.sub) + '</div>' + (t.next ? '<div class="next">' + T(t.next) + '</div>' : '') + '</div>' +
        '<div class="repo"><img src="img/qr.png" alt="QR"><span>' + (lang === 'en' ? 'The project' : 'El proyecto') + '<br><b>' + T(t.qr) + '</b></span></div>' +
        '<div class="credit">' + T(t.credit) + '</div>';
    },
    annex: function () {
      var b = C.backup[lang] || C.backup.es;
      return '<div class="scrim-left" style="background:rgba(11,13,15,.88)"></div><div class="col"><div class="tag">' + (lang === 'en' ? 'Annex · outside the 3 minutes' : 'Anexo · fuera de los 3 minutos') +
        '</div><div class="title">' + (lang === 'en' ? 'If the jury asks' : 'Si pregunta el jurado') + '</div></div>' +
        '<div class="qa"><div>' + T(b.realtime) + '</div><div>' + T(b.fps) + '</div></div>';
    }
  };

  var stage = document.getElementById('stage'), slideRoot = document.getElementById('slides'), layerRoot = document.getElementById('layers');
  var hud = document.getElementById('hud'), notes = document.getElementById('notes'), progress = document.getElementById('progress');
  var slideEls = [];

  function build() {
    slideRoot.innerHTML = '';
    slideEls = slides.map(function (s) {
      var el = document.createElement('section');
      el.className = 'slide s-' + s.id;
      var t = s.annex ? {} : (s[lang] || s.es);
      if (lang === 'en' && s.en) { t = Object.assign({}, s.es, s.en); }
      el.innerHTML = templates[s.id](t);
      slideRoot.appendChild(el);
      return el;
    });
    document.documentElement.lang = lang;
    boards = {};
    slideEls.forEach(function (el) {
      el.querySelectorAll('[data-board]').forEach(function (box) {
        if (!window.WalkbackBoard) return;
        try { boards[box.getAttribute('data-board')] = window.WalkbackBoard.create(box); } catch (e) { console.error('board', e); }
      });
    });
  }
  var boards = {};
  function renderBoard(key, P) {
    var b = boards[key];
    if (!b) return;
    try { b.render(P, model.site, M); if (!reduced) b.animateIn(); } catch (e) { console.error('board render', e); }
  }

  // ------------------------------------------------------------------ scene layers
  var FALLBACK = { globe: 'img/mars_global.jpg', panorama: 'img/sajama_crop.jpg', gale: 'img/ortho.jpg', astronaut: 'img/sajama_crop.jpg' };
  var layers = {};
  function makeLayer(name) {
    var div = document.createElement('div'); div.className = 'layer';
    var cv = document.createElement('canvas'); cv.width = 1920; cv.height = 1080;
    var img = document.createElement('img'); img.className = 'fb'; img.alt = ''; img.src = FALLBACK[name] || '';
    div.appendChild(cv); div.appendChild(img); layerRoot.appendChild(div);
    var scene = null, f = S.factory(name);
    try {
      if (f && window.THREE) scene = f(cv, { THREE: window.THREE, reduced: reduced, C: S.C, model: model,
        makeRenderer: S.helpers.makeRenderer, tex: S.helpers.tex, pillar: S.helpers.pillar });
    } catch (e) { console.error('scene ' + name, e); scene = null; }
    if (!scene) div.classList.add('fallback');
    cv.addEventListener('webglcontextlost', function (e) { e.preventDefault(); div.classList.add('fallback'); layers[name].scene = null; });
    layers[name] = { div: div, canvas: cv, scene: scene };
  }
  ['globe', 'panorama', 'gale', 'astronaut'].forEach(makeLayer);

  // ------------------------------------------------------------------ navigation
  var cur = Math.max(0, Math.min(slides.length - 1, (parseInt(location.hash.slice(1), 10) || 1) - 1));
  var activeLayer = null, clockStart = null, demo = { on: false, t0: 0, act: 0, done: false };

  function plannedWindow(i) {
    var a = 0; for (var k = 0; k < i; k++) a += slides[k].segundos || 0;
    var b = a + (slides[i].segundos || 0);
    function f(s) { return Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0'); }
    return slides[i].annex ? (lang === 'en' ? 'annex' : 'anexo') : (slides[i].segundos ? f(a) + '–' + f(b) : (lang === 'en' ? 'before the clock' : 'antes del reloj'));
  }

  function show(i, opts) {
    opts = opts || {};
    cur = Math.max(0, Math.min(slides.length - 1, i));
    var s = slides[cur], now = performance.now();
    slideEls.forEach(function (el, j) { el.classList.toggle('on', j === cur); });
    history.replaceState(null, '', location.search + '#' + (cur + 1));
    var want = s.scene;
    Object.keys(layers).forEach(function (n) { layers[n].div.classList.toggle('on', n === want); });
    var L = layers[want];
    if (L && L.scene) {
      if (activeLayer !== want && activeLayer && layers[activeLayer].scene && layers[activeLayer].scene.leave) layers[activeLayer].scene.leave();
      L.scene.enter(s, now, { snap: activeLayer !== want || opts.snap, final: opts.final });
    }
    activeLayer = want;
    stopDemo();
    if (s.id === 'verdict') renderBoard('100', model.P100);
    var t = s[lang] || s.es || {};
    notes.innerHTML = '<h4>' + plannedWindow(cur) + ' · ' + esc(s.id) + '</h4>' + T((s.es && s.es.dice) || '') +
      (lang === 'en' && s.en && s.en.dice ? '<h4>EN</h4>' + T(s.en.dice) : '');
    void t;
    tick();
  }
  function next() { show(cur >= mainCount ? Math.min(cur + 1, slides.length - 1) : Math.min(cur + 1, mainCount - 1)); }
  function prev() { show(cur - 1); }

  // ------------------------------------------------------------------ demo backup (key R): the app's rule, animated
  function demoEl() { return slideEls[slides.findIndex(function (s) { return s.id === 'demo'; })]; }
  function stopDemo() {
    var el = demoEl(); if (!el) return;
    demo.on = false; demo.done = false; demo.act = 0;
    el.classList.remove('backup', 'act1', 'act2', 'done');
    el.querySelectorAll('.act').forEach(function (a) { a.classList.remove('on'); });
    var g = layers.gale; if (g.scene && g.scene.action) g.scene.action('reach', Infinity);
    g.div.classList.toggle('dim', slides[cur].id === 'demo');
  }
  function startDemo() {
    var el = demoEl(); if (!el) return;
    stopDemo();
    demo.on = true; demo.t0 = performance.now();
    el.classList.add('backup');
    layers.gale.div.classList.remove('dim');
  }
  function tankAt(sec) {                // 100 -> 15 % in 2 s, then 15 -> 8 % in 4 s (the red only appears under ~12.5 %)
    var sm = window.WalkbackGale.smooth01;
    if (sec < 2) return 100 + (15 - 100) * sm(sec / 2);
    return 15 + (8 - 15) * sm((sec - 2) / 4);
  }
  function demoFrame(now) {
    if (!demo.on) return;
    var el = demoEl(), sec = (now - demo.t0) / 1000;
    var act = sec < 15 ? 1 : 2;              // act 3 (the visor) is not built: act 2 lasts until the end (AUDIT-R19)
    if (act !== demo.act) {
      demo.act = act;
      el.classList.toggle('act1', act === 1); el.classList.toggle('act2', act === 2);
      el.querySelectorAll('.act').forEach(function (a) { a.classList.toggle('on', +a.getAttribute('data-act') === act); });
      if (act === 1) { renderBoard('demo', model.P100); if (layers.gale.scene) layers.gale.scene.action('pose', 'board'); }
      if (act === 2 && layers.gale.scene) layers.gale.scene.action('pose', 'whatif');
    }
    var pct = 100;
    if (act === 2) {
      pct = tankAt(sec - 15);
      if (layers.gale.scene) layers.gale.scene.action('reach', M.reachMinutes(pct));
      if (!demo.done && sec - 15 >= 6) { demo.done = true; el.classList.add('done'); renderBoard('demo', model.P8); }
    }
    el.querySelector('[data-tank]').textContent = (lang === 'en' ? 'TANK ' : 'TANQUE ') + Math.round(pct) + ' %';
    el.querySelector('[data-reach]').textContent = (lang === 'en' ? 'covers ' : 'alcanza para ') + dec(M.reachMinutes(pct), 1, lang) +
      (lang === 'en' ? ' min of walking back · ESTIMATE' : ' min de vuelta a pie · ESTIMADO');
  }

  // ------------------------------------------------------------------ clock, hud
  function tick() {
    var sec = clockStart === null ? 0 : Math.floor((performance.now() - clockStart) / 1000);
    var label = cur < mainCount ? (cur + 1) + '/' + mainCount : (lang === 'en' ? 'Annex' : 'Anexo');
    hud.textContent = label + ' · ' + plannedWindow(cur) + ' · ' + (clockStart === null ? 'T reloj' : Math.floor(sec / 60) + ':' + String(sec % 60).padStart(2, '0')) +
      ' · N notas · L ' + (lang === 'es' ? 'EN' : 'ES') + (slides[cur].id === 'demo' ? ' · R respaldo · V video' : '');
    hud.style.color = sec >= 180 ? '#F23829' : '';
    progress.style.width = (clockStart === null ? 0 : Math.min(sec / 180, 1) * 100) + '%';
  }

  // ------------------------------------------------------------------ render loop (only the visible scene draws)
  var frames = 0, slowSince = 0, lastNow = 0;
  function loop(now) {
    requestAnimationFrame(loop);
    var L = activeLayer && layers[activeLayer];
    if (L && L.scene) { try { L.scene.frame(now); } catch (e) { console.error('frame', e); L.div.classList.add('fallback'); L.scene = null; } }
    demoFrame(now);
    if (lastNow) { var dt = now - lastNow; if (dt > 24) { if (!slowSince) slowSince = now; } else slowSince = 0; }
    lastNow = now; frames++;
  }

  // ------------------------------------------------------------------ print (P): every slide with its scene drawn, one page each
  function printDeck() {
    var keep = cur, out = document.createElement('div'); out.id = 'print';
    for (var i = 0; i < mainCount; i++) {
      show(i, { snap: true, final: true });
      var L = layers[slides[i].scene], page = document.createElement('div');
      page.style.cssText = 'position:relative;width:1920px;height:1080px;overflow:hidden;break-after:page;background:#0B0D0F';
      var src = null;
      if (L && L.scene) { try { L.scene.frame(performance.now() + 60000); src = L.canvas.toDataURL('image/jpeg', 0.9); } catch (e) { src = null; } }
      var img = document.createElement('img'); img.src = src || FALLBACK[slides[i].scene]; img.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;object-fit:cover';
      page.appendChild(img);
      var clone = slideEls[i].cloneNode(true); clone.classList.add('on'); clone.style.transition = 'none'; clone.querySelectorAll('video').forEach(function (v) { v.remove(); });
      page.appendChild(clone); out.appendChild(page);
    }
    var css = document.createElement('style');
    css.textContent = '@media print{#stage{display:none!important}#print{display:block!important}#print .slide{opacity:1;position:absolute;inset:0}}#print{display:none}';
    document.body.appendChild(css); document.body.appendChild(out);
    show(keep);
    setTimeout(function () { window.print(); setTimeout(function () { out.remove(); css.remove(); }, 500); }, 300);
  }

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
    var k = e.key, lk = k.toLowerCase();
    if (['ArrowRight', 'PageDown', ' ', 'Enter'].indexOf(k) >= 0) { e.preventDefault(); next(); }
    else if (['ArrowLeft', 'PageUp', 'Backspace'].indexOf(k) >= 0) { e.preventDefault(); prev(); }
    else if (k === 'Home') show(0);
    else if (k === 'End') show(mainCount - 1);
    else if (lk === 'a') show(cur < mainCount ? mainCount : 0);
    else if (lk === 'n') notes.classList.toggle('on');
    else if (lk === 't') { clockStart = clockStart === null ? performance.now() : null; tick(); }
    else if (lk === 'f') { if (document.fullscreenElement) document.exitFullscreen(); else document.documentElement.requestFullscreen(); }
    else if (lk === 'r' && slides[cur].id === 'demo') { if (demo.on) stopDemo(); else startDemo(); }
    else if (lk === 'v' && slides[cur].id === 'demo') videoFile.click();
    else if (lk === 'l') { lang = lang === 'es' ? 'en' : 'es'; build(); show(cur, { snap: true }); }
    else if (lk === 'p') printDeck();
  });
  addEventListener('click', function (e) {
    if (e.target.closest('a,button,input,video') || notes.contains(e.target)) return;
    if (e.clientX < innerWidth / 3) prev(); else next();
  });
  function fit() { stage.style.transform = 'translate(-50%,-50%) scale(' + Math.min(innerWidth / 1920, innerHeight / 1080) + ')'; }
  addEventListener('resize', fit);

  fit(); build(); show(cur, { snap: true });
  setInterval(tick, 250);
  requestAnimationFrame(loop);
  window.deck.show = show; window.deck.startDemo = startDemo; window.deck.values = values;
  window.deck.fps = function () { return frames; };
})();
