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
