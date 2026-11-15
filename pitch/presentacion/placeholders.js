// Written with Claude Code (AI), directed and tested by team MARSXR
// Walkback pitch: every {key} that content.js may use, built from the app's own numbers (walkback-model.js)
// and from content.js FACTS. Pure: no DOM, no three.js; runs in the browser and in Node (tools/check-content.js).
// Spanish writes the decimal comma, English the decimal point. A key ending in "En" is always English,
// so a Spanish slide can quote an English figure and the other way round.
// Every value that comes from the model is an ESTIMATE (plan B): the slide must say so next to it.
(function (root) {
  'use strict';

  function fmt(value, decimals, lang) {
    var s = Number(value).toFixed(decimals);
    return lang === 'es' ? s.replace('.', ',') : s;
  }
  /** HelmetHud.MinutesText: "43 min", "2 h 12 min". */
  function minutesText(m) {
    m = Math.round(m);
    return m < 60 ? m + ' min' : Math.floor(m / 60) + ' h ' + (m % 60) + ' min';
  }
  /** HelmetHud.MetersText for a route: "1.8 km" (one decimal). */
  function kmText(meters, lang) { return fmt(meters / 1000, 1, lang) + ' km'; }

  /**
   * computed(M, G, STOPS): everything the slides need from the model, computed once.
   * Returns { site, slope, back, plan, plan15, plan8, w8, w15 } (see walkback-model.js).
   */
  function computed(M, G, STOPS) {
    var site = M.siteFromGale(G); site.stops = STOPS;
    var slope = M.slopeDegrees(site);
    var plan = M.plan(site, slope, STOPS, 100), back = plan.home;
    return { site: site, slope: slope, back: back, plan: plan, plan15: M.plan(site, slope, STOPS, 15), plan8: M.plan(site, slope, STOPS, 8),
      w15: M.whatIf(back, 15), w8: M.whatIf(back, 8), full: M.whatIf(back, 100) };
  }

  /**
   * values(M, C, FACTS, lang): the map { key: text } for one language ('es' or 'en').
   * C is computed(). FACTS is WALKBACK_CONTENT.FACTS.
   */
  function values(M, C, FACTS, lang) {
    var P = C.plan, P8 = C.plan8, w8 = C.w8, v = {};
    var stops = C.site.stops, far = 0, farName = '';
    stops.forEach(function (s) { var m = C.back[M.nodeOf(C.site, s.x, s.y)]; if (m > far) { far = m; farName = s.name; } });
    var prof = M.profile(C.site, P), high = 0;
    prof.forEach(function (p) { if (p.height > high) high = p.height; });

    function both(key, es, en) { v[key] = lang === 'es' ? es : en; v[key + 'En'] = en; v[key + 'Es'] = es; }

    // ---- the plan at a full tank (ESTIMATE)
    both('km', kmText(P.walkMeters, 'es'), kmText(P.walkMeters, 'en'));
    v.meters = String(Math.round(P.walkMeters));
    v.walk = minutesText(P.walkMinutes);
    v.work = minutesText(P.workMinutes);
    v.total = minutesText(P.totalMinutes);
    v.usedL = String(Math.round(P.usedLiters));
    v.usedPct = String(Math.round(P.usedPercent));
    v.spareL = String(Math.round(P.spareLiters));
    v.tankL = String(M.TANK_LITERS);
    v.reserveL = String(M.RESERVE_LITERS);
    v.limit = String(M.SLOPE_LIMIT_DEGREES);
    v.stops = String(stops.length);
    v.verdict = M.describe(P).headline;
    v.summary = M.describe(P).summary;
    v.highM = String(Math.round(high));
    both('farStopMin', fmt(far, 1, 'es'), fmt(far, 1, 'en'));
    v.farStopName = farName;

    // ---- WHAT IF at 8 % (ESTIMATE)
    both('red8', fmt(w8.redPercent, 1, 'es'), fmt(w8.redPercent, 1, 'en'));          // of the ground that has a way back
    both('red8Whole', fmt(w8.beyond / C.back.length * 100, 1, 'es'), fmt(w8.beyond / C.back.length * 100, 1, 'en'));
    both('noWayPct', fmt(w8.noWayBack / C.back.length * 100, 1, 'es'), fmt(w8.noWayBack / C.back.length * 100, 1, 'en'));
    both('reach8', fmt(w8.reachMinutes, 1, 'es'), fmt(w8.reachMinutes, 1, 'en'));
    both('reach15', fmt(C.w15.reachMinutes, 1, 'es'), fmt(C.w15.reachMinutes, 1, 'en'));
    v.verdict8 = M.describe(P8).headline;
    v.summary8 = M.describe(P8).summary;
    v.short8L = String(Math.round(-P8.spareLiters));
    // which stops the oxygen still covers at 8 %, and which are in the red
    var red = [], ok = [];
    stops.forEach(function (s) { (C.back[M.nodeOf(C.site, s.x, s.y)] > w8.reachMinutes ? red : ok).push(s.name); });
    function list(names, and) { return names.length < 2 ? names.join('') : names.slice(0, -1).join(', ') + ' ' + and + ' ' + names[names.length - 1]; }
    both('redStops8', list(red, 'y'), list(red, 'and'));
    both('okStops8', list(ok, 'y'), list(ok, 'and'));
    // the tank level under which red first shows: the oxygen above the reserve covers less than the farthest ground with a way back
    var firstRed = (C.full.farthestMinutes * M.WALK_LITERS_PER_MINUTE + M.RESERVE_LITERS) / M.TANK_LITERS * 100;
    both('firstRedPct', fmt(firstRed, 1, 'es'), fmt(firstRed, 1, 'en'));

    // ---- FACTS (not from the model: each has its source in content.js)
    var F = FACTS || {};
    Object.keys(F).forEach(function (k) {
      if (v[k] !== undefined) return;
      v[k] = String(F[k]);
    });
    // FACTS written with a decimal comma get an English twin with a point, unless content.js gives one.
    Object.keys(F).forEach(function (k) {
      if (/En$/.test(k)) return;
      var es = String(F[k]), en = F[k + 'En'] !== undefined ? String(F[k + 'En']) : es.replace(/(\d),(\d)/g, '$1.$2');
      v[k + 'En'] = en;
      if (lang === 'en') v[k] = en;
    });
    return v;
  }

  /** fill('{km} out and back', map) -> text. Unknown keys are left as they are and reported in missing[]. */
  function fill(text, map, missing) {
    return String(text).replace(/\{([A-Za-z0-9]+)\}/g, function (all, key) {
      if (map[key] === undefined) { if (missing && missing.indexOf(key) < 0) missing.push(key); return all; }
      return map[key];
    });
  }

  /** Every string inside a slide's language block (also inside arrays), filled. Returns a deep copy. */
  function fillDeep(node, map, missing) {
    if (typeof node === 'string') return fill(node, map, missing);
    if (Array.isArray(node)) return node.map(function (x) { return fillDeep(x, map, missing); });
    if (node && typeof node === 'object') { var out = {}; Object.keys(node).forEach(function (k) { out[k] = fillDeep(node[k], map, missing); }); return out; }
    return node;
  }

  var api = { computed: computed, values: values, fill: fill, fillDeep: fillDeep, minutesText: minutesText, kmText: kmText, fmt: fmt };
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  root.WalkbackPlaceholders = api;
})(typeof window !== 'undefined' ? window : globalThis);
