// Written with Claude (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Walkback
{
    /// <summary>
    /// PLAN B for the route and the verdict, used only until session C's model (RoutePlanner, Verdict,
    /// SuitFailure in Assets/Walkback/Model/) lands. It answers the app's question with what is already
    /// in the project, and every number it gives is labelled ESTIMATE on screen:
    ///   - The route: habitat -> stops in order -> habitat, each leg the fastest way over the relief
    ///     (ReturnFallback: Tobler slope pace on the walking model's flat-ground test pace, ground over
    ///     20° never stepped on).
    ///   - Oxygen: walking spends HudEstimate.WalkLitersPerMinute for every minute walked (so a slower,
    ///     steeper leg costs more); work at a stop spends HudEstimate.DesignLitersPerMinute.
    ///   - The verdict: VIABLE only if, on arrival at every stop and again after its work, the oxygen
    ///     left above the reserve still covers the walk back to the habitat from there, and the plan
    ///     ends with the reserve untouched.
    ///   - WHAT IF: the same plan starting with part of a tank (a hand-set "the suit failed" case).
    /// It is not the team's walking model: no suit metabolic model, no sand, no gravity term.
    /// Pure functions: no scene needed, the automatic test calls them directly.
    /// </summary>
    public static class MissionFallback
    {
        public const string Note = "ESTIMATE: plan B over the relief, not the walking model yet";

        public class Plan
        {
            /// <summary>False when a leg has no way under the slope limit; BrokenLeg names it.</summary>
            public bool HasRoute = true;
            public string BrokenLeg = "";
            /// <summary>The route in local meters (x east, y north), habitat to habitat (or up to the broken leg).</summary>
            public readonly List<Vector2> Path = new List<Vector2>();
            public float WalkMeters;
            public float WalkMinutes;
            public float WorkMinutes;
            /// <summary>Percent of a full tank the plan starts with (100 unless it is a WHAT IF).</summary>
            public float TankPercent = 100f;
            public float TankLiters;
            public float UsedLiters;
            /// <summary>Liters left above the reserve back at the habitat (negative = the plan runs short).</summary>
            public float SpareLiters;
            /// <summary>First stop where the oxygen left above the reserve no longer covers the walk back, or -1.</summary>
            public int FailsAtStop = -1;
            /// <summary>True if it already fails on arrival at that stop; false if only after its work.</summary>
            public bool FailsOnArrival;
            public string FailsAtName = "";

            public float TotalMinutes { get { return WalkMinutes + WorkMinutes; } }
            public bool Viable { get { return HasRoute && FailsAtStop < 0 && SpareLiters >= 0f; } }
        }

        /// <summary>Minutes of walking that the oxygen above the reserve covers, starting with this much of a tank.</summary>
        public static float ReachMinutes(float tankPercent)
        {
            float liters = HudEstimate.TankLiters * tankPercent / 100f - HudEstimate.ReserveLiters;
            return Mathf.Max(0f, liters / HudEstimate.WalkLitersPerMinute);
        }

        public static Plan Make(SiteData site, float[] slopeDegrees, float tankPercent)
        {
            var plan = new Plan { TankPercent = tankPercent, TankLiters = HudEstimate.TankLiters * tankPercent / 100f };
            StopInfo[] stops = site.Stops ?? new StopInfo[0];
            int n = site.GridSize;

            // Minutes home from every node: the walk back from each stop, and the way of the last leg.
            var homeNext = new int[n * n];
            float[] home = ReturnFallback.MinutesTo(site, slopeDegrees, site.Habitat, homeNext);

            Vector2 from = site.Habitat;
            string fromName = "Habitat";
            float used = 0f;
            plan.Path.Add(from);
            var next = new int[n * n];
            for (int leg = 0; leg <= stops.Length; leg++)
            {
                bool lastLeg = leg == stops.Length;
                Vector2 to = lastLeg ? site.Habitat : new Vector2(stops[leg].x, stops[leg].y);
                string toName = lastLeg ? "Habitat" : StopName(stops[leg], leg);

                float[] minutes = lastLeg ? home : ReturnFallback.MinutesTo(site, slopeDegrees, to, next);
                int[] way = lastLeg ? homeNext : next;
                int node = ReturnFallback.NodeOf(site, from);
                float legMinutes = minutes[node];
                if (float.IsInfinity(legMinutes) || float.IsNaN(legMinutes))
                {
                    plan.HasRoute = false;
                    plan.BrokenLeg = fromName + " -> " + toName;
                    break;
                }

                // The leg's way: node by node to the target's node, then the target itself.
                Vector2 previous = from;
                int guard = n * n;
                while (node >= 0 && guard-- > 0)
                {
                    Vector2 p = site.NodeToMeters(node % n, node / n);
                    plan.Path.Add(p);
                    plan.WalkMeters += Vector2.Distance(previous, p);
                    previous = p;
                    node = way[node];
                }
                plan.Path.Add(to);
                plan.WalkMeters += Vector2.Distance(previous, to);

                plan.WalkMinutes += legMinutes;
                used += legMinutes * HudEstimate.WalkLitersPerMinute;

                if (!lastLeg)
                {
                    float backMinutes = home[ReturnFallback.NodeOf(site, to)];
                    if (float.IsInfinity(backMinutes) || float.IsNaN(backMinutes))
                    {
                        plan.HasRoute = false;
                        plan.BrokenLeg = toName + " -> Habitat";
                        break;
                    }
                    float backLiters = backMinutes * HudEstimate.WalkLitersPerMinute;
                    if (plan.FailsAtStop < 0 && plan.TankLiters - used - HudEstimate.ReserveLiters < backLiters)
                    {
                        plan.FailsAtStop = leg;
                        plan.FailsOnArrival = true;
                        plan.FailsAtName = toName;
                    }
                    float work = HudEstimate.TaskMinutes(stops[leg]);
                    plan.WorkMinutes += work;
                    used += work * HudEstimate.DesignLitersPerMinute;
                    if (plan.FailsAtStop < 0 && plan.TankLiters - used - HudEstimate.ReserveLiters < backLiters)
                    {
                        plan.FailsAtStop = leg;
                        plan.FailsOnArrival = false;
                        plan.FailsAtName = toName;
                    }
                }
                from = to;
                fromName = toName;
            }
            plan.UsedLiters = used;
            plan.SpareLiters = plan.TankLiters - used - HudEstimate.ReserveLiters;
            return plan;
        }

        /// <summary>The verdict card for a plan: state, headline (null = the state's default), the numbers line and the note.</summary>
        public static void Describe(Plan plan, out VerdictState state, out string headline, out string summary, out string note)
        {
            bool whatIf = plan.TankPercent < 100f;
            note = whatIf ? "WHAT IF: tank at " + F(plan.TankPercent, "0") + " %. ESTIMATE, not the walking model yet" : Note;
            headline = null;
            if (!plan.HasRoute)
            {
                state = VerdictState.NotViable;
                headline = "NO SAFE ROUTE";
                summary = "No way under " + F(TerrainLayers.SlopeLimitDegrees, "0") + "°: " + plan.BrokenLeg;
                return;
            }
            if (plan.Viable)
            {
                state = VerdictState.Viable;
                summary = HelmetHud.MinutesText(plan.TotalMinutes) + "  ·  O2 used " + F(plan.UsedLiters / HudEstimate.TankLiters * 100f, "0") +
                          " %  ·  spare " + F(plan.SpareLiters, "0") + " L";
                return;
            }
            state = VerdictState.NotViable;
            if (plan.FailsAtStop >= 0)
                summary = plan.FailsOnArrival
                    ? "No oxygen to walk back from " + plan.FailsAtName
                    : "No oxygen to walk back after the work at " + plan.FailsAtName;
            else
                summary = "The plan ends " + F(-plan.SpareLiters, "0") + " L into the reserve";
        }

        /// <summary>One line for the app's log.</summary>
        public static string LogLine(Plan plan)
        {
            if (!plan.HasRoute) return "no safe route, " + plan.BrokenLeg;
            return (plan.Viable ? "VIABLE" : "NOT VIABLE") + ", tank " + F(plan.TankPercent, "0") + " %, walk " + F(plan.WalkMeters, "0") + " m in " +
                   F(plan.WalkMinutes, "0.0") + " min, work " + F(plan.WorkMinutes, "0") + " min, O2 used " + F(plan.UsedLiters, "0.0") + " L, spare " +
                   F(plan.SpareLiters, "0.0") + " L" + (plan.FailsAtStop >= 0 ? ", fails at " + plan.FailsAtName + (plan.FailsOnArrival ? " on arrival" : " after work") : "");
        }

        static string StopName(StopInfo stop, int index)
        {
            return stop != null && !string.IsNullOrEmpty(stop.name) ? stop.name : "Stop " + (index + 1);
        }

        static string F(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
