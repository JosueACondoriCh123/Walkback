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
        /// <summary>The tag when the way back is read from the map's return layer (ReturnFallback) instead of a straight line.</summary>
        public const string LabelWithReturnMap = "ESTIMATE: way back over the relief, rest in straight lines, no walking model yet";
        /// <summary>Oxygen per minute of walking, from the same two test numbers (20 L and 19.3 min per km on flat ground).</summary>
        public const float WalkLitersPerMinute = FlatLitersPerKilometer / FlatMinutesPerKilometer;

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
            /// <summary>True when returnMinutes was read from the map's return layer, not a straight line.</summary>
            public bool returnFromMap;
            public string nextName;
            public Vector2 nextPoint;
            public float nextMeters;
            public float nextMinutes;
            public float nextOxygenPercent;
            public float taskMinutes;
            /// <summary>Height of the next target above this stop, from the relief (negative = downhill).</summary>
            public float nextClimbMeters;
            /// <summary>Walking minutes the oxygen above the reserve covers after the way back (negative = short).</summary>
            public float marginMinutes;
            public ReturnState returnState;
        }

        public static float Minutes(float meters) { return meters / 1000f * FlatMinutesPerKilometer; }
        public static float Liters(float meters) { return meters / 1000f * FlatLitersPerKilometer; }

        /// <summary>True when what is left above the reserve no longer covers the way back.</summary>
        public static bool TurnBack(float litersUsed, float returnLiters)
        {
            return TankLiters - litersUsed - ReserveLiters < returnLiters;
        }

        /// <summary>How well the oxygen left covers the way back: the colour of the minutes on the visor.</summary>
        public enum ReturnState { Covered, Tight, NotCovered }

        /// <summary>
        /// Amber starts when the oxygen above the reserve is less than this many times what the way back
        /// needs. A design choice of the visor (a proposal, not a number from the walking model).
        /// </summary>
        public const float TightFactor = 2f;

        /// <summary>
        /// Green: the oxygen above the reserve covers the way back at least twice. Amber: it covers it,
        /// but less than twice. Red: it does not cover it (the same rule as TurnBack).
        /// </summary>
        public static ReturnState StateOf(float litersAboveReserve, float returnLiters)
        {
            if (litersAboveReserve < returnLiters) return ReturnState.NotCovered;
            return litersAboveReserve < TightFactor * returnLiters ? ReturnState.Tight : ReturnState.Covered;
        }

        /// <summary>
        /// Minutes of walking that the oxygen above the reserve still covers after the way back
        /// (negative: the way back is short by that many minutes).
        /// </summary>
        public static float MarginMinutes(float litersAboveReserve, float returnLiters)
        {
            return (litersAboveReserve - returnLiters) / WalkLitersPerMinute;
        }

        public static float TaskMinutes(StopInfo stop)
        {
            float total = 0f;
            if (stop != null && stop.tasks != null)
                foreach (var t in stop.tasks) if (t != null) total += t.minutes;
            return total;
        }

        /// <summary>
        /// Same as AtStop(site, index), but the way back comes from the map's own return layer
        /// (<paramref name="returnMinutes"/>: minutes back from every node, row 0 = north), so the visor and
        /// the map on the table never show two different numbers for the same place. Oxygen for the way
        /// back is those minutes at the walking rate. If the map has no value for the stop's node (no way
        /// back on foot, or no map), the straight-line estimate stays.
        /// </summary>
        public static State AtStop(SiteData site, int index, float[] returnMinutes)
        {
            State s = AtStop(site, index);
            int n = site.GridSize;
            if (returnMinutes == null || returnMinutes.Length != n * n) return s;
            float spacing = site.NodeSpacing;
            int column = Mathf.Clamp(Mathf.RoundToInt(s.here.x / spacing), 0, n - 1);
            int row = Mathf.Clamp(Mathf.RoundToInt((site.SideMeters - s.here.y) / spacing), 0, n - 1);
            float minutes = returnMinutes[row * n + column];
            if (float.IsNaN(minutes) || float.IsInfinity(minutes)) return s;
            float used = TankLiters * (1f - s.oxygenLeftPercent / 100f);
            s.returnMinutes = minutes;
            s.returnLiters = minutes * WalkLitersPerMinute;
            s.turnBack = TurnBack(used, s.returnLiters);
            s.returnFromMap = true;
            s.marginMinutes = MarginMinutes(TankLiters - used - ReserveLiters, s.returnLiters);
            s.returnState = StateOf(TankLiters - used - ReserveLiters, s.returnLiters);
            return s;
        }

        /// <summary>
        /// The visor on arrival at a stop with the plan's own numbers, so the visor inside the 360° photo
        /// and the board on the table never disagree: walking minutes and meters from the plan's legs
        /// (the route over the relief, not straight lines), oxygen from the tank the plan starts with
        /// (a WHAT IF carries into the visor) and the same TURN BACK rule the verdict uses.
        /// Without a plan with a route, or for a stop the plan does not reach, the straight-line estimate stays.
        /// </summary>
        public static State AtStop(SiteData site, int index, float[] returnMinutes, MissionFallback.Plan plan)
        {
            State s = AtStop(site, index, returnMinutes);
            if (plan == null || !plan.HasRoute || s.stop < 0 || s.stop + 1 >= plan.Legs.Count || plan.Legs[s.stop].ToStop != s.stop) return s;
            float walk = 0f, work = 0f;
            for (int i = 0; i <= s.stop; i++)
            {
                walk += plan.Legs[i].Minutes;
                if (i < s.stop) work += plan.Legs[i].WorkMinutesAtEnd;
            }
            float used = walk * WalkLitersPerMinute + work * DesignLitersPerMinute;
            s.elapsedMinutes = walk + work;
            s.oxygenLeftPercent = Mathf.Max(0f, (plan.TankLiters - used) / TankLiters * 100f);
            MissionFallback.Leg next = plan.Legs[s.stop + 1];
            s.nextMeters = next.Meters;
            s.nextMinutes = next.Minutes;
            s.nextOxygenPercent = next.Minutes * WalkLitersPerMinute / TankLiters * 100f;
            float aboveReserve = plan.TankLiters - used - ReserveLiters;
            s.turnBack = aboveReserve < s.returnLiters;
            s.marginMinutes = MarginMinutes(aboveReserve, s.returnLiters);
            s.returnState = StateOf(aboveReserve, s.returnLiters);
            return s;
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
            s.marginMinutes = MarginMinutes(TankLiters - used - ReserveLiters, s.returnLiters);
            s.returnState = StateOf(TankLiters - used - ReserveLiters, s.returnLiters);
            if (site.Heights != null && site.Heights.Length == site.GridSize * site.GridSize)
                s.nextClimbMeters = RouteGeometry.HeightAt(site, s.nextPoint.x, s.nextPoint.y) - RouteGeometry.HeightAt(site, s.here.x, s.here.y);
            return s;
        }
    }
}
