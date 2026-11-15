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
            // The menu button read as "held" (Unity's XR input) counts once per press, however long it is held.
            bool before = false;
            int presses = 0;
            foreach (bool held in new[] { false, true, true, true, false, false, true, false })
                if (WalkbackApp.RisingEdge(held, ref before)) presses++;
            Check("Mission button: a held menu button counts once per press (two presses in the test)", presses == 2, presses + " presses");
            Check("Log: the fps line says where the user is (Mars, room, 360 photo, opening, table)",
                WalkbackApp.WhereName(MissionMode.Mode.Mars, false, true) == "Mars" && WalkbackApp.WhereName(MissionMode.Mode.Room, false, true) == "room" &&
                WalkbackApp.WhereName(MissionMode.Mode.Table, true, true) == "360 photo" && WalkbackApp.WhereName(MissionMode.Mode.Table, false, true) == "opening" &&
                WalkbackApp.WhereName(MissionMode.Mode.Table, false, false) == "table");

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
                Check("Mission jump: the stop's pillar stands " + MissionProgress.ApproachMeters.ToString("0.0") + " m straight ahead (a real walk to it, not one step)",
                    pillarMeters > MissionProgress.ApproachMeters - 0.1f && pillarMeters <= MissionProgress.ApproachMeters + 0.05f && Vector3.Angle(toPillar, Vector3.forward) < 1f &&
                    (mission.MetersOf(cam.transform.position) - mission.Progress.PointAt(mission.Progress.RouteMeters)).magnitude < 0.01f,
                    pillarMeters.ToString("F2") + " m ahead, " + Vector3.Angle(toPillar, Vector3.forward).ToString("F1") + "° off");

                // ---- The arrows on the maps: the way the user looks, as a direction on Mars
                Vector2 toStop = mission.Progress.LegTarget(mission.Progress.Leg) - mission.MetersOf(cam.transform.position);
                float bearing = Mathf.Repeat(Mathf.Atan2(toStop.x, toStop.y) * Mathf.Rad2Deg, 360f);
                mission.Tick(cam.transform, 0.01f);
                MiniMap miniMap = app.hud.miniMap;
                bool arrowAhead = Mathf.Abs(Mathf.DeltaAngle(mission.HeadingDegrees, bearing)) < 1f && miniMap.HeadingKnown && miniMap.youHeading != null && miniMap.youHeading.gameObject.activeSelf &&
                    Mathf.Abs(Mathf.DeltaAngle(miniMap.you.localEulerAngles.z, -bearing)) < 1f && app.wristMap.youNose != null && app.wristMap.youNose.gameObject.activeSelf &&
                    Mathf.Abs(Mathf.DeltaAngle(app.wristMap.youPin.localEulerAngles.y, bearing)) < 1f;
                HelmetHud visor = app.hud;
                bool tapeAhead = visor.HeadingKnown && visor.compass.gameObject.activeSelf && visor.headingCard.gameObject.activeSelf && visor.arrowPlate.gameObject.activeSelf &&
                    mission.HasBearing && Mathf.Abs(Mathf.DeltaAngle(mission.BearingToNext, bearing)) < 1f && Mathf.Abs(visor.compass.stopMarker.localPosition.x) < 0.005f &&
                    Mathf.Abs(Mathf.DeltaAngle(visor.arrow.localEulerAngles.z, 0f)) < 1f && visor.headingCard.lines[0].text == HudCompass.BearingText(mission.HeadingDegrees);
                string headingAhead = visor.headingCard.lines[0].text;
                Quaternion lookingAtStop = cam.transform.rotation;
                cam.transform.rotation = Quaternion.AngleAxis(90f, Vector3.up) * lookingAtStop;
                mission.Tick(cam.transform, 0.01f);
                bool arrowTurned = Mathf.Abs(Mathf.DeltaAngle(mission.HeadingDegrees, bearing + 90f)) < 1f && Mathf.Abs(Mathf.DeltaAngle(miniMap.you.localEulerAngles.z, -(bearing + 90f))) < 1f;
                // Looking 90° to the right of the stop: the stop is to the left. The arrow points left and the tape's bar waits at its left end.
                bool tapeTurned = visor.compass.TargetOffTape && visor.compass.stopMarker.localPosition.x < -HudCompass.WidthMeters / 2f + 0.011f &&
                    Mathf.Abs(Mathf.DeltaAngle(visor.arrow.localEulerAngles.z, 90f)) < 1f && visor.headingCard.lines[0].text == HudCompass.BearingText(mission.HeadingDegrees);
                cam.transform.rotation = lookingAtStop;
                mission.Tick(cam.transform, 0.01f);
                Check("Mission HUD: the compass shows where the user looks; its amber bar and the arrow point to the next stop (ahead, then to the left after turning right)",
                    tapeAhead && tapeTurned, "heading " + headingAhead + ", stop at " + HudCompass.BearingText(bearing));
                Check("Mission maps: the \"you\" marks are arrows that point where the user looks (toward the stop, then 90° to the right)",
                    arrowAhead && arrowTurned, "heading " + mission.HeadingDegrees.ToString("F0") + "°, stop at " + bearing.ToString("F0") + "°");

                // ---- TURN BACK NOW sounds once each time it comes on, not while it stays on
                {
                    int alertsBefore = app.hud.TurnBackAlerts;
                    var calm = new MissionProgress.Snapshot { nextName = "test" };
                    var alarm = calm;
                    alarm.turnBack = true;
                    app.hud.ShowLive(calm, app.terrain.Site);
                    app.hud.ShowLive(alarm, app.terrain.Site);
                    bool cardOn = app.hud.warningCard.gameObject.activeSelf;
                    app.hud.ShowLive(alarm, app.terrain.Site);
                    int afterStaying = app.hud.TurnBackAlerts - alertsBefore;
                    app.hud.ShowLive(calm, app.terrain.Site);
                    bool cardOff = !app.hud.warningCard.gameObject.activeSelf;
                    app.hud.ShowLive(alarm, app.terrain.Site);
                    int afterSecond = app.hud.TurnBackAlerts - alertsBefore;
                    app.hud.ShowLive(calm, app.terrain.Site);
                    float[] tone = HelmetHud.AlertTone(HelmetHud.AlertRate);
                    float peak = 0f, gap = 0f;
                    for (int i = 0; i < tone.Length; i++)
                    {
                        peak = Mathf.Max(peak, Mathf.Abs(tone[i]));
                        float t = i / (float)HelmetHud.AlertRate;
                        if (t > 0.23f && t < 0.33f) gap = Mathf.Max(gap, Mathf.Abs(tone[i]));
                    }
                    Check("Mission HUD: TURN BACK NOW sounds once each time it comes on (two beeps under one second, made in code)",
                        cardOn && cardOff && afterStaying == 1 && afterSecond == 2 && tone.Length > 0 && tone.Length <= HelmetHud.AlertRate && peak > 0.3f && peak <= 1f && gap == 0f,
                        afterSecond + " alerts for two turn-ons, " + (tone.Length / (float)HelmetHud.AlertRate).ToString("F2") + " s, peak " + peak.ToString("F2"));
                    // A full refresh puts the real numbers back on the visor.
                    mission.Tick(cam.transform, 0.3f);
                }

                // ---- The estimate line never covers the name on the pillar (it did, from 7 to 5.5 m, when it was above the cards)
                {
                    Vector3 wasAt = cam.transform.position;
                    Quaternion wasLooking = cam.transform.rotation;
                    Vector3 labelAt = mission.pillarLabel.transform.position;
                    Vector3 away = new Vector3(wasAt.x - labelAt.x, 0f, wasAt.z - labelAt.z).normalized;
                    // Eyes 1.75 m below the name, as on level ground (name at 3.35 m, eyes at 1.6 m).
                    float eyeY = labelAt.y - (MissionMode.PillarHeightMeters + 0.35f - 1.6f);
                    int crossings = 0, oldCrossings = 0, poses = 0;
                    string covered = "";
                    foreach (float pitch in new[] { 0f, 10f, 20f })
                        foreach (float meters in new[] { 40f, 20f, 10f, 7.4f, 7f, 6.5f, 6f, 5.6f, 5f, 4f, 3f, 2f })
                        {
                            cam.transform.position = new Vector3(labelAt.x, eyeY, labelAt.z) + away * meters;
                            // Looking toward the stop, level or down at the path (pitch is degrees down).
                            cam.transform.rotation = Quaternion.LookRotation(-away, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
                            mission.pillarLabel.Face(cam.transform.position);
                            app.hud.SnapTo(cam.transform);
                            Rect name = ViewRect(cam, mission.pillarLabel);
                            app.hud.estimateTag.transform.localPosition = HelmetHud.EstimateBelow;
                            if (ViewRect(cam, app.hud.estimateTag).Overlaps(name)) { crossings++; covered += " " + meters.ToString("0.#") + " m/" + pitch.ToString("0") + "°"; }
                            app.hud.estimateTag.transform.localPosition = HelmetHud.EstimateAbove;
                            if (ViewRect(cam, app.hud.estimateTag).Overlaps(name)) oldCrossings++;
                            app.hud.estimateTag.transform.localPosition = HelmetHud.EstimateBelow;
                            poses++;
                        }
                    cam.transform.SetPositionAndRotation(wasAt, wasLooking);
                    mission.pillarLabel.Face(cam.transform.position);
                    app.hud.SnapTo(cam.transform);
                    mission.Tick(cam.transform, 0.3f);
                    Check("Mission HUD: the estimate line sits below the cards and never covers the name on the pillar (2 to 40 m away, looking level or down at the path)",
                        crossings == 0 && oldCrossings > 0 && app.hud.estimateTag.transform.localPosition == HelmetHud.EstimateBelow,
                        crossings + " of " + poses + " poses covered" + covered + "; above the cards it was " + oldCrossings);
                }

                // ---- The visor's new numbers: bearing and height difference to the stop, the margin, the oxygen bar
                {
                    Vector2 hereNow = mission.MetersOf(cam.transform.position), target = mission.Progress.LegTarget(mission.Progress.Leg);
                    float climb = RouteGeometry.HeightAt(site, target.x, target.y) - RouteGeometry.HeightAt(site, hereNow.x, hereNow.y);
                    var last = mission.Last;
                    Check("Mission HUD: the next card gives the stop's distance, minutes and bearing, and the height difference from the relief",
                        visor.nextCard.lines[1].text.EndsWith(HudCompass.BearingText(last.nextBearingDegrees)) && Mathf.Abs(Mathf.DeltaAngle(last.nextBearingDegrees, bearing)) < 1f &&
                        Mathf.Abs(last.nextStraightMeters - toStop.magnitude) < 0.05f && Mathf.Abs(last.nextClimbMeters - climb) < 0.01f &&
                        visor.nextCard.lines[2].text.StartsWith(HudCompass.ClimbText(climb)), visor.nextCard.lines[1].text + " | " + visor.nextCard.lines[2].text);
                    Check("Mission HUD: the minutes back are green with a full tank, with the oxygen margin under them; the oxygen bar matches the number",
                        last.returnState == HudEstimate.ReturnState.Covered && visor.returnCard.lines[0].color == WalkbackTheme.Safe && last.marginMinutes > 60f &&
                        visor.returnCard.lines[2].text == HelmetHud.MarginText(last.returnState, last.marginMinutes, false) &&
                        Mathf.Abs(visor.OxygenBarFraction - last.oxygenLeftPercent / 100f) < 0.001f && Mathf.Abs(visor.oxygenFill.localScale.x - HelmetHud.OxygenBarMeters * visor.OxygenBarFraction) < 0.0005f,
                        visor.returnCard.lines[0].text + " | " + visor.returnCard.lines[2].text);
                    Canvas.ForceUpdateCanvases();
                    string wide = "";
                    foreach (var card in new[] { visor.oxygenCard, visor.returnCard, visor.nextCard, visor.headingCard })
                        foreach (var cardLine in card.lines)
                        {
                            float box = ((RectTransform)cardLine.transform).rect.width;
                            if (cardLine.preferredWidth > box) wide += cardLine.text + " " + cardLine.preferredWidth.ToString("F0") + "/" + box.ToString("F0") + " px; ";
                        }
                    bool middleClear;
                    int boxCount;
                    string overlaps = HudOverlaps(visor, out middleClear, out boxCount);
                    Check("Mission HUD: with the compass, the heading and the arrow on, no two boxes overlap, the middle stays clear and every line fits",
                        overlaps.Length == 0 && middleClear && boxCount >= 10 && wide.Length == 0, boxCount + " boxes " + overlaps + wide);

                    // The compass and the heading sit between and over the top cards, under where the pillar's name
                    // is seen. From 5 m in (a jump lands MissionProgress.ApproachMeters from the stop), looking level or down at the path,
                    // with the stop's ground level with the user's, 0.4 m lower or 0.5 m higher, they stay under the name.
                    Vector3 wasAt = cam.transform.position;
                    Quaternion wasLooking = cam.transform.rotation;
                    Vector3 labelAt = mission.pillarLabel.transform.position;
                    Vector3 away = new Vector3(wasAt.x - labelAt.x, 0f, wasAt.z - labelAt.z).normalized;
                    int covered = 0, poses = 0;
                    string where = "";
                    foreach (float stopGround in new[] { -0.4f, 0f, 0.5f })
                        foreach (float pitch in new[] { 0f, 10f, 20f })
                            foreach (float meters in new[] { 5f, 4f, 3f, MissionProgress.ApproachMeters, 2f, MissionProgress.ArriveMeters })
                            {
                                float eyeY = labelAt.y - (MissionMode.PillarHeightMeters + 0.35f - 1.6f) - stopGround;
                                cam.transform.position = new Vector3(labelAt.x, eyeY, labelAt.z) + away * meters;
                                cam.transform.rotation = Quaternion.LookRotation(-away, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
                                mission.pillarLabel.Face(cam.transform.position);
                                visor.SnapTo(cam.transform);
                                Rect name = ViewRect(cam, mission.pillarLabel);
                                if (ViewRectOf(cam, visor.compass.transform, HudCompass.WidthMeters, HudCompass.HeightMeters).Overlaps(name) || ViewRect(cam, visor.headingCard).Overlaps(name))
                                {
                                    covered++;
                                    where += " " + meters.ToString("0.#") + " m/" + pitch.ToString("0") + "°/" + stopGround.ToString("0.#") + " m";
                                }
                                poses++;
                            }
                    cam.transform.SetPositionAndRotation(wasAt, wasLooking);
                    mission.pillarLabel.Face(cam.transform.position);
                    visor.SnapTo(cam.transform);
                    mission.Tick(cam.transform, 0.3f);
                    Check("Mission HUD: from 5 m to the arrival, where a jump lands (" + MissionProgress.ApproachMeters.ToString("0.#") + " m) included, the compass and the heading never cover the name on the pillar",
                        covered == 0, covered + " of " + poses + " poses covered" + where);
                }

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

                // ---- START MISSION on the board does what the menu button does; the RECENTER sign hides with the table
                {
                    PanelButton startButton = app.panel.missionButton;
                    TableControls controls = app.tableControls;
                    if (controls != null && controls.app == null) controls.app = app;
                    bool ready = startButton != null && startButton.Interactable;
                    if (startButton != null) startButton.OnPointerClick(startButton.transform.position);
                    bool inMars = mission.Current == MissionMode.Mode.Mars;
                    if (controls != null) controls.SyncRecenterSign();
                    bool signHidden = controls == null || controls.recenterButton == null || !controls.recenterButton.gameObject.activeSelf;
                    if (mission.Current != MissionMode.Mode.Table) mission.Switch(MissionMode.Mode.Table, cam.transform);
                    if (controls != null) controls.SyncRecenterSign();
                    bool signBack = controls == null || controls.recenterButton == null || controls.recenterButton.gameObject.activeSelf;
                    Check("Mission: START MISSION on the board takes the table into Mars (it works with hands too), and the RECENTER sign hides with the table",
                        ready && inMars && signHidden && signBack && mission.Current == MissionMode.Mode.Table,
                        "ready " + ready + ", Mars " + inMars + ", sign hidden " + signHidden + ", back " + signBack);
                }

                // ---- Without the menu button or the grip: two signs low in front, pressed with the pointer
                {
                    mission.NextMode(cam.transform);
                    Transform bar = mission.controls;
                    PointerButton stopSign = mission.nextStopButton, viewSign = mission.nextViewButton;
                    bool shown = bar != null && bar.gameObject.activeSelf && stopSign != null && viewSign != null &&
                        stopSign.GetComponent<Collider>() != null && viewSign.GetComponent<Collider>() != null && mission.Current == MissionMode.Mode.Mars;
                    Vector3 toBar = shown ? bar.position - cam.transform.position : Vector3.zero;
                    bool inReach = shown && new Vector2(toBar.x, toBar.z).magnitude < 1f && toBar.y < -0.3f && toBar.y > -0.9f && Vector3.Dot(bar.forward, toBar) > 0f;
                    string onMars = viewSign != null && viewSign.label != null ? viewSign.label.text : "";

                    // Walking 3 m away leaves them behind: they come back in front.
                    Vector3 stood = cam.transform.position;
                    cam.transform.position = stood + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 3f;
                    mission.Tick(cam.transform, 0.3f);
                    Vector3 after = shown ? bar.position - cam.transform.position : Vector3.zero;
                    bool follows = shown && new Vector2(after.x, after.z).magnitude < 1f;
                    cam.transform.position = stood;
                    mission.Tick(cam.transform, 0.3f);

                    int jumpsBefore = mission.Progress != null ? mission.Progress.Jumps : -1;
                    if (stopSign != null) stopSign.OnPointerClick(stopSign.transform.position);
                    bool signJumped = mission.Progress != null && mission.Progress.Jumps == jumpsBefore + 1;
                    if (viewSign != null) viewSign.OnPointerClick(viewSign.transform.position);
                    bool inRoom = mission.Current == MissionMode.Mode.Room && bar != null && bar.gameObject.activeSelf;
                    string inRoomText = viewSign != null && viewSign.label != null ? viewSign.label.text : "";
                    if (viewSign != null) viewSign.OnPointerClick(viewSign.transform.position);
                    bool atTable = mission.Current == MissionMode.Mode.Table && bar != null && !bar.gameObject.activeSelf;
                    Check("Mission: NEXT STOP and the view sign float low in front of the user and do the grip's and the menu button's job (hands or controllers)",
                        shown && inReach && follows && signJumped && inRoom && atTable && onMars == MissionMode.RoomViewText && inRoomText == MissionMode.BackToTableText,
                        "shown " + shown + ", in reach " + inReach + ", follows " + follows + ", jump " + signJumped + ", room " + inRoom + ", table " + atTable);
                }

                // ---- The walk from where a jump lands to the stop: the signs stay in front all the way and at the arrival
                // (they stood 0.7 m ahead and the walk is 1 m: they ended 0.3 m behind the user, where nothing moved them)
                {
                    mission.NextMode(cam.transform);
                    mission.Jump(cam.transform);
                    Vector3 stood = cam.transform.position;
                    Vector3 ahead = mission.pillar.position - stood;
                    ahead.y = 0f;
                    ahead.Normalize();
                    int steps = Mathf.RoundToInt((MissionProgress.ApproachMeters - MissionProgress.ArriveMeters) * 10f) + 1;
                    string lost = "";
                    for (int i = 1; i <= steps && mission.controls != null; i++)
                    {
                        cam.transform.position = stood + ahead * (i * 0.1f);
                        mission.Tick(cam.transform, 0.3f);
                        Vector3 to = mission.controls.position - cam.transform.position;
                        to.y = 0f;
                        if (to.magnitude < MissionMode.ControlsTooCloseMeters - 0.001f || to.magnitude > MissionMode.ControlsFollowMeters + 0.001f || Vector3.Dot(to.normalized, ahead) < 0.5f)
                            lost += (i * 0.1f).ToString("0.0") + " m ";
                        if (mission.CardShown) break;
                    }
                    Vector3 atEnd = mission.controls != null ? mission.controls.position - cam.transform.position : Vector3.zero;
                    atEnd.y = 0f;
                    bool inFront = mission.Last.arrived && mission.CardShown && Mathf.Abs(atEnd.magnitude - MissionMode.ControlsAheadMeters) < 0.01f && Vector3.Dot(atEnd.normalized, ahead) > 0.99f;
                    bool rule = !MissionMode.ControlsLeftBehind(new Vector3(0f, -0.5f, 0.7f), Vector3.forward) && !MissionMode.ControlsLeftBehind(new Vector3(0.7f, 0f, 0f), Vector3.forward) &&
                        MissionMode.ControlsLeftBehind(new Vector3(0f, -0.5f, 0.3f), Vector3.forward) && MissionMode.ControlsLeftBehind(new Vector3(0f, 0f, 1.2f), Vector3.forward) &&
                        MissionMode.ControlsLeftBehind(new Vector3(0f, 0f, -0.7f), Vector3.forward);

                    // The card has just opened, seen from where the user arrived: looking level it is under the whole
                    // visor; looking at it, its text is clear of every box of the visor. (At 1.3 m above the floor it
                    // crossed the NEXT card, the arrow, the mini map and the ESTIMATE line, and each covered the other.)
                    Quaternion level = cam.transform.rotation;
                    Vector3 cardAt = mission.stopCard.transform.position;
                    float squareTo = Vector3.Angle(mission.stopCard.transform.forward, cardAt - cam.transform.position);
                    float below = Mathf.Atan2(cam.transform.position.y - cardAt.y, FlatDistance(cardAt, cam.transform.position)) * Mathf.Rad2Deg;
                    app.hud.SnapTo(cam.transform);
                    Canvas.ForceUpdateCanvases();
                    string wideLines = "";
                    foreach (var cardLine in mission.stopCard.lines)
                        if (cardLine.preferredWidth > ((RectTransform)cardLine.transform).rect.width) wideLines += cardLine.text + "; ";
                    string overLevel = CardOverVisor(cam, app.hud, mission.stopCard, false);
                    cam.transform.rotation = Quaternion.LookRotation(cardAt - cam.transform.position, Vector3.up);
                    app.hud.SnapTo(cam.transform);
                    string overText = CardOverVisor(cam, app.hud, mission.stopCard, true);
                    cam.transform.rotation = level;
                    app.hud.SnapTo(cam.transform);

                    cam.transform.position = stood;
                    mission.Switch(MissionMode.Mode.Table, cam.transform);
                    Check("Mission signs: walking from where a jump lands to the stop they stay in front, and at the arrival they stand in front, under the card",
                        lost.Length == 0 && inFront && rule, lost.Length > 0 ? "not in front after " + lost : "in front at " + atEnd.magnitude.ToString("F2") + " m, rule " + rule);
                    Check("Mission arrival: the stop's card opens under the visor, facing the eyes (looking level it touches no box of the visor; looking at it, none covers its text), every line on one line",
                        mission.stopCard != null && squareTo < 1f && below > 20f && below < 40f && overLevel.Length == 0 && overText.Length == 0 && wideLines.Length == 0,
                        below.ToString("F0") + "° below the horizon" + (overLevel.Length > 0 ? ", level: over " + overLevel : "") + (overText.Length > 0 ? ", looking at it: text under " + overText : "") + wideLines);
                }

                // ---- Back at the table after walking on Mars: the table comes in front of the user
                // (it stayed where the mission began: after the walk to a stop, around the user or behind them)
                {
                    TableControls controls = app.tableControls;
                    if (controls != null && controls.app == null) controls.app = app;
                    var saved = new System.Collections.Generic.Dictionary<Transform, Pose>();
                    if (controls != null)
                        foreach (Transform t in controls.StageObjects()) saved[t] = new Pose(t.position, t.rotation);
                    Vector3 mapScale = app.mapRoot.localScale, tableAt = app.table.position;
                    float yawWas = controls != null ? controls.StageYaw : 0f;
                    Vector3 stood = cam.transform.position;
                    Quaternion looked = cam.transform.rotation;

                    // Not moved: the table stays as the user left it.
                    mission.NextMode(cam.transform);
                    mission.Switch(MissionMode.Mode.Table, cam.transform);
                    bool stays = !mission.BringTableToUser(cam.transform) && (app.table.position - tableAt).magnitude < 0.0001f;

                    // 1.2 m away and looking another way: the table is 1 m in front of where the user looks now.
                    mission.NextMode(cam.transform);
                    cam.transform.SetPositionAndRotation(stood + new Vector3(0.4f, 0f, 1.1f), Quaternion.Euler(0f, 70f, 0f) * looked);
                    mission.Switch(MissionMode.Mode.Table, cam.transform);
                    bool came = mission.BringTableToUser(cam.transform);
                    Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                    Vector3 toTable = app.table.position - cam.transform.position;
                    toTable.y = 0f;
                    bool inFront = came && (toTable - forward * TableControls.TableDistance).magnitude < 0.01f && app.table.gameObject.activeSelf == tableWas && !app.MapControlsLocked;
                    bool rule = !MissionMode.MovedAway(new Vector3(0f, 1.6f, 0f), new Vector3(0.3f, 1.2f, 0.3f), MissionMode.TableComesMeters) &&
                        MissionMode.MovedAway(Vector3.zero, new Vector3(0.4f, 0f, 0.4f), MissionMode.TableComesMeters);

                    // Everything back exactly where the other checks expect it.
                    cam.transform.SetPositionAndRotation(stood, looked);
                    if (controls != null)
                    {
                        Vector3 facing = Quaternion.Euler(0f, yawWas, 0f) * Vector3.forward;
                        controls.Recenter(tableAt - facing * TableControls.TableDistance + Vector3.up * 1.6f, facing);
                        foreach (var pair in saved) pair.Key.SetPositionAndRotation(pair.Value.position, pair.Value.rotation);
                    }
                    app.mapRoot.localScale = mapScale;
                    Check("Mission: back at the table after walking away, the table comes 1 m in front of the user; if they did not move it stays",
                        controls != null && stays && came && inFront && rule && (app.table.position - tableAt).magnitude < 0.0001f,
                        "stays " + stays + ", came " + came + ", in front " + inFront + " (" + toTable.magnitude.ToString("F2") + " m)");
                }

                // ---- RECENTER with the pointer: B and Y do not exist with hands
                {
                    mission.NextMode(cam.transform);
                    PointerButton sign = mission.recenterButton, stopSign = mission.nextStopButton;
                    BoxCollider signBox = sign != null ? sign.GetComponent<BoxCollider>() : null, stopBox = stopSign != null ? stopSign.GetComponent<BoxCollider>() : null;
                    // In the signs' own plane: its top edge is at least 10 mm under the bottom edge of the row above.
                    float under = signBox != null && stopBox != null
                        ? (stopSign.transform.localPosition.y - stopBox.size.y / 2f) - (sign.transform.localPosition.y + signBox.size.y / 2f) : -1f;
                    bool there = sign != null && mission.controls != null && sign.transform.IsChildOf(mission.controls) && sign.gameObject.activeInHierarchy && under >= 0.01f &&
                        sign.label != null && sign.label.text == MissionMode.RecenterText && sign.label.fontSize * sign.label.transform.lossyScale.y >= 0.0239f;

                    // The sign does what B does (the press itself goes to the app's head, which in this test is not this camera).
                    int recentersBefore = mission.Recenters;
                    if (sign != null) sign.OnPointerClick(sign.transform.position);
                    bool pressed = mission.Recenters == recentersBefore + 1;

                    // Looking 90° away from the stop and recentering: the stop is straight ahead again, the signs in front.
                    Quaternion looked = cam.transform.rotation;
                    mission.Recenter(cam.transform);
                    cam.transform.rotation = Quaternion.Euler(0f, 90f, 0f) * looked;
                    mission.Tick(cam.transform, 0.3f);
                    bool turnedAway = mission.HasBearing && Mathf.Abs(Mathf.DeltaAngle(mission.HeadingDegrees, mission.BearingToNext)) > 80f;
                    mission.Recenter(cam.transform);
                    mission.Tick(cam.transform, 0.3f);
                    bool aheadAgain = mission.HasBearing && Mathf.Abs(Mathf.DeltaAngle(mission.HeadingDegrees, mission.BearingToNext)) < 1f;
                    Vector3 toSigns = mission.controls != null ? mission.controls.position - cam.transform.position : Vector3.zero;
                    toSigns.y = 0f;
                    bool signsInFront = toSigns.sqrMagnitude > 0.01f && Vector3.Dot(toSigns.normalized, Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized) > 0.99f;
                    cam.transform.rotation = looked;
                    mission.Switch(MissionMode.Mode.Table, cam.transform);
                    Check("Mission: a RECENTER sign under NEXT STOP does what B or Y does (hands have no B or Y): the stop is ahead again, the signs in front",
                        there && pressed && turnedAway && aheadAgain && signsInFront && !mission.Recenter(cam.transform),
                        "there " + there + ", pressed " + pressed + ", turned away " + turnedAway + ", ahead again " + aheadAgain + ", signs in front " + signsInFront);
                }

                // ---- An error in a change behind the curtain must not leave the curtain closed
                {
                    int failedBefore = mission.FailedChanges;
                    bool threw = false, ran = false;
                    try { ran = mission.BehindCurtain(MissionMode.JumpFadeSeconds, cam.transform, () => { throw new System.InvalidOperationException("test"); }); }
                    catch (System.Exception) { threw = true; }
                    Check("Mission: an error in a change behind the fade curtain is logged and the curtain still opens (no black headset)",
                        ran && !threw && mission.FailedChanges == failedBefore + 1 && !mission.Fading && mission.CurtainAlpha == 0f &&
                        (mission.curtain == null || !mission.curtain.gameObject.activeSelf), "threw " + threw + ", failed changes " + (mission.FailedChanges - failedBefore));
                }
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
                Shot(cam, "mission_01_mars_before_the_stop.png", eye, eye + Vector3.forward);
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
        /// <summary>The box a label takes on screen (viewport units), from the four corners of its canvas.</summary>
        static Rect ViewRect(Camera cam, GlobeLabel label)
        {
            var corners = new Vector3[4];
            label.canvasRect.GetWorldCorners(corners);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                Vector3 v = cam.WorldToViewportPoint(corner);
                x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x);
                y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>
        /// The visor's boxes that the arrival card's plate (or only its block of text) shares the view with,
        /// from this camera: both are at about the same distance, so whichever is nearer covers the other.
        /// </summary>
        static string CardOverVisor(Camera cam, HelmetHud hud, GlobeLabel card, bool textOnly)
        {
            Rect cardBox = ViewRect(cam, card);
            if (textOnly)
            {
                float wide = 0f, tall = 0f;
                foreach (var line in card.lines)
                {
                    wide = Mathf.Max(wide, line.preferredWidth * Mathf.Abs(line.transform.lossyScale.x));
                    tall += ((RectTransform)line.transform).rect.height * Mathf.Abs(line.transform.lossyScale.y);
                }
                cardBox = ViewRectOf(cam, card.transform, wide, tall);
            }
            string over = "";
            System.Action<string, Transform, float, float> test = (name, t, w, h) =>
            {
                if (t != null && t.gameObject.activeInHierarchy && ViewRectOf(cam, t, w, h).Overlaps(cardBox)) over += name + " ";
            };
            foreach (var box in new[] { hud.oxygenCard, hud.returnCard, hud.nextCard, hud.estimateTag, hud.warningCard, hud.headingCard, hud.miniMap.caption })
                test(box.name, box.transform, box.backPlate.localScale.x, box.backPlate.localScale.y);
            test("Compass", hud.compass.transform, HudCompass.WidthMeters, HudCompass.HeightMeters);
            test("NextArrow", hud.arrowPlate, 0.10f, 0.10f);
            test("MiniMap", hud.miniMap.transform, MiniMap.SizeMeters + 0.012f, MiniMap.SizeMeters + 0.012f);
            return over;
        }

        /// <summary>The box a flat rectangle (width x height, centered on a transform, in its plane) takes on screen.</summary>
        static Rect ViewRectOf(Camera cam, Transform center, float width, float height)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector3 corner = center.TransformPoint(new Vector3((i % 2 == 0 ? -1f : 1f) * width / 2f, (i < 2 ? -1f : 1f) * height / 2f, 0f));
                Vector3 v = cam.WorldToViewportPoint(corner);
                x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x);
                y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

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

            Check("Mission progress: the stop is due east (090°) on level ground, and with a full tank the way back is covered (green)",
                Mathf.Abs(s.nextBearingDegrees - 90f) < 0.01f && Mathf.Abs(s.nextStraightMeters - 1000f) < 0.5f && Mathf.Abs(s.nextClimbMeters) < 0.001f &&
                s.returnState == HudEstimate.ReturnState.Covered && Mathf.Abs(s.marginMinutes - (HudEstimate.TankLiters - HudEstimate.ReserveLiters) / walkRate) < 0.05f,
                HudCompass.BearingText(s.nextBearingDegrees) + ", margin " + s.marginMinutes.ToString("F0") + " min");

            // The jump lands ApproachMeters before the stop and counts as walked (997.5 m of the 1000 m leg at 2.5 m).
            float approach = MissionProgress.ApproachMeters, land = 1000f - approach;
            bool jumped = progress.JumpNext();
            Vector2 landing = progress.PointAt(progress.RouteMeters);
            s = progress.Read(landing);
            float walked995 = 19.3f * land / 1000f;
            Check("Mission jump: lands " + approach.ToString("0.0") + " m before the stop, looking at it", jumped && Mathf.Abs(landing.x - land) < 0.05f && Mathf.Abs(landing.y) < 0.05f &&
                Vector2.Dot(progress.DirectionToTarget(), Vector2.right) > 0.999f, landing.x.ToString("F2") + ", " + landing.y.ToString("F2"));
            Check("Mission jump: the stretch jumped over counts as walked (its minutes and oxygen, from the plan)",
                Mathf.Abs(s.walkMinutes - walked995) < 0.03f && Mathf.Abs(s.usedLiters - walked995 * walkRate) < 0.05f && Mathf.Abs(s.nextMeters - approach) < 0.05f,
                s.walkMinutes.ToString("F2") + " min, " + s.usedLiters.ToString("F2") + " L");
            Check("Mission progress: the way back is read from the map's return layer at the node you stand on",
                !s.noWayBack && Mathf.Abs(s.returnMinutes - home[ReturnFallback.NodeOf(site, landing)]) < 0.001f && Mathf.Abs(s.returnMinutes - walked995) < 0.1f,
                s.returnMinutes.ToString("F2") + " min");

            // Walking for real: 0.5 m forward moves the numbers by 0.5 m (still short of the arrival); a step back gives nothing back.
            float stepTo = land + 0.5f;
            progress.Walk(new Vector2(stepTo, 0.3f));
            var forward = progress.Read(new Vector2(stepTo, 0.3f));
            progress.Walk(new Vector2(stepTo - 1f, 0f));
            var back = progress.Read(new Vector2(stepTo - 1f, 0f));
            Check("Mission walk: the headset's position moves the numbers (0.5 m walked = 0.5 m less to the stop)",
                Mathf.Abs(forward.routeMeters - stepTo) < 0.05f && Mathf.Abs(forward.nextMeters - (approach - 0.5f)) < 0.05f && forward.usedLiters > s.usedLiters && !forward.arrived,
                forward.nextMeters.ToString("F2") + " m to the stop");
            Check("Mission walk: stepping back gives no oxygen back", Mathf.Abs(back.routeMeters - stepTo) < 0.05f && Mathf.Abs(back.usedLiters - forward.usedLiters) < 0.0001f);

            progress.Walk(new Vector2(998.7f, 0f));
            var arrived = progress.Read(new Vector2(998.7f, 0f));
            Check("Mission arrival: closer than 1.5 m to the stop counts as arrived", arrived.arrived && progress.Arrived && arrived.nextStop == 0 && Mathf.Approximately(arrived.taskMinutesAtNext, 10f),
                arrived.nextMeters.ToString("F2") + " m left on the route");
            Check("Mission arrival: the work at the stop is not charged before leaving it", arrived.workMinutes < 0.001f);

            // Leaving the stop charges its work; the jump lands ApproachMeters before the habitat.
            jumped = progress.JumpNext();
            landing = progress.PointAt(progress.RouteMeters);
            s = progress.Read(landing);
            float walkedHome = 19.3f + walked995;
            Check("Mission jump: leaving a stop charges its work, and the next point is " + approach.ToString("0.0") + " m before the habitat",
                jumped && progress.Leg == 1 && Mathf.Approximately(s.workMinutes, 10f) && Mathf.Abs(landing.x - approach) < 0.05f && s.nextName == "Habitat" && s.nextStop == -1 && !s.arrived,
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
            Check("Mission progress: when TURN BACK is on the minutes back are red and the margin says by how much the oxygen falls short",
                shortRead.returnState == HudEstimate.ReturnState.NotCovered && shortRead.marginMinutes < 0f && HelmetHud.MarginText(shortRead.returnState, shortRead.marginMinutes, false).StartsWith("short by"),
                HelmetHud.MarginText(shortRead.returnState, shortRead.marginMinutes, false));

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
