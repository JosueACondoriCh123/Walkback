// Written with Claude Code (AI), directed and tested by team MARSXR
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
    ///     20° never stepped on). That way moves node to node in 8 directions, so it zigzags; Smooth
    ///     replaces stretches of it with straight lines where the straight line is walkable by the same
    ///     rules and no slower. The plan's meters and minutes are those of the smoothed route.
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
            /// <summary>The same legs before smoothing, node to node on the grid (to say how much the smoothing changed).</summary>
            public float GridMeters;
            public float GridMinutes;
            /// <summary>How many stretches of the grid way were replaced by a straight line.</summary>
            public int Shortcuts;
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

            /// <summary>One entry per leg walked, in order: habitat -> stop 1, stop 1 -> stop 2 ... last stop -> habitat.</summary>
            public readonly List<Leg> Legs = new List<Leg>();

            public float TotalMinutes { get { return WalkMinutes + WorkMinutes; } }
            public bool Viable { get { return HasRoute && FailsAtStop < 0 && SpareLiters >= 0f; } }
        }

        /// <summary>One leg of the plan: where it starts and ends in Plan.Path, and what it costs.</summary>
        public class Leg
        {
            public string FromName = "";
            public string ToName = "";
            /// <summary>Index into site.Stops of the stop this leg ends at, or -1 for the habitat.</summary>
            public int ToStop = -1;
            /// <summary>Indices into Plan.Path of the leg's first and last point (the next leg starts where this one ends).</summary>
            public int FirstPoint;
            public int LastPoint;
            public float Meters;
            public float Minutes;
            /// <summary>Minutes of work at the stop this leg ends at (0 for the habitat).</summary>
            public float WorkMinutesAtEnd;
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

                // The leg's way: node by node to the target's node, then the target itself, each point
                // with the minutes left from it to the target.
                int legFirstPoint = plan.Path.Count - 1;
                float legStartMeters = plan.WalkMeters;
                var points = new List<Vector2> { from };
                var left = new List<float> { legMinutes };
                int guard = n * n;
                while (node >= 0 && guard-- > 0)
                {
                    points.Add(site.NodeToMeters(node % n, node / n));
                    left.Add(minutes[node]);
                    node = way[node];
                }
                points.Add(to);
                left.Add(0f);
                plan.GridMeters += RouteGeometry.Length(points);
                plan.GridMinutes += legMinutes;

                // Straight lines where the zigzag of the grid is not needed; the leg's minutes are the smoothed way's.
                plan.Shortcuts += Smooth(site, slopeDegrees, points, left);
                legMinutes = left[0];

                // The path keeps a point every grid step at most, as before (the ribbon and the progress along
                // the route read it). A grid step stays as it is; a straight line gets every second point that
                // StraightMinutes read on it, so each point added is one that was checked as walkable.
                float gridStep = site.NodeSpacing * Mathf.Sqrt(2f) + 0.001f;
                for (int i = 1; i < points.Count; i++)
                {
                    Vector2 a = points[i - 1], b = points[i];
                    float meters = Vector2.Distance(a, b);
                    if (meters > gridStep)
                    {
                        int steps = Mathf.Max(1, Mathf.CeilToInt(meters / (site.NodeSpacing / 2f)));
                        for (int k = 2; k < steps; k += 2) plan.Path.Add(Vector2.Lerp(a, b, k / (float)steps));
                    }
                    plan.Path.Add(b);
                    plan.WalkMeters += meters;
                }

                plan.WalkMinutes += legMinutes;
                used += legMinutes * HudEstimate.WalkLitersPerMinute;
                plan.Legs.Add(new Leg
                {
                    FromName = fromName,
                    ToName = toName,
                    ToStop = lastLeg ? -1 : leg,
                    FirstPoint = legFirstPoint,
                    LastPoint = plan.Path.Count - 1,
                    Meters = plan.WalkMeters - legStartMeters,
                    Minutes = legMinutes,
                    WorkMinutesAtEnd = lastLeg ? 0f : HudEstimate.TaskMinutes(stops[leg]),
                });

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

        /// <summary>A straight line may replace this many points of the grid way at most (30 nodes are 117 to 166 m on the Gale grid).</summary>
        public const int SmoothWindow = 30;
        /// <summary>Passes over a leg: each one can join stretches the pass before left as separate lines.</summary>
        public const int SmoothPasses = 3;

        /// <summary>
        /// Takes the zigzag out of a leg. <paramref name="points"/> is the grid way (start, nodes, target)
        /// and <paramref name="left"/> the minutes left to the target at each point. From each point it
        /// looks for the farthest point ahead (up to SmoothWindow) that a straight line reaches by
        /// StraightMinutes' rules in no more minutes than the grid way takes, and drops the points between.
        /// So a smoothed leg is never slower and never longer than the grid leg, never crosses no-go
        /// ground, and still cannot be shorter than the straight line between its ends.
        /// Both lists are replaced by the smoothed way. Returns how many shortcuts it took.
        /// </summary>
        public static int Smooth(SiteData site, float[] slopeDegrees, List<Vector2> points, List<float> left)
        {
            int shortcuts = 0;
            for (int pass = 0; pass < SmoothPasses; pass++)
            {
                int last = points.Count - 1;
                var kept = new List<Vector2> { points[0] };
                var cost = new List<float> { 0f };
                bool found = false;
                int i = 0;
                while (i < last)
                {
                    int best = i + 1;
                    float bestMinutes = Mathf.Max(0f, left[i] - left[i + 1]);
                    for (int j = Mathf.Min(last, i + SmoothWindow); j > i + 1; j--)
                    {
                        float straight;
                        if (!StraightMinutes(site, slopeDegrees, points[i], points[j], out straight)) continue;
                        if (straight > left[i] - left[j] + 1e-4f) continue;
                        best = j;
                        bestMinutes = straight;
                        shortcuts++;
                        found = true;
                        break;
                    }
                    kept.Add(points[best]);
                    cost.Add(bestMinutes);
                    i = best;
                }

                // Minutes left at each kept point, counted back from the target.
                double total = 0.0;
                for (int q = 1; q < cost.Count; q++) total += cost[q];
                points.Clear();
                left.Clear();
                double walked = 0.0;
                for (int q = 0; q < kept.Count; q++)
                {
                    walked += cost[q];
                    points.Add(kept[q]);
                    left.Add(Mathf.Max(0f, (float)(total - walked)));
                }
                if (!found) break;
            }
            return shortcuts;
        }

        /// <summary>
        /// Minutes to walk the straight line from a to b by the same rules as the grid (ReturnFallback):
        /// same flat pace and slope factor, no step steeper than the limit, and no point of the line
        /// inside a cell that has a no-go corner. It reads the ground every quarter grid step (about 1 m), so a
        /// shortcut cannot clip the corner of no-go ground between two readings.
        /// False when the line is not walkable.
        /// </summary>
        public static bool StraightMinutes(SiteData site, float[] slopeDegrees, Vector2 a, Vector2 b, out float minutes)
        {
            minutes = 0f;
            float meters = Vector2.Distance(a, b);
            if (meters < 1e-6f) return true;
            if (!Walkable(site, slopeDegrees, a)) return false;
            float maxGrade = Mathf.Tan(TerrainLayers.SlopeLimitDegrees * Mathf.Deg2Rad);
            float pace = ReturnStandIn.FlatMinutesPerKilometer / 1000f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(meters / (site.NodeSpacing / 4f)));
            float step = meters / steps;
            float previous = RouteGeometry.HeightAt(site, a.x, a.y);
            double total = 0.0;
            for (int k = 1; k <= steps; k++)
            {
                Vector2 p = Vector2.Lerp(a, b, k / (float)steps);
                if (!Walkable(site, slopeDegrees, p)) return false;
                float height = RouteGeometry.HeightAt(site, p.x, p.y);
                float grade = (height - previous) / step;
                if (Mathf.Abs(grade) > maxGrade) return false;
                total += step * pace / ReturnFallback.SlopeFactor(grade);
                previous = height;
            }
            minutes = (float)total;
            return true;
        }

        /// <summary>True when the four nodes around a point are all within the slope limit.</summary>
        public static bool Walkable(SiteData site, float[] slopeDegrees, Vector2 p)
        {
            int n = site.GridSize;
            float spacing = site.NodeSpacing;
            int c0 = Mathf.Min((int)Mathf.Clamp(p.x / spacing, 0f, n - 1), n - 2);
            int r0 = Mathf.Min((int)Mathf.Clamp((site.SideMeters - p.y) / spacing, 0f, n - 1), n - 2);
            float limit = TerrainLayers.SlopeLimitDegrees;
            return slopeDegrees[r0 * n + c0] <= limit && slopeDegrees[r0 * n + c0 + 1] <= limit &&
                   slopeDegrees[(r0 + 1) * n + c0] <= limit && slopeDegrees[(r0 + 1) * n + c0 + 1] <= limit;
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
                   F(plan.SpareLiters, "0.0") + " L, grid way " + F(plan.GridMeters, "0") + " m in " + F(plan.GridMinutes, "0.0") + " min before " + plan.Shortcuts + " shortcuts" + (plan.FailsAtStop >= 0 ? ", fails at " + plan.FailsAtName + (plan.FailsOnArrival ? " on arrival" : " after work") : "");
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
