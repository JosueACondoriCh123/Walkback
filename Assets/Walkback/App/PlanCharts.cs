// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// The numbers behind the charts on the mission board. Nothing new is computed about the mission:
    /// every value is read from the plan already made (MissionFallback.Plan: its route, its legs, its
    /// oxygen) and from the site's relief. So the charts are as much an ESTIMATE as the verdict card.
    ///   - Profile: height along the route, relative to the habitat, from the terrain's heights.
    ///   - At: minutes and oxygen spent when the route is drawn up to a fraction of its length
    ///     (the charts fill in step with the route drawing itself on the map).
    ///   - TankAt: how a full tank splits into used, spare, reserve and (WHAT IF) what was never there.
    ///   - Timeline: walking and work in the order they happen.
    /// Pure functions: no scene needed, the automatic test calls them directly.
    /// </summary>
    public static class PlanCharts
    {
        public class Profile
        {
            /// <summary>Route meters from the habitat at each point.</summary>
            public float[] Meters = new float[0];
            /// <summary>Height at each point, in meters above the habitat (negative = below it).</summary>
            public float[] Height = new float[0];
            public float TotalMeters;
            public float Highest;
            public float Lowest;
            /// <summary>One entry per stop of the plan, in order: where it is along the route, how high, its name.</summary>
            public float[] StopMeters = new float[0];
            public float[] StopHeight = new float[0];
            public string[] StopNames = new string[0];
        }

        /// <summary>Height along the plan's route, read from the relief at every point of the route.</summary>
        public static Profile MakeProfile(SiteData site, MissionFallback.Plan plan)
        {
            var profile = new Profile();
            if (site == null || plan == null || plan.Path.Count < 2) return profile;
            int count = plan.Path.Count;
            profile.Meters = new float[count];
            profile.Height = new float[count];
            float home = RouteGeometry.HeightAt(site, plan.Path[0].x, plan.Path[0].y);
            for (int i = 0; i < count; i++)
            {
                if (i > 0) profile.Meters[i] = profile.Meters[i - 1] + Vector2.Distance(plan.Path[i - 1], plan.Path[i]);
                profile.Height[i] = RouteGeometry.HeightAt(site, plan.Path[i].x, plan.Path[i].y) - home;
                profile.Highest = Mathf.Max(profile.Highest, profile.Height[i]);
                profile.Lowest = Mathf.Min(profile.Lowest, profile.Height[i]);
            }
            profile.TotalMeters = profile.Meters[count - 1];

            var meters = new List<float>();
            var heights = new List<float>();
            var names = new List<string>();
            foreach (MissionFallback.Leg leg in plan.Legs)
            {
                if (leg.ToStop < 0 || leg.LastPoint < 0 || leg.LastPoint >= count) continue;
                meters.Add(profile.Meters[leg.LastPoint]);
                heights.Add(profile.Height[leg.LastPoint]);
                names.Add(leg.ToName);
            }
            profile.StopMeters = meters.ToArray();
            profile.StopHeight = heights.ToArray();
            profile.StopNames = names.ToArray();
            return profile;
        }

        /// <summary>What has been spent when the route is drawn up to a point.</summary>
        public struct Moment
        {
            public float walkMinutes;
            public float workMinutes;
            public float usedLiters;
            public float TotalMinutes { get { return walkMinutes + workMinutes; } }
        }

        /// <summary>
        /// Minutes and oxygen when the route is drawn up to <paramref name="routeFraction"/> (0..1) of its
        /// length: each leg's minutes in proportion to how much of it is drawn, and the work of every stop
        /// already reached. At 1 it is the plan's own totals.
        /// </summary>
        public static Moment At(MissionFallback.Plan plan, float routeFraction)
        {
            var m = new Moment();
            if (plan == null || plan.Legs.Count == 0) return m;
            float total = 0f;
            foreach (MissionFallback.Leg leg in plan.Legs) total += leg.Meters;
            float drawn = Mathf.Clamp01(routeFraction) * total, start = 0f;
            bool whole = routeFraction >= 1f;
            foreach (MissionFallback.Leg leg in plan.Legs)
            {
                float end = start + leg.Meters;
                if (whole || drawn >= end - 1e-3f)
                {
                    m.walkMinutes += leg.Minutes;
                    m.workMinutes += leg.WorkMinutesAtEnd;
                }
                else
                {
                    if (drawn > start && leg.Meters > 1e-4f) m.walkMinutes += leg.Minutes * (drawn - start) / leg.Meters;
                    break;
                }
                start = end;
            }
            m.usedLiters = m.walkMinutes * HudEstimate.WalkLitersPerMinute + m.workMinutes * HudEstimate.DesignLitersPerMinute;
            return m;
        }

        /// <summary>A full tank as five parts that add up to 1.</summary>
        public struct Tank
        {
            /// <summary>Breathed, out of the oxygen above the reserve.</summary>
            public float used;
            /// <summary>Breathed out of the reserve (only when the plan runs short).</summary>
            public float intoReserve;
            /// <summary>Left above the reserve.</summary>
            public float spare;
            /// <summary>What is left of the reserve.</summary>
            public float reserve;
            /// <summary>Not in the tank at the start (WHAT IF).</summary>
            public float missing;
            /// <summary>Liters the plan asks for beyond the whole tank (0 unless it runs dry).</summary>
            public float shortLiters;
        }

        /// <summary>The tank the plan starts with, after <paramref name="usedLiters"/> have been breathed.</summary>
        public static Tank TankAt(MissionFallback.Plan plan, float usedLiters)
        {
            float full = HudEstimate.TankLiters;
            float tank = plan != null ? Mathf.Clamp(plan.TankLiters, 0f, full) : full;
            float reserve = Mathf.Min(HudEstimate.ReserveLiters, tank);
            float usable = tank - reserve;
            usedLiters = Mathf.Max(0f, usedLiters);
            float fromUsable = Mathf.Min(usedLiters, usable);
            float fromReserve = Mathf.Clamp(usedLiters - usable, 0f, reserve);
            return new Tank
            {
                used = fromUsable / full,
                intoReserve = fromReserve / full,
                spare = (usable - fromUsable) / full,
                reserve = (reserve - fromReserve) / full,
                missing = (full - tank) / full,
                shortLiters = Mathf.Max(0f, usedLiters - tank),
            };
        }

        /// <summary>One stretch of the mission's time: walking a leg, or working at a stop.</summary>
        public struct Span
        {
            /// <summary>Start and end as fractions of the plan's total time.</summary>
            public float start;
            public float end;
            public bool work;
            public float minutes;
            public string name;
        }

        /// <summary>Walking and work in the order they happen: leg, work at its stop, next leg... the last leg home.</summary>
        public static List<Span> Timeline(MissionFallback.Plan plan)
        {
            var spans = new List<Span>();
            if (plan == null || plan.Legs.Count == 0) return spans;
            float total = 0f;
            foreach (MissionFallback.Leg leg in plan.Legs) total += leg.Minutes + leg.WorkMinutesAtEnd;
            if (total <= 0f) return spans;
            float at = 0f;
            foreach (MissionFallback.Leg leg in plan.Legs)
            {
                spans.Add(new Span { start = at / total, end = (at + leg.Minutes) / total, work = false, minutes = leg.Minutes, name = leg.FromName + " -> " + leg.ToName });
                at += leg.Minutes;
                if (leg.WorkMinutesAtEnd <= 0f) continue;
                spans.Add(new Span { start = at / total, end = (at + leg.WorkMinutesAtEnd) / total, work = true, minutes = leg.WorkMinutesAtEnd, name = leg.ToName });
                at += leg.WorkMinutesAtEnd;
            }
            return spans;
        }
    }
}
