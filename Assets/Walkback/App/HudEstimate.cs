// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// TODO(H2): replace with session C's Mission model (Assets/Walkback/Model/) when it lands on main.
    /// Until then the helmet HUD shows ESTIMATES built only from the walking model's own test numbers
    /// (ficha A): 1 km on flat ground = 19.3 min and 20 L of oxygen; the tank is 497 L (480 min at the
    /// 351 W design rate); the 31 L reserve is never touched; work at a stop spends at the design rate.
    /// Distances are straight lines habitat -> stops in order -> habitat. Slope and sand are ignored,
    /// so the HUD labels every number as an estimate (rule 1).
    /// </summary>
    public static class HudEstimate
    {
        public const float TankLiters = 497f;
        public const float ReserveLiters = 31f;
        public const float FlatLitersPerKilometer = 20f;
        public const float FlatMinutesPerKilometer = ReturnStandIn.FlatMinutesPerKilometer;
        public const float DesignLitersPerMinute = TankLiters / 480f;
        public const string Label = "ESTIMATE: straight lines on flat ground, not the walking model yet";

        /// <summary>What the astronaut's visor shows on arrival at a stop of the plan.</summary>
        public struct State
        {
            public int stop;
            public string stopName;
            public Vector2 here;
            public float elapsedMinutes;
            public float oxygenLeftPercent;
            public float returnMinutes;
            public float returnLiters;
            public bool turnBack;
            public string nextName;
            public Vector2 nextPoint;
            public float nextMeters;
            public float nextMinutes;
            public float nextOxygenPercent;
            public float taskMinutes;
        }

        public static float Minutes(float meters) { return meters / 1000f * FlatMinutesPerKilometer; }
        public static float Liters(float meters) { return meters / 1000f * FlatLitersPerKilometer; }

        /// <summary>True when what is left above the reserve no longer covers the way back.</summary>
        public static bool TurnBack(float litersUsed, float returnLiters)
        {
            return TankLiters - litersUsed - ReserveLiters < returnLiters;
        }

        public static float TaskMinutes(StopInfo stop)
        {
            float total = 0f;
            if (stop != null && stop.tasks != null)
                foreach (var t in stop.tasks) if (t != null) total += t.minutes;
            return total;
        }

        /// <summary>On arrival at stop <paramref name="index"/>, walking habitat -> stops in order (work done at the earlier stops).</summary>
        public static State AtStop(SiteData site, int index)
        {
            var stops = site.Stops;
            index = Mathf.Clamp(index, 0, stops.Length - 1);
            Vector2 previous = site.Habitat;
            float walked = 0f, work = 0f;
            for (int i = 0; i <= index; i++)
            {
                var p = new Vector2(stops[i].x, stops[i].y);
                walked += Vector2.Distance(previous, p);
                previous = p;
                if (i < index) work += TaskMinutes(stops[i]);
            }
            float used = Liters(walked) + work * DesignLitersPerMinute;
            var s = new State
            {
                stop = index,
                stopName = stops[index].name,
                here = previous,
                elapsedMinutes = Minutes(walked) + work,
                oxygenLeftPercent = Mathf.Max(0f, (TankLiters - used) / TankLiters * 100f),
                taskMinutes = TaskMinutes(stops[index]),
            };
            float back = Vector2.Distance(s.here, site.Habitat);
            s.returnMinutes = Minutes(back);
            s.returnLiters = Liters(back);
            s.turnBack = TurnBack(used, s.returnLiters);
            bool last = index == stops.Length - 1;
            s.nextName = last ? "Habitat" : stops[index + 1].name;
            s.nextPoint = last ? site.Habitat : new Vector2(stops[index + 1].x, stops[index + 1].y);
            s.nextMeters = Vector2.Distance(s.here, s.nextPoint);
            s.nextMinutes = Minutes(s.nextMeters);
            s.nextOxygenPercent = Liters(s.nextMeters) / TankLiters * 100f;
            return s;
        }
    }
}
