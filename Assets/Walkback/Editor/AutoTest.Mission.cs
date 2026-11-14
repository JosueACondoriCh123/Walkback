// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;

namespace Walkback.EditorTools
{
    /// <summary>Mission mode: the progress along the plan (pure checks) and the mode itself in the scene.</summary>
    public static partial class AutoTest
    {
        static partial void CheckMission(WalkbackApp app)
        {
            CheckMissionProgress();
        }

        /// <summary>Flat ground, habitat in the south-west corner, one stop 1 km east with 10 minutes of work.</summary>
        static SiteData MissionTestSite()
        {
            var site = SiteLoader.Synthetic();
            for (int i = 0; i < site.Heights.Length; i++) site.Heights[i] = 0f;
            site.Info.habitat = new SitePoint { x = 0f, y = 0f };
            site.Stops = new[] { new StopInfo { id = "a", name = "A", x = 1000f, y = 0f, tasks = new[] { new StopTask { name = "t", minutes = 10f } } } };
            return site;
        }

        static void CheckMissionProgress()
        {
            SiteData site = MissionTestSite();
            var slope = new float[site.GridSize * site.GridSize];
            float[] home = ReturnFallback.Minutes(site, slope);
            var plan = MissionFallback.Make(site, slope, 100f);
            float walkRate = HudEstimate.WalkLitersPerMinute, workRate = HudEstimate.DesignLitersPerMinute;

            Check("Mission plan: one leg per stop and one back home, each with its meters and minutes",
                plan.Legs.Count == 2 && plan.Legs[0].ToStop == 0 && plan.Legs[1].ToStop == -1 && plan.Legs[0].ToName == "A" && plan.Legs[1].ToName == "Habitat" &&
                Mathf.Abs(plan.Legs[0].Meters - 1000f) < 0.5f && Mathf.Abs(plan.Legs[0].Minutes - 19.3f) < 0.03f && Mathf.Approximately(plan.Legs[0].WorkMinutesAtEnd, 10f) &&
                plan.Legs[1].FirstPoint == plan.Legs[0].LastPoint && plan.Legs[1].LastPoint == plan.Path.Count - 1,
                plan.Legs.Count + " legs, first " + plan.Legs[0].Meters.ToString("F1") + " m in " + plan.Legs[0].Minutes.ToString("F2") + " min");

            var progress = new MissionProgress(site, plan, home);
            var s = progress.Read(site.Habitat);
            Check("Mission progress: at the habitat nothing is spent and the next stop is the whole leg away",
                progress.Valid && s.usedLiters < 0.001f && Mathf.Abs(s.oxygenLeftPercent - 100f) < 0.01f && s.nextName == "A" && s.nextStop == 0 &&
                Mathf.Abs(s.nextMeters - 1000f) < 0.5f && Mathf.Abs(s.nextMinutes - 19.3f) < 0.03f && !s.turnBack && s.canJump,
                "next " + s.nextName + " " + s.nextMeters.ToString("F1") + " m");

            // The jump lands 5 m before the stop and counts as walked: 995 m of the 1000 m leg.
            bool jumped = progress.JumpNext();
            Vector2 landing = progress.PointAt(progress.RouteMeters);
            s = progress.Read(landing);
            float walked995 = 19.3f * 0.995f;
            Check("Mission jump: lands 5 m before the stop, looking at it", jumped && Mathf.Abs(landing.x - 995f) < 0.05f && Mathf.Abs(landing.y) < 0.05f &&
                Vector2.Dot(progress.DirectionToTarget(), Vector2.right) > 0.999f, landing.x.ToString("F2") + ", " + landing.y.ToString("F2"));
            Check("Mission jump: the stretch jumped over counts as walked (its minutes and oxygen, from the plan)",
                Mathf.Abs(s.walkMinutes - walked995) < 0.03f && Mathf.Abs(s.usedLiters - walked995 * walkRate) < 0.05f && Mathf.Abs(s.nextMeters - 5f) < 0.05f,
                s.walkMinutes.ToString("F2") + " min, " + s.usedLiters.ToString("F2") + " L");
            Check("Mission progress: the way back is read from the map's return layer at the node you stand on",
                !s.noWayBack && Mathf.Abs(s.returnMinutes - home[ReturnFallback.NodeOf(site, landing)]) < 0.001f && Mathf.Abs(s.returnMinutes - walked995) < 0.1f,
                s.returnMinutes.ToString("F2") + " min");

            // Walking for real: 2 m forward moves the numbers by 2 m; a step back gives nothing back.
            progress.Walk(new Vector2(997f, 0.3f));
            var forward = progress.Read(new Vector2(997f, 0.3f));
            progress.Walk(new Vector2(996f, 0f));
            var back = progress.Read(new Vector2(996f, 0f));
            Check("Mission walk: the headset's position moves the numbers (2 m walked = 2 m less to the stop)",
                Mathf.Abs(forward.routeMeters - 997f) < 0.05f && Mathf.Abs(forward.nextMeters - 3f) < 0.05f && forward.usedLiters > s.usedLiters && !forward.arrived,
                forward.nextMeters.ToString("F2") + " m to the stop");
            Check("Mission walk: stepping back gives no oxygen back", Mathf.Abs(back.routeMeters - 997f) < 0.05f && Mathf.Abs(back.usedLiters - forward.usedLiters) < 0.0001f);

            progress.Walk(new Vector2(998.7f, 0f));
            var arrived = progress.Read(new Vector2(998.7f, 0f));
            Check("Mission arrival: closer than 1.5 m to the stop counts as arrived", arrived.arrived && progress.Arrived && arrived.nextStop == 0 && Mathf.Approximately(arrived.taskMinutesAtNext, 10f),
                arrived.nextMeters.ToString("F2") + " m left on the route");
            Check("Mission arrival: the work at the stop is not charged before leaving it", arrived.workMinutes < 0.001f);

            // Leaving the stop charges its work; the jump lands 5 m before the habitat.
            jumped = progress.JumpNext();
            landing = progress.PointAt(progress.RouteMeters);
            s = progress.Read(landing);
            float walkedHome = 19.3f + walked995;
            Check("Mission jump: leaving a stop charges its work, and the next point is 5 m before the habitat",
                jumped && progress.Leg == 1 && Mathf.Approximately(s.workMinutes, 10f) && Mathf.Abs(landing.x - 5f) < 0.05f && s.nextName == "Habitat" && s.nextStop == -1 && !s.arrived,
                "leg " + progress.Leg + ", work " + s.workMinutes.ToString("F0") + " min, at x " + landing.x.ToString("F2"));
            Check("Mission jump: time and oxygen add up to the plan's own numbers",
                Mathf.Abs(s.elapsedMinutes - (walkedHome + 10f)) < 0.06f && Mathf.Abs(s.usedLiters - (walkedHome * walkRate + 10f * workRate)) < 0.1f,
                s.elapsedMinutes.ToString("F2") + " min, " + s.usedLiters.ToString("F2") + " L");
            Check("Mission jump: after the last point of the route there is nowhere to jump", !progress.CanJump && !progress.JumpNext() && progress.Jumps == 2);

            // A WHAT IF carries into the visor: with 8 % of a tank the walk back from the stop is not covered.
            var shortPlan = MissionFallback.Make(site, slope, 8f);
            var shortProgress = new MissionProgress(site, shortPlan, home);
            shortProgress.JumpNext();
            var shortRead = shortProgress.Read(shortProgress.PointAt(shortProgress.RouteMeters));
            Check("Mission progress: TURN BACK when the oxygen left above the reserve no longer covers the way back (WHAT IF included)",
                shortRead.turnBack && !s.turnBack && shortRead.oxygenLeftPercent < 8f, "O2 left " + shortRead.oxygenLeftPercent.ToString("F1") + " %");

            // No way back on the map (cut-off ground): the visor says so and warns.
            var cut = new float[home.Length];
            for (int i = 0; i < cut.Length; i++) cut[i] = float.PositiveInfinity;
            var cutRead = new MissionProgress(site, plan, cut).Read(new Vector2(500f, 0f));
            Check("Mission progress: ground with no way back on the map warns TURN BACK", cutRead.noWayBack && cutRead.turnBack);

            var none = new MissionProgress(site, new MissionFallback.Plan { HasRoute = false }, home);
            Check("Mission progress: without a route there is nothing to walk or jump", !none.Valid && !none.CanJump && !none.JumpNext());
        }
    }
}
