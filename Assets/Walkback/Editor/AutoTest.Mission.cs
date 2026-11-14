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
            CheckMissionMath();
            CheckMissionMode(app);
        }

        /// <summary>The anchor pose, the height on the mesh's own triangles and the curtain's curve: no scene needed.</summary>
        static void CheckMissionMath()
        {
            // A point 100 m east and 200 m north of the south-west corner, 7 m up in the mesh, must land on the
            // feet; and the route's direction (north-east) must turn to where the user looks (west).
            Vector3 feet = new Vector3(2f, 0f, -3f);
            Vector3 position;
            Quaternion rotation;
            Vector2 point = new Vector2(100f, 200f), direction = new Vector2(1f, 1f).normalized;
            MissionMode.AnchorPose(1000f, point, direction, feet, Vector3.left, 7f, out position, out rotation);
            Vector3 local = new Vector3(point.x - 500f, 7f, point.y - 500f);
            Vector3 ahead = local + new Vector3(direction.x, 0f, direction.y) * 10f;
            Vector3 landed = position + rotation * local, aheadWorld = position + rotation * ahead;
            Check("Mission anchor: the route point lands under the feet, the stop straight ahead of where you look",
                (landed - feet).magnitude < 0.001f && (aheadWorld - (feet + Vector3.left * 10f)).magnitude < 0.001f,
                "ahead at " + (aheadWorld - feet).ToString("F2"));

            // The height used for the feet is the terrain mesh's own: at the nodes the node's height, on the
            // cell's diagonal (north-west to south-east) the mean of those two corners.
            SiteData site = SiteLoader.Synthetic();
            int n = site.GridSize;
            float spacing = site.NodeSpacing;
            int row = 100, column = 60;
            float nw = site.Heights[row * n + column], se = site.Heights[(row + 1) * n + column + 1];
            float x = column * spacing, y = site.SideMeters - row * spacing;
            float atNode = MissionMode.TriangleHeight(site, x, y) + site.MinElevation;
            float onDiagonal = MissionMode.TriangleHeight(site, x + spacing / 2f, y - spacing / 2f) + site.MinElevation;
            Check("Mission ground: the height under the feet is the terrain mesh's own (nodes and the cell's diagonal)",
                Mathf.Abs(atNode - nw) < 0.001f && Mathf.Abs(onDiagonal - (nw + se) / 2f) < 0.001f,
                atNode.ToString("F3") + " at the node, " + onDiagonal.ToString("F3") + " on the diagonal");

            Check("Mission fade: the curtain closes, the world changes behind it, the curtain opens",
                MissionMode.CurtainAlphaAt(0f) < 0.001f && Mathf.Abs(MissionMode.CurtainAlphaAt(0.25f) - 0.5f) < 0.001f && Mathf.Abs(MissionMode.CurtainAlphaAt(0.5f) - 1f) < 0.001f &&
                Mathf.Abs(MissionMode.CurtainAlphaAt(0.75f) - 0.5f) < 0.001f && MissionMode.CurtainAlphaAt(1f) < 0.001f && MissionMode.FadeSeconds >= 0.8f && MissionMode.FadeSeconds <= 1.5f);

            string first, second;
            MissionMode.SplitInTwo("Curiosity's 44th drill hole, in the Monte Grande hollow: is the hollow rock different from the ridges?", out first, out second);
            Check("Mission card: a long science line is split in two at a space", first.Length > 30 && second.Length > 30 && !first.EndsWith(" ") && !second.StartsWith(" ") &&
                (first + " " + second).StartsWith("Curiosity's 44th drill hole"), first + " | " + second);
        }

        /// <summary>The viewer for the mission checks: standing at the origin, eyes at 1.6 m, looking along +z.</summary>
        static void MissionViewer(Camera cam)
        {
            cam.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, 0f), Quaternion.LookRotation(Vector3.forward, Vector3.up));
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        static void CheckMissionMode(WalkbackApp app)
        {
            MissionMode mission = app.mission;
            Check("Mission: the mode is in the scene, hidden at start", mission != null && mission.Current == MissionMode.Mode.Table && mission.mars != null && !mission.mars.gameObject.activeSelf &&
                mission.stopCard != null && !mission.stopCard.gameObject.activeSelf && (mission.curtain == null || !mission.curtain.gameObject.activeSelf) && !app.MissionActive);
            if (mission == null) return;
            Check("Mission: the fade curtain exists (without it the changes are immediate)", mission.curtain != null && mission.curtain.sharedMaterial != null && mission.curtain.sharedMaterial.shader.name == "Walkback/Fade");

            var cam = app.desktopCamera;
            Vector3 homePosition = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;
            Color background = cam.backgroundColor;
            float farPlane = cam.farClipPlane;
            MapLayer layerBefore = app.terrain.Layer;
            bool tableWas = app.table.gameObject.activeSelf, mapWas = app.mapRoot.gameObject.activeSelf, panelWas = app.panel.gameObject.activeSelf;
            try
            {
                MissionViewer(cam);
                Check("Mission: without a planned route the button does nothing", app.Plan != null || !mission.NextMode(cam.transform));
                app.PlanMission(false);
                if (app.Plan == null || !app.Plan.HasRoute)
                {
                    Note("Mission: no planned route on this site, the mode itself was not checked");
                    return;
                }
                var plan = app.Plan;
                SiteData site = app.terrain.Site;

                // ---- Table -> Mars
                bool entered = mission.NextMode(cam.transform);
                Check("Mission: the button goes from the table to Mars; table, map and panel hide, the map controls lock",
                    entered && mission.Current == MissionMode.Mode.Mars && app.MissionActive && mission.mars.gameObject.activeSelf && mission.terrainRenderer.enabled &&
                    !app.table.gameObject.activeSelf && !app.mapRoot.gameObject.activeSelf && !app.panel.gameObject.activeSelf && app.MapControlsLocked);
                Bounds marsBounds = mission.terrainRenderer.bounds;
                Check("Mission: Mars is the terrain's own mesh at real scale, 1:1 (no copy, not scaled)",
                    mission.terrainCopy.sharedMesh == app.terrain.GetComponent<MeshFilter>().sharedMesh && (mission.mars.lossyScale - Vector3.one).magnitude < 0.0001f &&
                    Mathf.Max(marsBounds.size.x, marsBounds.size.z) > site.SideMeters * 0.99f, Mathf.Max(marsBounds.size.x, marsBounds.size.z).ToString("F0") + " m across");
                bool opaque = cam.backgroundColor.a > 0.99f && cam.farClipPlane >= 2000f;
                foreach (var eye in app.cameraRig.GetComponentsInChildren<Camera>(true))
                    if (eye.backgroundColor.a < 0.99f || eye.farClipPlane < 2000f) opaque = false;
                Check("Mission: on Mars the cameras draw an opaque sky and see the whole kilometer", opaque, "far plane " + cam.farClipPlane.ToString("F0") + " m");
                Vector2 start = mission.Progress.PointAt(0f);
                Vector3 startWorld = mission.mars.TransformPoint(mission.LocalOf(start, 0f));
                Check("Mission: on entering, the start of the route is under the feet and Mars' ground is on the floor",
                    (mission.MetersOf(cam.transform.position) - start).magnitude < 0.01f && Mathf.Abs(startWorld.y - mission.FloorY) < 0.01f && FlatDistance(startWorld, cam.transform.position) < 0.01f,
                    "ground " + startWorld.y.ToString("F3") + " m");
                Check("Mission: the helmet HUD is on, labelled as an estimate, with a full tank at the start",
                    app.hud.Visible && app.hud.estimateTag.lines[0].text.StartsWith("ESTIMATE") && Mathf.Abs(mission.Last.oxygenLeftPercent - plan.TankPercent) < 0.01f && mission.Last.nextStop == plan.Legs[0].ToStop,
                    app.hud.oxygenCard.lines[0].text + " | " + app.hud.nextCard.lines[0].text);
                Check("Mission: the route lies on the ground at real scale, with a pillar at the next stop",
                    mission.routeRenderer.gameObject.activeSelf && mission.routeFilter.sharedMesh != null && mission.routeFilter.sharedMesh.vertexCount > 100 && mission.pillar.gameObject.activeSelf &&
                    mission.pillarLabel.lines[0].text == plan.Legs[0].ToName, mission.routeFilter.sharedMesh != null ? mission.routeFilter.sharedMesh.vertexCount + " vertices" : "no mesh");

                // ---- The jump counts as walked
                float legLength = mission.Progress.LegEnd(0) - mission.Progress.LegStart(0);
                float expectedMinutes = legLength > 0.01f ? plan.Legs[0].Minutes * (mission.Progress.ApproachOf(0) - mission.Progress.LegStart(0)) / legLength : 0f;
                bool jumped = mission.Jump(cam.transform);
                Check("Mission jump: the stretch jumped over is charged from the plan (minutes and oxygen)",
                    jumped && Mathf.Abs(mission.Last.walkMinutes - expectedMinutes) < 0.02f && Mathf.Abs(mission.Last.usedLiters - expectedMinutes * HudEstimate.WalkLitersPerMinute) < 0.05f,
                    mission.Last.walkMinutes.ToString("F2") + " min, " + mission.Last.usedLiters.ToString("F2") + " L");
                Vector3 toPillar = mission.pillar.position - cam.transform.position;
                toPillar.y = 0f;
                float pillarMeters = toPillar.magnitude;
                Check("Mission jump: the stop's pillar stands 5 m straight ahead (a real walk to it, not one step)",
                    pillarMeters > MissionProgress.ApproachMeters - 0.1f && pillarMeters <= MissionProgress.ApproachMeters + 0.05f && Vector3.Angle(toPillar, Vector3.forward) < 1f &&
                    (mission.MetersOf(cam.transform.position) - mission.Progress.PointAt(mission.Progress.RouteMeters)).magnitude < 0.01f,
                    pillarMeters.ToString("F2") + " m ahead, " + Vector3.Angle(toPillar, Vector3.forward).ToString("F1") + "° off");

                // ---- Walking: the headset's position moves the numbers; nothing else moves
                Vector3 marsBefore = mission.mars.position;
                var before = mission.Last;
                float stride = Mathf.Min(1.5f, Mathf.Max(0.3f, pillarMeters - MissionProgress.ArriveMeters - 0.3f));
                cam.transform.position += toPillar.normalized * stride;
                mission.Tick(cam.transform, 0.3f);
                Vector3 marsAfter = mission.mars.position;
                Check("Mission walk: walking toward the stop moves the visor's numbers (closer, more oxygen used)",
                    mission.Last.routeMeters > before.routeMeters + stride * 0.3f && mission.Last.nextMeters < before.nextMeters && mission.Last.usedLiters > before.usedLiters && !mission.Last.arrived,
                    stride.ToString("F2") + " m walked, route +" + (mission.Last.routeMeters - before.routeMeters).ToString("F2") + " m");
                Vector2 hereMeters = mission.MetersOf(cam.transform.position);
                float groundUnderFeet = mission.mars.TransformPoint(mission.LocalOf(hereMeters, 0f)).y;
                Check("Mission walk: Mars does not slide sideways; only its height follows, to keep the ground under the feet",
                    Mathf.Abs(marsAfter.x - marsBefore.x) < 0.0001f && Mathf.Abs(marsAfter.z - marsBefore.z) < 0.0001f && Mathf.Abs(groundUnderFeet - mission.FloorY) < 0.01f,
                    "ground " + groundUnderFeet.ToString("F3") + " m, Mars moved " + (marsAfter.y - marsBefore.y).ToString("F3") + " m up");

                // ---- Arrival opens the stop's card
                cam.transform.position = new Vector3(mission.pillar.position.x, 1.6f, mission.pillar.position.z) - toPillar.normalized * 1.0f;
                mission.Tick(cam.transform, 0.3f);
                int arrivedStop = plan.Legs[0].ToStop;
                string arrivedName = arrivedStop >= 0 ? site.Stops[arrivedStop].name : "HABITAT";
                Check("Mission arrival: within 1.5 m of the pillar the stop's card opens, with its name",
                    mission.Last.arrived && mission.CardShown && mission.stopCard.gameObject.activeSelf && mission.stopCard.lines[0].text == arrivedName &&
                    FlatDistance(mission.stopCard.transform.position, cam.transform.position) < 1.6f, mission.stopCard.lines[0].text + " | " + mission.stopCard.lines[2].text);
                float smallest = float.MaxValue;
                bool uiOnly = true;
                foreach (var label in new[] { mission.stopCard, mission.pillarLabel })
                    foreach (var line in label.lines)
                    {
                        smallest = Mathf.Min(smallest, line.fontSize * line.transform.lossyScale.y);
                        if (line.material == null || line.material.shader.name != "Walkback/UI") uiOnly = false;
                    }
                Check("Mission: card and pillar text at least 24 mm tall and never writing alpha (passthrough)", smallest >= 0.0239f && uiOnly, (smallest * 1000f).ToString("F0") + " mm smallest");

                // ---- Mars -> Room: Mars goes dark, the HUD, the route and the pillar stay on the real floor
                mission.NextMode(cam.transform);
                bool backgroundsBack = Mathf.Abs(cam.backgroundColor.a - background.a) < 0.001f && Mathf.Abs(cam.farClipPlane - farPlane) < 0.01f;
                foreach (var eye in app.cameraRig.GetComponentsInChildren<Camera>(true))
                    if (eye.backgroundColor.a > 0.001f) backgroundsBack = false;
                Bounds routeBounds = mission.routeRenderer.bounds;
                Check("Mission: the button goes on to the room: Mars off, the cameras see the room again",
                    mission.Current == MissionMode.Mode.Room && !mission.terrainRenderer.enabled && backgroundsBack && !mission.border.gameObject.activeSelf && !app.table.gameObject.activeSelf);
                Check("Mission: in the room the HUD, the route and the pillar stay, flat on the real floor",
                    app.hud.Visible && mission.routeRenderer.gameObject.activeSelf && routeBounds.size.y < 0.2f && Mathf.Abs(routeBounds.center.y - mission.FloorY) < 0.2f &&
                    mission.pillar.gameObject.activeSelf && Mathf.Abs(mission.pillar.position.y - (mission.FloorY + MissionMode.PillarHeightMeters / 2f)) < 0.01f && mission.Last.arrived,
                    "route " + routeBounds.size.y.ToString("F2") + " m thick at " + routeBounds.center.y.ToString("F2") + " m");

                // ---- Room -> Table: everything as it was
                mission.NextMode(cam.transform);
                Check("Mission: the button goes back to the table and everything is as it was",
                    mission.Current == MissionMode.Mode.Table && !app.MissionActive && !mission.mars.gameObject.activeSelf && !mission.stopCard.gameObject.activeSelf && !app.hud.Visible &&
                    app.table.gameObject.activeSelf == tableWas && app.mapRoot.gameObject.activeSelf == mapWas && app.panel.gameObject.activeSelf == panelWas && !app.MapControlsLocked &&
                    Mathf.Abs(cam.farClipPlane - farPlane) < 0.01f && mission.Progress == null);

                // ---- No free space: the same mode standing in the room, without Mars
                app.missionWithoutMars = true;
                mission.NextMode(cam.transform);
                bool straightToRoom = mission.Current == MissionMode.Mode.Room && !mission.terrainRenderer.enabled && app.hud.Visible;
                mission.NextMode(cam.transform);
                app.missionWithoutMars = false;
                Check("Mission: with no free space the button skips Mars (table -> room -> table)", straightToRoom && mission.Current == MissionMode.Mode.Table);
            }
            finally
            {
                // Back to the state the other pieces expect, whatever happened above.
                if (mission.Current != MissionMode.Mode.Table) mission.Switch(MissionMode.Mode.Table, cam.transform);
                app.missionWithoutMars = false;
                app.ClearPlannedRoute();
                app.panel.ShowNoRoute();
                app.terrain.ShowLayer(layerBefore);
                app.RefreshPanel();
                cam.transform.SetPositionAndRotation(homePosition, homeRotation);
            }
        }

        static partial void ShotsMission(WalkbackApp app)
        {
            MissionMode mission = app.mission;
            if (mission == null) return;
            var cam = app.desktopCamera;
            Vector3 homePosition = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;
            MapLayer layerBefore = app.terrain.Layer;
            try
            {
                app.PlanMission(false);
                if (app.Plan == null || !app.Plan.HasRoute)
                {
                    Note("Mission: no screenshots (no planned route on this site)");
                    return;
                }
                MissionViewer(cam);
                mission.NextMode(cam.transform);
                mission.Jump(cam.transform);
                mission.Tick(cam.transform, 0.3f);
                app.hud.SnapTo(cam.transform);
                Canvas.ForceUpdateCanvases();
                Vector3 eye = cam.transform.position;
                Shot(cam, "mission_01_mars_5m_before_the_stop.png", eye, eye + Vector3.forward);
                Shot(cam, "mission_02_mars_route_on_the_ground.png", eye, eye + new Vector3(0f, -0.6f, 1f));

                Vector3 toPillar = mission.pillar.position - eye;
                toPillar.y = 0f;
                Vector3 arrival = new Vector3(mission.pillar.position.x, 1.6f, mission.pillar.position.z) - toPillar.normalized * 1.2f;
                cam.transform.position = arrival;
                mission.Tick(cam.transform, 0.3f);
                app.hud.SnapTo(cam.transform);
                Canvas.ForceUpdateCanvases();
                Shot(cam, "mission_03_mars_arrival_card.png", arrival, arrival + toPillar.normalized);

                mission.NextMode(cam.transform);
                app.hud.SnapTo(cam.transform);
                Canvas.ForceUpdateCanvases();
                Shot(cam, "mission_04_room_hud_route_pillar.png", arrival - toPillar.normalized * 2f, arrival + toPillar.normalized);
            }
            finally
            {
                if (mission.Current != MissionMode.Mode.Table) mission.Switch(MissionMode.Mode.Table, cam.transform);
                app.ClearPlannedRoute();
                app.panel.ShowNoRoute();
                app.terrain.ShowLayer(layerBefore);
                app.RefreshPanel();
                cam.transform.SetPositionAndRotation(homePosition, homeRotation);
            }
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
