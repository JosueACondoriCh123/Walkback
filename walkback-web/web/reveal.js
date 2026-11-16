// Written with Claude Code (AI), directed and tested by team MARSXR
//
// Two small things as the page scrolls: sections and cards fade up the first time they come
// on screen, and the headset clips play only while they are on screen (muted, looped).
// With less motion asked, nothing fades and the clips show their controls instead of autoplaying.
(() => {
  if (!("IntersectionObserver" in window)) return;
  const still = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  if (!still) {
    const items = document.querySelectorAll(
      ".section-head, .problem-card, .how, .flow-step, .stop-card, .plan-kpi, .phase, .shot, .member, .faq-item, .tested, .xr-box, .map-grid",
    );
    document.documentElement.classList.add("js-reveal");
    const seen = new IntersectionObserver((entries) => {
      for (const e of entries) {
        if (!e.isIntersecting) continue;
        // Cards in the same row come in one after the other.
        const siblings = [...e.target.parentElement.children].filter((c) => c.classList.contains("reveal"));
        e.target.style.transitionDelay = `${Math.min(siblings.indexOf(e.target), 5) * 70}ms`;
        e.target.classList.add("is-in");
        seen.unobserve(e.target);
      }
    }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });
    items.forEach((el) => { el.classList.add("reveal"); seen.observe(el); });
  }

  const clips = document.querySelectorAll("video.phase-clip");
  if (still) {
    clips.forEach((v) => { v.controls = true; });
    return;
  }
  const play = new IntersectionObserver((entries) => {
    for (const e of entries) {
      if (e.isIntersecting) e.target.play().catch(() => { e.target.controls = true; });
      else e.target.pause();
    }
  }, { threshold: 0.35 });
  clips.forEach((v) => play.observe(v));
})();
