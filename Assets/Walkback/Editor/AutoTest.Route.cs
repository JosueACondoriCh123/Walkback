// Written with Claude Code (AI), directed and tested by team MARSXR
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Walkback.EditorTools
{
    public static partial class AutoTest
    {
        static partial void CheckRoute(WalkbackApp app)
        {
            var view = app.routeView;
            var pillars = app.stopPillars;
            Check("Route: view and pillars wired under the map", view != null && pillars != null && view.transform.parent == app.mapRoot && pillars.transform.parent == app.mapRoot);
            if (view == null || pillars == null) return;
            var site = app.terrain.Site;

            var material = view.lightMaterial;
            Check("Route: light shader loaded", material != null && material.shader != null && material.shader.name == "Walkback/RouteLight", material != null ? material.shader.name : "no material");
            Check("Route: light is opaque and writes alpha (no holes in passthrough)", material != null && material.renderQueue < 2500, material != null ? "queue " + material.renderQueue : "");

            // Start state: pillars and the straight line, no amber route (rule 1).
            app.ShowRouteStartState(false);
            Canvas.ForceUpdateCanvases();
            Note(site.StopsAreReal ? "STOPS: stops.json, " + site.Stops.Length + " stops" : "STOPS: " + site.Stops.Length + " TEST STOPs (no stops.json yet)");
            Check("Pillars: the habitat plus one per stop", pillars.Habitat != null && pillars.Stops.Count == site.Stops.Length, "habitat + " + pillars.Stops.Count);

            bool names = pillars.Habitat != null && pillars.Habitat.label.text == "HABITAT";
            for (int i = 0; i < pillars.Stops.Count; i++)
            {
                string expected = string.IsNullOrEmpty(site.Stops[i].name) ? "Stop " + (i + 1) : site.Stops[i].name;
                if (pillars.Stops[i].label.text != expected) names = false;
                if (!site.StopsAreReal && !pillars.Stops[i].label.text.Contains("TEST STOP")) names = false;
            }
            Check("Pillars: labels carry the stop names (TEST STOP while stand-ins)", names);

            float worstOffset = 0f;
            worstOffset = Mathf.Max(worstOffset, Vector3.Distance(pillars.Habitat.transform.position, app.mapRoot.TransformPoint(RouteGeometry.ToMap(site, site.Habitat, 0f))));
            for (int i = 0; i < pillars.Stops.Count; i++)
                worstOffset = Mathf.Max(worstOffset, Vector3.Distance(pillars.Stops[i].transform.position, app.mapRoot.TransformPoint(RouteGeometry.ToMap(site, new Vector2(site.Stops[i].x, site.Stops[i].y), 0f))));
            Check("Pillars: each stands on the ground where site.json and stops.json put it", worstOffset < 0.0005f, (worstOffset * 1000f).ToString("F2") + " mm worst");

            var beamBounds = pillars.Habitat.beam.bounds;
            Check("Pillars: about 11 cm tall on the table", beamBounds.size.y > 0.08f && beamBounds.size.y < 0.15f, beamBounds.size.y.ToString("F3") + " m");
            float labelText = StopPillars.LabelFontSize * pillars.Habitat.labelRoot.lossyScale.y;
            Check("Pillars: label text at least 0.025 m tall in the room", labelText >= 0.025f, labelText.ToString("F3") + " m");
            Check("Pillars: labels use the UI material (no holes in passthrough)", pillars.Habitat.label.material != null && pillars.Habitat.label.material.shader.name == "Walkback/UI");
            Check("Pillars: habitat and stops have different colors", pillars.Stops.Count == 0 || pillars.Habitat.baseColor != pillars.Stops[0].baseColor);
            Check("Pillars: the habitat is not a stop button", pillars.Habitat.hit == null);

            bool pressable = true;
            foreach (var p in pillars.Stops)
                if (p.hit == null || !ReferenceEquals(p.hit.GetComponentInParent<IPointerTarget>(), p)) pressable = false;
            Check("Pillars: every stop pillar is pressable (collider + pointer target)", pressable);

            if (pillars.Stops.Count > 0)
            {
                int last = pillars.Stops.Count - 1;
                var target = pillars.Stops[last].hit.GetComponentInParent<IPointerTarget>();
                float before = pillars.Stops[last].beam.transform.localScale.x;
                target.OnPointerEnter();
                float hovered = pillars.Stops[last].beam.transform.localScale.x;
                target.OnPointerExit();
                Check("Pillars: hovering a pillar makes it thicker", hovered > before * 1.1f && Mathf.Approximately(pillars.Stops[last].beam.transform.localScale.x, before));
                target.OnPointerClick(pillars.Stops[last].transform.position);
                bool onlyLast = true;
                for (int i = 0; i < pillars.Stops.Count; i++) if (pillars.Stops[i].Selected != (i == last)) onlyLast = false;
                Check("Pillars: pressing a stop pillar selects that stop", app.SelectedStop == last && onlyLast, "SelectedStop " + app.SelectedStop);
                app.SelectStop(-1);
                Check("Pillars: clearing the selection clears the highlight", !pillars.Stops[last].Selected);
            }

            // The straight line.
            Vector2[] waypoints = app.StopWaypoints();
            Check("Straight line: drawn habitat -> stops -> habitat", view.HasStraightLine && Mathf.Abs(view.StraightLengthMeters - RouteGeometry.Length(waypoints)) < 0.01f, view.StraightLengthMeters.ToString("F0") + " m");
            var lineBlock = new MaterialPropertyBlock();
            view.LineRenderer.GetPropertyBlock(lineBlock);
            Color lineColor = lineBlock.GetColor("_Color");
            Check("Straight line: white dashes", lineBlock.GetFloat("_DashCount") >= 2f && lineColor.r > 0.9f && lineColor.g > 0.9f && lineColor.b > 0.9f, lineBlock.GetFloat("_DashCount").ToString("F0") + " dashes");
            Mesh lineMesh = view.LineRenderer.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] lineVerts = lineMesh.vertices;
            Vector3 habitatMap = RouteGeometry.ToMap(site, site.Habitat, 0f);
            Vector3 lineStart = (lineVerts[0] + lineVerts[1]) / 2f, lineEnd = (lineVerts[lineVerts.Length - 2] + lineVerts[lineVerts.Length - 1]) / 2f;
            Check("Straight line: starts and ends at the habitat", Flat(lineStart - habitatMap) < 1f && Flat(lineEnd - habitatMap) < 1f);
            Check("Straight line: never under the terrain", MinClearance(site, lineVerts) > RouteView.LineLiftMeters - 0.3f, MinClearance(site, lineVerts).ToString("F2") + " m min clearance");
            Check("Before session C's route: no amber route on the map", !view.HasRoute);

            // The ribbon, with a hand-made path (test only).
            Vector2[] path = TestPath(site);
            app.ShowPlannedRoute(path, true);
            Check("Route ribbon: shown whole at edit time", view.HasRoute && Mathf.Approximately(view.Progress, 1f));
            Mesh ribbonMesh = view.RibbonRenderer.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] ribbonVerts = ribbonMesh.vertices;
            float minClear = MinClearance(site, ribbonVerts), maxClear = MaxClearance(site, ribbonVerts);
            Check("Route ribbon: draped on the relief (1.5 to 4 m above the ground)", minClear > RouteView.RibbonLiftMeters - 0.3f && maxClear < 4f, minClear.ToString("F2") + " to " + maxClear.ToString("F2") + " m");
            float worstWidth = 0f;
            for (int i = 0; i + 1 < ribbonVerts.Length; i += 2) worstWidth = Mathf.Max(worstWidth, Mathf.Abs(Flat(ribbonVerts[i] - ribbonVerts[i + 1]) - RouteView.RibbonWidthMeters));
            Check("Route ribbon: 12 m wide (12 mm on the table)", worstWidth < 0.5f, "worst width error " + worstWidth.ToString("F2") + " m");
            Check("Route ribbon: length matches the path", Mathf.Abs(view.RouteLengthMeters - RouteGeometry.Length(path)) < 0.01f, view.RouteLengthMeters.ToString("F0") + " m");
            view.SetProgress(0.5f);
            var ribbonBlock = new MaterialPropertyBlock();
            view.RibbonRenderer.GetPropertyBlock(ribbonBlock);
            Check("Route ribbon: can be drawn up to a fraction (draws itself)", Mathf.Approximately(ribbonBlock.GetFloat("_Progress"), 0.5f));
            view.SetProgress(1f);
            app.ClearPlannedRoute();
            Check("Route ribbon: clears", !view.HasRoute);

            CheckRouteSmoothing();
            CheckLabelLifting(app, pillars);

            // Mission plan B on the site itself: the amber route and the verdict come from the relief.
            MapLayer layerBeforePlan = app.terrain.Layer;
            app.PlanMission(false);
            Check("Mission plan B: the app plans the site's stops", app.Plan != null, app.Plan != null ? MissionFallback.LogLine(app.Plan) : "no plan");
            if (app.Plan != null && app.terrain.Site.IsMarsData)
            {
                Check("Mission plan B: every leg has a way on the real site, and the amber route is on the map", app.Plan.HasRoute && view.HasRoute, MissionFallback.LogLine(app.Plan));
                Check("Mission plan B: the route over the relief is never shorter than the straight line", app.Plan.WalkMeters >= view.StraightLengthMeters - 1f,
                    app.Plan.WalkMeters.ToString("F0") + " m, straight " + view.StraightLengthMeters.ToString("F0") + " m");
                {
                    // The smoothed route against the grid way it came from, on the real relief.
                    MissionFallback.Plan real = app.Plan;
                    SiteData realSite = app.terrain.Site;
                    float longestStep = 0f;
                    int onNoGo = 0;
                    for (int i = 0; i < real.Path.Count; i++)
                    {
                        if (i > 0) longestStep = Mathf.Max(longestStep, Vector2.Distance(real.Path[i - 1], real.Path[i]));
                        // A point is fine on walkable ground (no no-go corner around it) or when it is a grid point of the way itself.
                        if (!MissionFallback.Walkable(realSite, app.terrain.Slope, real.Path[i]) && app.terrain.Slope[ReturnFallback.NodeOf(realSite, real.Path[i])] > TerrainLayers.SlopeLimitDegrees) onNoGo++;
                    }
                    bool legsLongEnough = true;
                    foreach (var leg in real.Legs)
                        if (leg.Meters < Vector2.Distance(real.Path[leg.FirstPoint], real.Path[leg.LastPoint]) - 0.01f) legsLongEnough = false;
                    Check("Mission plan B: the smoothed route is no longer and no slower than the grid way, and no leg is shorter than its straight line",
                        real.WalkMeters <= real.GridMeters + 0.5f && real.WalkMinutes <= real.GridMinutes + 0.01f && legsLongEnough,
                        real.GridMeters.ToString("F0") + " m in " + real.GridMinutes.ToString("F1") + " min on the grid -> " + real.WalkMeters.ToString("F0") + " m in " + real.WalkMinutes.ToString("F1") + " min, " + real.Shortcuts + " shortcuts");
                    Check("Mission plan B: the smoothed route never steps on no-go ground and keeps a point every grid step",
                        onNoGo == 0 && longestStep <= realSite.NodeSpacing * Mathf.Sqrt(2f) + 0.01f, onNoGo + " points on no-go nodes, longest step " + longestStep.ToString("F2") + " m");
                    if (real.Legs.Count > 0)
                        Note("Mission plan B: turning in the last 12 m before " + real.Legs[0].ToName + ": " + TurningDegrees(real, real.Legs[0], 12f).ToString("F0") + "° (the grid way made a Z there)");
                }
                Check("Mission plan B: the verdict card shows the plan, labelled as an estimate", app.panel.Verdict != VerdictState.NoRoute && app.panel.verdictNote.text.StartsWith("ESTIMATE"),
                    app.panel.verdictTitle.text + " | " + app.panel.verdictDetail.text + " | " + app.panel.verdictNote.text);
                app.SetWhatIfTank(8f);
                Check("What if: with 8 % of a tank the verdict is NOT VIABLE and the map shows where you no longer walk back from",
                    app.panel.Verdict == VerdictState.NotViable && app.terrain.Layer == MapLayer.Return && !float.IsInfinity(app.terrain.ReturnReachMinutes) && app.panel.verdictNote.text.StartsWith("WHAT IF"),
                    app.panel.verdictDetail.text + " | reach " + app.terrain.ReturnReachMinutes.ToString("F1") + " min");
                app.SetWhatIfTank(100f);
                Check("What if: back to a full tank, the red is gone", float.IsInfinity(app.terrain.ReturnReachMinutes) && app.panel.verdictNote.text.StartsWith("ESTIMATE"));
            }
            // Back to the state the other pieces' checks expect: no amber route, NO ROUTE YET, the layer as it was.
            app.ClearPlannedRoute();
            app.panel.ShowNoRoute();
            app.terrain.ShowLayer(layerBeforePlan);
            app.RefreshPanel();

            // Leave the start state, labels turned to the desktop eye, for the base screenshots.
            pillars.FaceCamera(app.desktopCamera);
            Canvas.ForceUpdateCanvases();
        }

        static partial void ShotsRoute(WalkbackApp app)
        {
            if (app.routeView == null || app.stopPillars == null) return;
            var cam = app.desktopCamera;
            Vector3 homePosition = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;
            var site = app.terrain.Site;

            ShowLayer(app, MapLayer.Height);
            app.ShowPlannedRoute(TestPath(site), false);
            Vector3 above = app.mapRoot.position + new Vector3(0f, 1.0f, 0f);
            Vector3 eye = new Vector3(0f, 1.45f, 0.30f), table = new Vector3(0f, 0.80f, 1.0f);

            RouteShot(app, "route_01_top.png", above, above + Vector3.down, Vector3.forward);
            string top = Path.Combine(OutputFolder, "route_01_top.png");
            int amber = CountPixels(top, p => p.r > 190 && p.g > 125 && p.g < 215 && p.b < 45);
            int white = CountPixels(top, p => p.r > 225 && p.g > 225 && p.b > 225);
            Check("Route shot: the amber route shows from above", amber > 1500, amber + " amber px");
            Check("Route shot: the white dashes show from above", white > 300, white + " white px");

            RouteShot(app, "route_02_eye.png", eye, table, Vector3.up);

            ShowLayer(app, MapLayer.Slope);
            RouteShot(app, "route_03_on_slope.png", above, above + Vector3.down, Vector3.forward);
            ShowLayer(app, MapLayer.Height);

            if (app.stopPillars.Stops.Count > 0)
            {
                app.SelectStop(0);
                RouteShot(app, "route_04_selected.png", eye, table, Vector3.up);
                app.SelectStop(-1);
            }

            app.ClearPlannedRoute();
            cam.transform.SetPositionAndRotation(homePosition, homeRotation);
            app.stopPillars.FaceCamera(cam);
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Pillar labels that would cover each other from where the user looks go up until every one reads.</summary>
        static void CheckLabelLifting(WalkbackApp app, StopPillars pillars)
        {
            // The rule on its own, in camera space (z forward): labels 30 cm wide and 5 cm tall, a step of 5.5 cm.
            var label = new Vector2(0.3f, 0.05f);
            var up = new Vector3(0f, 0.055f, 0f);
            int[] behind = StopPillars.ViewStacks(new[] { new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 1.05f) }, new[] { label, label }, new[] { up, up }, null);
            int[] swapped = StopPillars.ViewStacks(new[] { new Vector3(0f, 0f, 1.05f), new Vector3(0f, 0f, 1f) }, new[] { label, label }, new[] { up, up }, null);
            int[] beside = StopPillars.ViewStacks(new[] { new Vector3(-0.3f, 0f, 1f), new Vector3(0.3f, 0f, 1.2f) }, new[] { label, label }, new[] { up, up }, null);
            int[] three = StopPillars.ViewStacks(new[] { new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 1.04f), new Vector3(0f, 0f, 1.08f) }, new[] { label, label, label }, new[] { up, up, up }, null);
            int[] fromAbove = StopPillars.ViewStacks(new[] { new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 1.05f) }, new[] { label, label }, new[] { up, Vector3.zero }, null);
            int[] unseen = StopPillars.ViewStacks(new[] { new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f) }, new[] { label, label }, new[] { up, up }, null);
            Check("Pillar labels: a label behind another goes up one step, the nearer one keeps its place; side by side nobody moves",
                behind[0] == 0 && behind[1] == 1 && swapped[0] == 1 && swapped[1] == 0 && beside[0] == 0 && beside[1] == 0 && three[0] == 0 && three[1] == 1 && three[2] == 2 &&
                fromAbove[1] == 0 && unseen[1] == 0, "three in a row: " + three[0] + ", " + three[1] + ", " + three[2]);
            // 1 cm clear of each other: stays down if it was down, stays up if it was up (no flicker at the edge).
            var close = new[] { new Vector3(0f, 0f, 1f), new Vector3(0.31f, 0f, 1f) };
            int[] wasDown = StopPillars.ViewStacks(close, new[] { label, label }, new[] { up, up }, new[] { 0, 0 });
            int[] wasUp = StopPillars.ViewStacks(close, new[] { label, label }, new[] { up, up }, new[] { 0, 1 });
            Check("Pillar labels: a lifted label comes down only when it is clear by a margin", wasDown[1] == 0 && wasUp[1] == 1);

            // The map on the table from the user's place in front of it (the habitat's label covered Altadena's from there).
            var cam = app.desktopCamera;
            Vector3 wasAt = cam.transform.position;
            Quaternion wasLooking = cam.transform.rotation;
            cam.transform.position = MissionPanel.UserEye;
            cam.transform.rotation = Quaternion.LookRotation(app.mapRoot.position - MissionPanel.UserEye, Vector3.up);
            pillars.SetGrowth(1f);
            pillars.FaceCamera(cam);
            Canvas.ForceUpdateCanvases();
            var views = new List<PillarView> { pillars.Habitat };
            views.AddRange(pillars.Stops);
            float worst = 0f;
            string worstPair = "none", lifted = "";
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i].viewLift > 0f) lifted += (lifted.Length > 0 ? ", " : "") + views[i].label.text;
                for (int j = 0; j < i; j++)
                {
                    Rect a = LabelViewRect(cam, views[i].labelRoot), b = LabelViewRect(cam, views[j].labelRoot);
                    float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                    if (w <= 0f || h <= 0f) continue;
                    float share = w * h / Mathf.Max(1e-9f, Mathf.Min(a.width * a.height, b.width * b.height));
                    if (share > worst) { worst = share; worstPair = views[i].label.text + " / " + views[j].label.text; }
                }
            }
            Check("Pillar labels: from the front of the table no label covers another (under 5 % of the smaller one at most)", worst < 0.05f,
                "worst " + (worst * 100f).ToString("F0") + " % (" + worstPair + "); lifted: " + (lifted.Length > 0 ? lifted : "none"));
            cam.transform.SetPositionAndRotation(wasAt, wasLooking);
            pillars.FaceCamera(cam);
        }

        /// <summary>The box a pillar's label takes on screen, in viewport units.</summary>
        static Rect LabelViewRect(Camera cam, RectTransform label)
        {
            var corners = new Vector3[4];
            label.GetWorldCorners(corners);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                Vector3 v = cam.WorldToViewportPoint(corner);
                x0 = Mathf.Min(x0, v.x); x1 = Mathf.Max(x1, v.x);
                y0 = Mathf.Min(y0, v.y); y1 = Mathf.Max(y1, v.y);
            }
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>Degrees the route turns, left and right added up, over the last <paramref name="meters"/> of a leg.</summary>
        static float TurningDegrees(MissionFallback.Plan plan, MissionFallback.Leg leg, float meters)
        {
            float turned = 0f, back = 0f;
            for (int i = leg.LastPoint - 1; i > leg.FirstPoint && back < meters; i--)
            {
                Vector2 before = plan.Path[i] - plan.Path[i - 1], after = plan.Path[i + 1] - plan.Path[i];
                back += after.magnitude;
                if (before.sqrMagnitude > 1e-8f && after.sqrMagnitude > 1e-8f) turned += Vector2.Angle(before, after);
            }
            return turned;
        }

        /// <summary>The smoothing of the plan's route, on made-up ground where the right answer is known.</summary>
        static void CheckRouteSmoothing()
        {
            var site = SiteLoader.Synthetic();
            int n = site.GridSize;
            float spacing = site.NodeSpacing, pace = ReturnStandIn.FlatMinutesPerKilometer / 1000f;
            for (int i = 0; i < site.Heights.Length; i++) site.Heights[i] = 0f;
            site.Info.habitat = new SitePoint { x = 0f, y = 0f };
            Vector2 stop = new Vector2(1000f, 500f);
            site.Stops = new[] { new StopInfo { id = "a", name = "A", x = stop.x, y = stop.y } };
            var slope = new float[n * n];

            // Flat ground: the grid goes diagonal and then straight (1 207 m); the smoothed leg is the straight line (1 118 m).
            var flat = MissionFallback.Make(site, slope, 100f);
            float straight = Vector2.Distance(site.Habitat, stop);
            Check("Route smoothing: on flat ground the leg becomes the straight line, with the straight line's minutes",
                flat.HasRoute && flat.Legs.Count == 2 && Mathf.Abs(flat.Legs[0].Meters - straight) < 0.5f && Mathf.Abs(flat.Legs[0].Minutes - straight * pace) < 0.03f &&
                flat.GridMeters > flat.WalkMeters + 100f && flat.Shortcuts > 0 && flat.Path.Count > 100,
                (flat.GridMeters / 2f).ToString("F0") + " m on the grid -> " + flat.Legs[0].Meters.ToString("F0") + " m, straight " + straight.ToString("F0") + " m");

            // A no-go wall from the north edge down to y = 150 m, well across the straight line (which passes x = 500 m at
            // y = 250 m): the route goes around its end. (Ending the wall at 250 m left a legal path past its tip only 0.07 m longer.)
            int wallColumn = n / 2;
            for (int r = 0; r < n; r++)
                if (site.SideMeters - r * spacing > 150f) slope[r * n + wallColumn] = TerrainLayers.SlopeLimitDegrees + 5f;
            var walled = MissionFallback.Make(site, slope, 100f);
            int onWall = 0;
            foreach (Vector2 p in walled.Path)
                if (!MissionFallback.Walkable(site, slope, p) && slope[ReturnFallback.NodeOf(site, p)] > TerrainLayers.SlopeLimitDegrees) onWall++;
            float through, beside;
            bool crosses = MissionFallback.StraightMinutes(site, slope, new Vector2(400f, 600f), new Vector2(600f, 600f), out through);
            bool passes = MissionFallback.StraightMinutes(site, slope, new Vector2(400f, 100f), new Vector2(600f, 100f), out beside);
            Check("Route smoothing: a straight line never crosses no-go ground; the route goes around the wall",
                walled.HasRoute && onWall == 0 && walled.Legs[0].Meters > straight + 1f && walled.Legs[0].Meters <= walled.GridMeters / 2f + 0.5f && !crosses && passes && Mathf.Abs(beside - 200f * pace) < 0.01f,
                walled.Legs[0].Meters.ToString("F0") + " m around, straight " + straight.ToString("F0") + " m, " + onWall + " points on the wall");

            // A ramp to the east steeper than the limit (1 in 2 = 26.6°) that the slope layer does not mark:
            // the step rule alone stops a line up it; along it (north) the walk is flat.
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++) { site.Heights[r * n + c] = c * spacing * 0.5f; slope[r * n + c] = 0f; }
            float up, along;
            bool climbs = MissionFallback.StraightMinutes(site, slope, new Vector2(100f, 500f), new Vector2(200f, 500f), out up);
            bool contours = MissionFallback.StraightMinutes(site, slope, new Vector2(100f, 400f), new Vector2(100f, 500f), out along);
            Check("Route smoothing: a line steeper than 20° is not taken, even where the slope layer says nothing",
                !climbs && contours && Mathf.Abs(along - 100f * pace) < 0.01f, "along the ramp " + along.ToString("F2") + " min for 100 m");

            // A gentle climb (1 in 10): walkable, at the grid's own slope pace.
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++) site.Heights[r * n + c] = c * spacing * 0.1f;
            float gentle;
            bool walks = MissionFallback.StraightMinutes(site, slope, new Vector2(100f, 500f), new Vector2(200f, 500f), out gentle);
            float expected = 100f * pace / ReturnFallback.SlopeFactor(0.1f);
            Check("Route smoothing: a straight line costs the same slope pace as the grid (Tobler on the 19.3 min/km flat pace)",
                walks && Mathf.Abs(gentle - expected) < expected * 0.01f, gentle.ToString("F2") + " min, expected " + expected.ToString("F2") + " min");
        }

        static void RouteShot(WalkbackApp app, string fileName, Vector3 from, Vector3 lookAt, Vector3 up)
        {
            var cam = app.desktopCamera;
            cam.transform.position = from;
            cam.transform.rotation = Quaternion.LookRotation(lookAt - from, up);
            app.stopPillars.FaceCamera(cam);
            Canvas.ForceUpdateCanvases();
            Shot(cam, fileName, from, lookAt, up);
        }

        /// <summary>Habitat to the first stop with a bend: a stand-in path for the test only.</summary>
        static Vector2[] TestPath(SiteData site)
        {
            Vector2 a = site.Habitat;
            Vector2 b = site.Stops.Length > 0 ? new Vector2(site.Stops[0].x, site.Stops[0].y) : new Vector2(site.SideMeters * 0.8f, site.SideMeters * 0.8f);
            Vector2 dir = b - a;
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized * dir.magnitude * 0.25f;
            var points = new List<Vector2> { a, Vector2.Lerp(a, b, 0.3f) + perp * 0.7f, Vector2.Lerp(a, b, 0.6f) + perp, b };
            for (int i = 0; i < points.Count; i++)
                points[i] = new Vector2(Mathf.Clamp(points[i].x, 20f, site.SideMeters - 20f), Mathf.Clamp(points[i].y, 20f, site.SideMeters - 20f));
            return points.ToArray();
        }

        static float Flat(Vector3 v)
        {
            return new Vector2(v.x, v.z).magnitude;
        }

        /// <summary>Lowest height of the vertices above the terrain under them (map meters).</summary>
        static float MinClearance(SiteData site, Vector3[] vertices)
        {
            float min = float.MaxValue, half = site.SideMeters / 2f;
            foreach (var v in vertices) min = Mathf.Min(min, v.y - RouteGeometry.HeightAt(site, v.x + half, v.z + half));
            return min;
        }

        static float MaxClearance(SiteData site, Vector3[] vertices)
        {
            float max = float.MinValue, half = site.SideMeters / 2f;
            foreach (var v in vertices) max = Mathf.Max(max, v.y - RouteGeometry.HeightAt(site, v.x + half, v.z + half));
            return max;
        }

        static int CountPixels(string pngPath, System.Func<Color32, bool> test)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGB24, false);
            image.LoadImage(File.ReadAllBytes(pngPath));
            int count = 0;
            foreach (var p in image.GetPixels32()) if (test(p)) count++;
            Object.DestroyImmediate(image);
            return count;
        }
    }
}
