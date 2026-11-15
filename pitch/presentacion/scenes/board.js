// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch: the app's main verdict board, redrawn in HTML + inline SVG over the 3D scene.
// Layout, words and colours follow Assets/Walkback/App/MissionPanel.cs, MissionCharts.cs and PlanCharts.cs
// (main, eb8f7f8). Every number is read from the plan object made by walkback-model.js (plan B, ESTIMATE):
// nothing on this board is typed by hand.
//
//   var board = WalkbackBoard.create(container);   // container: an absolutely positioned div; the board fills it
//   board.render(P, site, M);                      // P = M.plan(...), site = M.siteFromGale(...) with .stops, M = WalkbackModel
//   board.animateIn();                             // bars fill and the profile draws in 1.2 s (none with reduced motion)
(function () {
  'use strict';

  var DESIGN_W = 860, DESIGN_H = 820;          // the board is laid out at this size and scaled to its container
  var DURATION = 1200;

  // WalkbackTheme.cs / MissionCharts.cs colours (the chart colours are the app's, see MissionCharts: Used .. Work).
  var C = (window.WalkbackScenes && window.WalkbackScenes.C) || {
    back: '#0B0D0F', card: '#1F2229', text: '#EBEBEB', soft: '#CCD1D9',
    amber: '#FFB80D', green: '#40D966', red: '#F23829', sky: '#59D1FF', gray: '#46464A'
  };
  var TRACK = '#333842';                       // MissionCharts.Track (0.20, 0.22, 0.26)
  var USED = '#9EA6B3';                        // MissionCharts.Used (0.62, 0.65, 0.70): breathed, a neutral gray
  var PROFILE_FILL = '#745B1E';                // MissionCharts.ProfileFill: card lerped 38 % to amber
  var COL = {
    used: USED, spare: C.green, reserve: C.amber, intoReserve: C.red,   // MissionCharts: Spare = Safe, Reserve = Watch, IntoReserve = Danger
    walk: C.amber, work: C.sky                                            // MissionCharts: Walk = Watch, Work = Live
  };

  var CSS = [
    '.wb-board{position:absolute;inset:0;overflow:hidden;pointer-events:none;font-family:Inter,system-ui,sans-serif;color:' + C.text + ';}',
    '.wb-board *{box-sizing:border-box;}',
    '.wb-board .wb-frame{position:absolute;left:0;top:0;width:' + DESIGN_W + 'px;height:' + DESIGN_H + 'px;transform-origin:0 0;',
    '  background:rgba(11,13,15,0.92);border-radius:24px;padding:20px;display:flex;flex-direction:column;gap:12px;}',
    '.wb-board .wb-card{background:' + C.card + ';border-radius:10px;padding:14px 20px;position:relative;}',
    '.wb-board .wb-verdict{padding:16px 20px 16px 30px;}',
    '.wb-board .wb-stripe{position:absolute;left:0;top:0;bottom:0;width:10px;border-radius:10px 0 0 10px;}',
    '.wb-board .wb-head{display:flex;align-items:center;gap:18px;}',
    '.wb-board .wb-icon{flex:0 0 auto;width:96px;height:96px;display:block;}',
    '.wb-board .wb-headline{flex:1 1 auto;min-width:0;font-weight:700;font-size:88px;line-height:1;letter-spacing:-0.01em;white-space:nowrap;}',
    '.wb-board .wb-mission{flex:0 0 auto;align-self:flex-start;text-align:center;}',
    '.wb-board .wb-button{border:2px solid ' + C.soft + ';border-radius:8px;padding:9px 16px;font-weight:700;font-size:24px;letter-spacing:0.02em;line-height:1.1;white-space:nowrap;}',
    '.wb-board .wb-caption-small{margin-top:6px;font-size:22px;color:' + C.soft + ';white-space:nowrap;}',
    '.wb-board .wb-summary{margin-top:12px;font-size:32px;line-height:1.2;white-space:pre-wrap;}',
    '.wb-board .wb-note{margin-top:6px;font-size:24px;line-height:1.25;color:' + C.soft + ';white-space:pre-wrap;}',
    '.wb-board .wb-row{display:flex;justify-content:space-between;align-items:baseline;gap:16px;font-size:24px;font-weight:700;line-height:1.25;white-space:pre;}',
    '.wb-board .wb-cap{color:' + C.soft + ';}',
    '.wb-board .wb-bar{position:relative;height:34px;margin:9px 0;background:' + TRACK + ';overflow:hidden;}',
    '.wb-board .wb-fill{position:absolute;inset:0;}',
    '.wb-board .wb-seg{position:absolute;top:0;bottom:0;}',
    '.wb-board .wb-mark{position:absolute;top:0;bottom:0;width:4px;background:' + C.text + ';}',
    '.wb-board .wb-numbers{margin-top:4px;font-size:22px;color:' + C.text + ';white-space:pre;}',
    '.wb-board .wb-plotwrap{position:relative;margin-top:8px;}',
    '.wb-board .wb-plot{display:block;overflow:visible;}',
    '.wb-board .wb-stop{position:absolute;font-size:22px;line-height:28px;white-space:nowrap;transform:translateX(-50%);color:' + C.text + ';}',
    '.wb-board .wb-hidden{visibility:hidden;}'
  ].join('\n');

  function injectCss() {
    if (document.getElementById('wb-board-css')) return;
    var s = document.createElement('style');
    s.id = 'wb-board-css';
    s.textContent = CSS;
    document.head.appendChild(s);
  }

  function el(tag, cls, parent, text) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text != null) e.textContent = text;
    if (parent) parent.appendChild(e);
    return e;
  }
  var SVG = 'http://www.w3.org/2000/svg';
  function sv(tag, attrs, parent) {
    var e = document.createElementNS(SVG, tag);
    for (var k in attrs) if (attrs.hasOwnProperty(k)) e.setAttribute(k, attrs[k]);
    if (parent) parent.appendChild(e);
    return e;
  }

  // HelmetHud.MinutesText / MetersText, MissionCharts.Signed
  function minutesText(m) {
    if (m < 1) return 'under 1 min';
    var w = Math.floor(m + 0.5);
    return w < 60 ? w + ' min' : Math.floor(w / 60) + ' h ' + (w % 60) + ' min';
  }
  function metersText(m) { return m >= 1000 ? (m / 1000).toFixed(1) + ' km' : Math.round(m) + ' m'; }
  function signed(m) { var w = Math.round(m); if (w === 0) w = 0; return (w > 0 ? '+' : '') + w; }

  /** PlanCharts.TankAt at the end of the plan: five fractions of the full tank. */
  function tankParts(P, M) {
    var full = M.TANK_LITERS, tank = Math.min(Math.max(P.tankLiters, 0), full);
    var reserve = Math.min(M.RESERVE_LITERS, tank), usable = tank - reserve, used = Math.max(0, P.usedLiters);
    var fromUsable = Math.min(used, usable), fromReserve = Math.min(Math.max(used - usable, 0), reserve);
    return { used: fromUsable / full, intoReserve: fromReserve / full, spare: (usable - fromUsable) / full,
      reserve: (reserve - fromReserve) / full, missing: (full - tank) / full };
  }

  /** PlanCharts.Timeline: walking and work in the order they happen, as fractions of the total time. */
  function timeline(P) {
    var spans = [], total = 0, at = 0, i, leg;
    for (i = 0; i < P.legs.length; i++) total += P.legs[i].minutes + P.legs[i].workMinutesAtEnd;
    if (total <= 0) return spans;
    for (i = 0; i < P.legs.length; i++) {
      leg = P.legs[i];
      spans.push({ start: at / total, end: (at + leg.minutes) / total, work: false });
      at += leg.minutes;
      if (leg.workMinutesAtEnd > 0) {
        spans.push({ start: at / total, end: (at + leg.workMinutesAtEnd) / total, work: true });
        at += leg.workMinutesAtEnd;
      }
    }
    return spans;
  }

  /** PlanCharts.MakeProfile: height along the route above the habitat (the path's first point), and the stops on it. */
  function makeProfile(site, P, M) {
    var raw = M.profile(site, P), meters = [], height = [], i;
    if (raw.length < 2) return null;
    var home = raw[0].height, hi = 0, lo = 0;
    for (i = 0; i < raw.length; i++) {
      meters.push(raw[i].meters);
      height.push(raw[i].height - home);
      if (height[i] > hi) hi = height[i];
      if (height[i] < lo) lo = height[i];
    }
    var stops = [];
    for (i = 0; i < P.legs.length; i++) {
      var leg = P.legs[i];
      if (leg.toStop < 0 || leg.lastPoint < 0 || leg.lastPoint >= raw.length) continue;
      stops.push({ meters: meters[leg.lastPoint], height: height[leg.lastPoint], name: leg.toName });
    }
    return { meters: meters, height: height, total: meters[meters.length - 1], highest: hi, lowest: lo, stops: stops };
  }

  var measureCtx = null;
  function textWidth(text, px) {
    if (!measureCtx) measureCtx = document.createElement('canvas').getContext('2d');
    measureCtx.font = '400 ' + px + 'px Inter, system-ui, sans-serif';
    return measureCtx.measureText(text).width;
  }

  function reducedMotion() {
    return !!(window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  }
  function ease(t) { return t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2; }

  var uid = 0;

  function create(container) {
    injectCss();
    var id = 'wbb' + (++uid);
    var root = el('div', 'wb-board', container);
    var frame = el('div', 'wb-frame', root);

    // Verdict card: icon, headline, START MISSION (drawn, not clickable), summary, note.
    var verdict = el('div', 'wb-card wb-verdict', frame);
    var stripe = el('div', 'wb-stripe', verdict);
    var head = el('div', 'wb-head', verdict);
    var icon = sv('svg', { 'class': 'wb-icon', viewBox: '0 0 150 150', 'aria-hidden': 'true' }, head);
    var headline = el('div', 'wb-headline', head);
    var mission = el('div', 'wb-mission', head);
    el('div', 'wb-button', mission, 'START MISSION');
    el('div', 'wb-caption-small', mission, '1:1 terrain, on foot');
    var summary = el('div', 'wb-summary', verdict);
    var note = el('div', 'wb-note', verdict);

    // Oxygen card.
    var tankCard = el('div', 'wb-card', frame);
    var tankRow = el('div', 'wb-row', tankCard);
    var tankCaption = el('span', 'wb-cap', tankRow);
    var tankBar = el('div', 'wb-bar', tankCard);
    var tankFill = el('div', 'wb-fill', tankBar);
    var tankLabels = el('div', 'wb-row', tankCard);
    var tankUsed = el('span', '', tankLabels), tankSpare = el('span', '', tankLabels), tankReserve = el('span', '', tankLabels);

    // Time card.
    var timeCard = el('div', 'wb-card', frame);
    var timeRow = el('div', 'wb-row', timeCard);
    var timeCaption = el('span', 'wb-cap', timeRow);
    var timeTotal = el('span', '', timeRow);
    var timeBar = el('div', 'wb-bar', timeCard);
    var timeFill = el('div', 'wb-fill', timeBar);
    var timeLabels = el('div', 'wb-row', timeCard);
    var timeWalk = el('span', '', timeLabels), timeWork = el('span', '', timeLabels);

    // Height card.
    var plotCard = el('div', 'wb-card', frame);
    plotCard.style.flex = '1 1 auto';
    var plotRow = el('div', 'wb-row', plotCard);
    el('span', 'wb-cap', plotRow, 'HEIGHT ALONG THE ROUTE');
    var plotNumbers = el('div', 'wb-numbers', plotCard);
    var plotWrap = el('div', 'wb-plotwrap', plotCard);
    var PW = DESIGN_W - 40 - 40, PH = 110;     // plot size (frame padding 20 x 2, card padding 20 x 2)
    var plot = sv('svg', { 'class': 'wb-plot', width: PW, height: PH, viewBox: '0 0 ' + PW + ' ' + PH }, plotWrap);
    var clip = sv('clipPath', { id: id + '-clip' }, sv('defs', {}, plot));
    var clipRect = sv('rect', { x: -10, y: -20, width: PW + 20, height: PH + 40 }, clip);
    var plotBase = sv('g', {}, plot);
    var plotDrawn = sv('g', { 'clip-path': 'url(#' + id + '-clip)' }, plot);
    var plotStops = sv('g', {}, plot);
    var labelBox = el('div', '', plotWrap);
    labelBox.style.position = 'relative';
    labelBox.style.height = '60px';

    var state = { stops: [], total: 0, anim: 0, hasPlan: false };

    function fit() {
      var w = container.clientWidth, h = container.clientHeight;
      if (!w || !h) return;
      var s = Math.min(w / DESIGN_W, h / DESIGN_H);
      frame.style.transform = 'translate(' + ((w - DESIGN_W * s) / 2) + 'px,' + ((h - DESIGN_H * s) / 2) + 'px) scale(' + s + ')';
    }
    if (window.ResizeObserver) new ResizeObserver(fit).observe(container);
    else window.addEventListener('resize', fit);

    /** The headline shrinks only if the START MISSION block leaves it too little room ("NOT VIABLE"). */
    function fitHeadline() {
      var size = 88;
      headline.style.fontSize = size + 'px';
      while (size > 60 && headline.scrollWidth > headline.clientWidth + 1) {
        size -= 2;
        headline.style.fontSize = size + 'px';
      }
    }

    function drawIcon(kind, color) {
      while (icon.firstChild) icon.removeChild(icon.firstChild);
      sv('circle', { cx: 75, cy: 75, r: 70.5, fill: color }, icon);
      var line = { stroke: C.card, 'stroke-width': 18, 'stroke-linecap': 'round', 'stroke-linejoin': 'round', fill: 'none' };
      if (kind === 'viable') { line.points = '40.5,76.5 66,102 111,49.5'; sv('polyline', line, icon); }
      else if (kind === 'notViable') {
        line.d = 'M46.5,46.5 L103.5,103.5 M103.5,46.5 L46.5,103.5'; sv('path', line, icon);
      } else { line.d = 'M42,75 L108,75'; sv('path', line, icon); }
    }

    function segments(fill, parts) {
      while (fill.firstChild) fill.removeChild(fill.firstChild);
      var x = 0;
      for (var i = 0; i < parts.length; i++) {
        var w = parts[i].f * 100;
        if (w <= 0) continue;
        var seg = el('div', 'wb-seg', fill);
        seg.style.left = x + '%';
        seg.style.width = 'calc(' + w + '% - 2px)';           // a 2 px dark gap after each part, like the app
        seg.style.minWidth = '1px';
        seg.style.background = parts[i].color;
        x += w;
      }
      return x;
    }

    function placeLabels() {
      while (labelBox.firstChild) labelBox.removeChild(labelBox.firstChild);
      // MissionCharts.PlaceStopLabels: centred under the post; a name that runs into the one before goes on the second row.
      var lastRight = -Infinity, lastLow = true;
      for (var i = 0; i < state.stops.length; i++) {
        var s = state.stops[i], half = textWidth(s.name, 22) / 2 + 8;
        var center = Math.min(Math.max(s.x, half), PW - half);
        var low = center - half < lastRight && !lastLow;
        var lab = el('div', 'wb-stop', labelBox, s.name);
        lab.style.left = center + 'px';
        lab.style.top = (low ? 30 : 2) + 'px';
        s.label = lab;
        lastRight = center + half; lastLow = low;
      }
    }

    function setProgress(p) {
      state.anim = p;
      var cut = (1 - p) * 100;
      var clipCss = p >= 1 ? '' : 'inset(0 ' + cut + '% 0 0)';
      tankFill.style.clipPath = tankFill.style.webkitClipPath = clipCss;
      timeFill.style.clipPath = timeFill.style.webkitClipPath = clipCss;
      clipRect.setAttribute('width', p >= 1 ? PW + 20 : Math.max(0, p * PW + 10));
      var drawn = p * state.total;
      for (var i = 0; i < state.stops.length; i++) {
        var s = state.stops[i], on = p >= 1 || s.meters <= drawn + 0.5;
        if (s.mark) s.mark.setAttribute('visibility', on ? 'visible' : 'hidden');
        if (s.label) s.label.className = 'wb-stop' + (on ? '' : ' wb-hidden');
      }
    }

    function render(P, site, M) {
      M = M || window.WalkbackModel;
      var d = M.describe(P), whatIf = P.tankPercent < 100;
      var kind = !P.hasRoute ? 'none' : (P.viable ? 'viable' : 'notViable');
      var color = kind === 'viable' ? C.green : kind === 'notViable' ? C.red : C.amber;

      stripe.style.background = color;
      drawIcon(kind, color);
      headline.textContent = d.headline;
      headline.style.color = color;
      summary.textContent = d.summary;
      note.textContent = d.note;

      var hasPlan = P.hasRoute && P.legs.length > 0;
      state.hasPlan = hasPlan;
      while (plotBase.firstChild) plotBase.removeChild(plotBase.firstChild);
      while (plotDrawn.firstChild) plotDrawn.removeChild(plotDrawn.firstChild);
      while (plotStops.firstChild) plotStops.removeChild(plotStops.firstChild);
      state.stops = []; state.total = 0;
      sv('rect', { x: 0, y: PH - 3, width: PW, height: 3, fill: TRACK }, plotBase);

      if (!hasPlan) {
        // MissionCharts.ShowNoPlan: empty tracks, captions only, no number anywhere.
        tankCaption.textContent = 'OXYGEN'; tankCaption.style.color = C.soft;
        timeCaption.textContent = 'TIME';
        [tankUsed, tankSpare, tankReserve, timeTotal, timeWalk, timeWork].forEach(function (t) { t.textContent = ' '; });
        plotNumbers.textContent = ' ';
        segments(tankFill, []); segments(timeFill, []);
        placeLabels(); setProgress(1); fit(); fitHeadline();
        return;
      }

      // Oxygen: used, into the reserve (red), spare, reserve; what was never in the tank stays as the dark track.
      tankCaption.textContent = whatIf ? 'OXYGEN (estimate)  ·  tank at ' + Math.round(P.tankPercent) + ' %' : 'OXYGEN (estimate)';
      tankCaption.style.color = whatIf ? C.amber : C.soft;
      var t = tankParts(P, M);
      segments(tankFill, [
        { f: t.used, color: COL.used }, { f: t.intoReserve, color: COL.intoReserve },
        { f: t.spare, color: COL.spare }, { f: t.reserve, color: COL.reserve }]);
      // The reserve's mark: where the reserve starts in the tank this plan begins with.
      var mark = el('div', 'wb-mark', tankFill);
      mark.style.left = 'calc(' + ((Math.min(P.tankLiters, M.TANK_LITERS) - Math.min(M.RESERVE_LITERS, P.tankLiters)) / M.TANK_LITERS * 100) + '% - 2px)';
      tankUsed.textContent = 'USED ' + Math.round(P.usedLiters) + ' L';
      tankUsed.style.color = COL.used;
      if (P.spareLiters >= 0) { tankSpare.textContent = 'SPARE ' + Math.round(P.spareLiters) + ' L'; tankSpare.style.color = COL.spare; }
      else { tankSpare.textContent = 'SHORT ' + Math.round(-P.spareLiters) + ' L'; tankSpare.style.color = COL.intoReserve; }
      tankReserve.textContent = 'RESERVE ' + Math.round(M.RESERVE_LITERS) + ' L';
      tankReserve.style.color = COL.reserve;

      // Time: the legs and the work at each stop, in order.
      timeCaption.textContent = 'TIME (estimate)';
      timeTotal.textContent = 'TOTAL ' + minutesText(P.totalMinutes);
      var spans = timeline(P), tparts = [], last = 0;
      for (var i = 0; i < spans.length; i++) { tparts.push({ f: spans[i].end - last, color: spans[i].work ? COL.work : COL.walk }); last = spans[i].end; }
      segments(timeFill, tparts);
      timeWalk.textContent = 'WALK ' + minutesText(P.walkMinutes); timeWalk.style.color = COL.walk;
      timeWork.textContent = 'WORK ' + minutesText(P.workMinutes); timeWork.style.color = COL.work;

      // Height along the route.
      var prof = makeProfile(site, P, M);
      if (prof) {
        plotNumbers.textContent = metersText(prof.total) + '  ·  highest ' + signed(prof.highest) + ' m  ·  lowest ' + signed(prof.lowest) + ' m  ·  habitat = 0';
        var range = prof.highest - prof.lowest, top = 14, bottom = 8;
        var py = function (h) { var k = range > 0.5 ? (h - prof.lowest) / range : 0.5; return PH - bottom - k * (PH - top - bottom); };
        var px = function (m) { return prof.total > 0 ? m / prof.total * PW : 0; };
        sv('rect', { x: 0, y: py(0) - 1, width: PW, height: 2, fill: TRACK }, plotBase);    // the habitat's own height
        var line = [], j;
        for (j = 0; j < prof.meters.length; j++) line.push(px(prof.meters[j]).toFixed(1) + ',' + py(prof.height[j]).toFixed(1));
        sv('path', { d: 'M0,' + (PH - 3) + ' L' + line.join(' L') + ' L' + PW + ',' + (PH - 3) + ' Z', fill: PROFILE_FILL }, plotDrawn);
        sv('path', { d: 'M' + line.join(' L'), fill: 'none', stroke: COL.walk, 'stroke-width': 4, 'stroke-linejoin': 'round', 'stroke-linecap': 'round' }, plotDrawn);
        state.total = prof.total;
        for (j = 0; j < prof.stops.length; j++) {
          var s = prof.stops[j], x = px(s.meters), y = py(s.height), g = sv('g', {}, plotStops);
          sv('rect', { x: x - 1.5, y: y, width: 3, height: PH - y, fill: C.text }, g);
          sv('rect', { x: x - 7, y: y - 7, width: 14, height: 14, fill: C.text }, g);
          state.stops.push({ meters: s.meters, x: x, name: s.name, mark: g });
        }
      } else plotNumbers.textContent = ' ';
      placeLabels();
      setProgress(1);
      fit();
      fitHeadline();
      if (document.fonts && document.fonts.ready) document.fonts.ready.then(function () { placeLabels(); setProgress(state.anim); fitHeadline(); });
    }

    var raf = 0, done = 0;
    function animateIn() {
      if (raf) cancelAnimationFrame(raf);
      if (done) clearTimeout(done);
      raf = done = 0;
      if (reducedMotion() || !state.hasPlan) { setProgress(1); return; }
      // If frames are throttled (hidden page, busy GPU), the board still ends whole.
      done = setTimeout(function () { if (raf) cancelAnimationFrame(raf); raf = 0; setProgress(1); }, DURATION + 150);
      var t0 = 0;
      setProgress(0);
      function step(now) {
        if (!t0) t0 = now;
        var k = Math.min(1, (now - t0) / DURATION);
        setProgress(ease(k));
        raf = k < 1 ? requestAnimationFrame(step) : 0;
      }
      raf = requestAnimationFrame(step);
    }

    fit();
    return { render: render, animateIn: animateIn, element: root };
  }

  window.WalkbackBoard = { create: create };
})();
