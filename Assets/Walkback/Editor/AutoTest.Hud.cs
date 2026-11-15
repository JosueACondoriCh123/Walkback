// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;
using UnityEngine.UI;

namespace Walkback.EditorTools
{
    /// <summary>Checks and screenshots of the helmet HUD, its 2D map and the wrist map. Leaves the scene as built.</summary>
    public static partial class AutoTest
    {
        static partial void CheckHud(WalkbackApp app)
        {
            var hud = app.hud;
            var wrist = app.wristMap;
            Check("HUD: helmet HUD and wrist map are in the scene, hidden at start", hud != null && wrist != null && !hud.Visible && !wrist.Visible);
            if (hud != null) CheckHudMarks(hud);

            // The estimate on hand-made inputs: 1 km on flat ground = 19.3 min and 20 L (ficha A test numbers).
            var line = SiteLoader.Synthetic();
            line.Info.habitat = new SitePoint { x = 0f, y = 0f };
            line.Stops = new[] { new StopInfo { id = "a", name = "A", x = 1000f, y = 0f, tasks = new[] { new StopTask { name = "t", minutes = 10f } } } };
            var at = HudEstimate.AtStop(line, 0);
            Check("HUD estimate: 1 km straight = 19.3 min, 4.0 % of the tank", Mathf.Abs(at.elapsedMinutes - 19.3f) < 0.01f && Mathf.Abs(100f - at.oxygenLeftPercent - 20f / 4.97f) < 0.01f,
                at.elapsedMinutes.ToString("F2") + " min, O2 left " + at.oxygenLeftPercent.ToString("F2") + " %");
            Check("HUD estimate: way back and next target after the last stop is the habitat", Mathf.Abs(at.returnMinutes - 19.3f) < 0.01f && at.nextName == "Habitat" && Mathf.Abs(at.nextMeters - 1000f) < 0.01f && Mathf.Approximately(at.taskMinutes, 10f));
            Check("HUD estimate: TURN BACK only when the tank above the reserve no longer covers the way back",
                HudEstimate.TurnBack(460f, 10f) && !HudEstimate.TurnBack(100f, 10f) && !at.turnBack);

            Check("HUD: minutes are rounded before they are split into hours (never \"1 h 60 min\")",
                HelmetHud.MinutesText(119.6f) == "2 h 0 min" && HelmetHud.MinutesText(59.7f) == "1 h 0 min" && HelmetHud.MinutesText(174.7f) == "2 h 55 min" &&
                HelmetHud.MinutesText(132f) == "2 h 12 min" && HelmetHud.MinutesText(42.7f) == "43 min" && HelmetHud.MinutesText(0.4f) == "under 1 min",
                HelmetHud.MinutesText(119.6f) + " | " + HelmetHud.MinutesText(59.7f) + " | " + HelmetHud.MinutesText(174.7f));
            // With a plan, the visor at a stop shows the plan's numbers and its tank (a WHAT IF reaches the visor in the photo too).
            {
                for (int i = 0; i < line.Heights.Length; i++) line.Heights[i] = 0f;
                var flat = new float[line.GridSize * line.GridSize];
                var full = HudEstimate.AtStop(line, 0, null, MissionFallback.Make(line, flat, 100f));
                var low = HudEstimate.AtStop(line, 0, null, MissionFallback.Make(line, flat, 8f));
                Check("HUD estimate: at a stop the visor uses the plan's tank (WHAT IF at 8 %: 4 % left on arrival and TURN BACK)",
                    Mathf.Abs(full.elapsedMinutes - 19.3f) < 0.05f && Mathf.Abs(full.oxygenLeftPercent - 95.98f) < 0.05f && !full.turnBack &&
                    Mathf.Abs(low.oxygenLeftPercent - 3.98f) < 0.05f && low.turnBack && low.returnState == HudEstimate.ReturnState.NotCovered,
                    "full tank " + full.oxygenLeftPercent.ToString("F1") + " %, tank at 8 % " + low.oxygenLeftPercent.ToString("F1") + " %, turn back " + low.turnBack);
            }

            // Stand at a stop with a photo, as after JUMP TO SITE.
            var site = app.terrain.Site;
            int stop = -1;
            for (int i = 0; i < site.Stops.Length; i++) if (!string.IsNullOrEmpty(site.Stops[i].panorama)) { stop = i; break; }
            if (stop < 0) stop = 0;
            var cam = app.desktopCamera;
            Vector3 home = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;
            cam.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            bool tableWas = app.table.gameObject.activeSelf;
            app.siteJump.Jump(stop);
            hud.ShowAt(stop);
            hud.SnapTo(cam.transform);
            var stopInfo = site.Stops[stop];
            wrist.Prepare(app, new Vector2(stopInfo.x, stopInfo.y), stopInfo.name);
            wrist.SetVisible(true);
            wrist.HoldInFrontOf(cam.transform);
            Canvas.ForceUpdateCanvases();

            Check("HUD: shows at the stop", hud.Visible && hud.AtStop == stop, stopInfo.name + (app.siteJump.Inside ? ", inside its photo" : ", no photo"));
            if (app.Plan != null && app.Plan.HasRoute && stop + 1 < app.Plan.Legs.Count)
            {
                float planWalk = 0f, planWork = 0f;
                for (int i = 0; i <= stop; i++)
                {
                    planWalk += app.Plan.Legs[i].Minutes;
                    if (i < stop) planWork += app.Plan.Legs[i].WorkMinutesAtEnd;
                }
                float planLeft = (app.Plan.TankLiters - planWalk * HudEstimate.WalkLitersPerMinute - planWork * HudEstimate.DesignLitersPerMinute) / HudEstimate.TankLiters * 100f;
                Check("HUD: at a stop the visor's time, oxygen and next leg are the plan's own (the visor and the board agree)",
                    Mathf.Abs(hud.Current.elapsedMinutes - (planWalk + planWork)) < 0.01f && Mathf.Abs(hud.Current.oxygenLeftPercent - planLeft) < 0.01f &&
                    Mathf.Abs(hud.Current.nextMeters - app.Plan.Legs[stop + 1].Meters) < 0.01f && Mathf.Abs(hud.Current.nextMinutes - app.Plan.Legs[stop + 1].Minutes) < 0.01f,
                    "elapsed " + hud.Current.elapsedMinutes.ToString("F1") + " min, O2 left " + hud.Current.oxygenLeftPercent.ToString("F1") + " %, next " + hud.Current.nextMeters.ToString("F0") + " m");
            }
            else Note("HUD: no plan with a route at this point of the test, the visor shows the straight-line estimate");
            Check("HUD: oxygen, way back and next stop are on the visor",
                hud.oxygenCard.lines[0].text.Contains("%") && hud.returnCard.lines[0].text.Contains("min") && hud.nextCard.lines[0].text.StartsWith("NEXT"),
                hud.oxygenCard.lines[0].text + " | " + hud.returnCard.lines[0].text + " | " + hud.nextCard.lines[0].text + " | " + hud.nextCard.lines[1].text);
            Check("HUD: every number is labelled as an estimate until session C's model lands (rule 1)", hud.estimateTag.gameObject.activeSelf && hud.estimateTag.lines[0].text.StartsWith("ESTIMATE"));
            // The visor and the map on the table must agree on the way back from this stop.
            {
                int hn = site.GridSize;
                int hc = Mathf.Clamp(Mathf.RoundToInt(stopInfo.x / site.NodeSpacing), 0, hn - 1);
                int hr = Mathf.Clamp(Mathf.RoundToInt((site.SideMeters - stopInfo.y) / site.NodeSpacing), 0, hn - 1);
                float onMap = app.terrain.ReturnMinutes[hr * hn + hc];
                Check("HUD: the way back is the same number as the map's return layer", float.IsInfinity(onMap) || Mathf.Abs(hud.Current.returnMinutes - onMap) < 0.01f,
                    "visor " + hud.Current.returnMinutes.ToString("F1") + " min, map " + onMap.ToString("F1") + " min");
            }
            Check("HUD: no TURN BACK warning at this stop", !hud.warningCard.gameObject.activeSelf);
            CheckHudMath();
            Check("HUD: inside a 360° photo there is no compass, no heading and no arrow (north is not known there)",
                !hud.HeadingKnown && hud.compass != null && !hud.compass.gameObject.activeSelf && !hud.headingCard.gameObject.activeSelf && !hud.arrowPlate.gameObject.activeSelf);
            {
                float barLength = hud.oxygenFill.localScale.x;
                var barBlock = new MaterialPropertyBlock();
                hud.oxygenFillRenderer.GetPropertyBlock(barBlock);
                Color barColor = barBlock.GetColor("_BaseColor"), numberColor = hud.oxygenCard.lines[0].color;
                Check("HUD: the oxygen bar is as long as the oxygen left, grows from its left end and has the number's colour",
                    Mathf.Abs(barLength - HelmetHud.OxygenBarMeters * hud.Current.oxygenLeftPercent / 100f) < 0.0005f &&
                    Mathf.Abs(hud.oxygenFill.localPosition.x - (barLength - HelmetHud.OxygenBarMeters) / 2f) < 0.0005f &&
                    Mathf.Abs(barColor.r - numberColor.r) < 0.01f && Mathf.Abs(barColor.g - numberColor.g) < 0.01f && numberColor == HelmetHud.OxygenColor(hud.Current.oxygenLeftPercent),
                    (barLength * 1000f).ToString("F0") + " mm of " + (HelmetHud.OxygenBarMeters * 1000f).ToString("F0") + " mm for " + hud.Current.oxygenLeftPercent.ToString("F0") + " %");
                Check("HUD: the minutes back are green, amber or red by the oxygen margin, and the margin is under them",
                    hud.returnCard.lines[0].color == HelmetHud.ReturnColor(hud.Current.returnState) && hud.Current.returnState == HudEstimate.ReturnState.Covered &&
                    hud.returnCard.lines[2].text == HelmetHud.MarginText(hud.Current.returnState, hud.Current.marginMinutes, false) && hud.returnCard.lines[2].text.StartsWith("spare O2"),
                    hud.returnCard.lines[0].text + " | " + hud.returnCard.lines[2].text);
                float climb = RouteGeometry.HeightAt(site, hud.Current.nextPoint.x, hud.Current.nextPoint.y) - RouteGeometry.HeightAt(site, stopInfo.x, stopInfo.y);
                Check("HUD: the next card says the height difference to the next stop, from the relief",
                    Mathf.Abs(hud.Current.nextClimbMeters - climb) < 0.01f && hud.nextCard.lines[2].text.StartsWith(HudCompass.ClimbText(climb)), hud.nextCard.lines[2].text);
                string wide = "";
                foreach (var card in new[] { hud.oxygenCard, hud.returnCard, hud.nextCard })
                    foreach (var cardLine in card.lines)
                    {
                        float box = ((RectTransform)cardLine.transform).rect.width;
                        if (cardLine.preferredWidth > box) wide += cardLine.text + " " + cardLine.preferredWidth.ToString("F0") + "/" + box.ToString("F0") + " px; ";
                    }
                Check("HUD: every line of the three cards fits on one line", wide.Length == 0, wide);
            }
            Check("HUD: inside a 360° photo the mini map's \"you\" is a dot, not an arrow (the headset's direction is not one on Mars)",
                !hud.miniMap.HeadingKnown && (hud.miniMap.youHeading == null || !hud.miniMap.youHeading.gameObject.activeSelf));
            Text estimateText = hud.estimateTag.lines[0];
            float estimateBox = ((RectTransform)estimateText.transform).rect.width;
            Check("HUD: the estimate line is short (40 characters at most) and fits its box on one line",
                HelmetHud.EstimateLine.StartsWith("ESTIMATE") && HelmetHud.EstimateLine.Length <= 40 && estimateText.text.StartsWith(HelmetHud.EstimateLine) && (!site.IsMarsData || estimateText.preferredWidth <= estimateBox),
                HelmetHud.EstimateLine.Length + " characters, " + estimateText.preferredWidth.ToString("F0") + " of " + estimateBox.ToString("F0") + " px");

            Vector3 eye = cam.transform.position;
            float worst = 0f;
            foreach (var card in new[] { hud.oxygenCard, hud.returnCard, hud.nextCard, hud.estimateTag })
                worst = Mathf.Max(worst, Vector3.Angle(cam.transform.forward, card.transform.position - eye));
            worst = Mathf.Max(worst, Vector3.Angle(cam.transform.forward, hud.miniMap.transform.position - eye));
            foreach (var extra in new[] { hud.compass.transform, hud.headingCard.transform, hud.arrowPlate })
                worst = Mathf.Max(worst, Vector3.Angle(cam.transform.forward, extra.position - eye));
            Check("HUD: everything within 26° of the center of the view (the compass and the arrow too)", worst < 26f, worst.ToString("F1") + "° worst");
            // The data is spread out: no two boxes of the visor overlap, and the very middle stays clear.
            {
                bool middleClear;
                int boxCount;
                string overlaps = HudOverlaps(hud, out middleClear, out boxCount);
                Check("HUD: at a stop no two boxes of the visor overlap, and only TURN BACK NOW may take the middle", overlaps.Length == 0 && middleClear && boxCount >= 7, boxCount + " boxes " + overlaps);
            }
            float smallest = float.MaxValue;
            bool uiOnly = true;
            foreach (var text in hud.GetComponentsInChildren<Text>(true))
            {
                smallest = Mathf.Min(smallest, text.fontSize * text.transform.lossyScale.y);
                if (text.material == null || text.material.shader.name != "Walkback/UI") uiOnly = false;
            }
            Check("HUD: text at least 17 mm tall at 1.2 m", smallest >= 0.0169f, (smallest * 1000f).ToString("F0") + " mm smallest");
            Check("HUD: numbers use a decimal point, whatever the device language", !hud.nextCard.lines[1].text.Contains(","), hud.nextCard.lines[1].text);
            Check("HUD: text never writes alpha (passthrough)", uiOnly);

            var map = hud.miniMap;
            var here = new Vector2(stopInfo.x, stopInfo.y);
            Vector3 youLocal = map.you.localPosition;
            Vector3 expect = map.LocalOf(here, 0.005f);
            Check("Mini map: you are where the stop is", map.you.gameObject.activeSelf && (youLocal - expect).magnitude < 0.0005f && Mathf.Abs(youLocal.x) <= MiniMap.SizeMeters / 2f && Mathf.Abs(youLocal.y) <= MiniMap.SizeMeters / 2f,
                "window " + map.WindowMeters.ToString("F0") + " m around " + map.WindowCenter.x.ToString("F0") + ", " + map.WindowCenter.y.ToString("F0"));
            Check("Mini map: the habitat and the next target fit in the window", map.InWindow(site.Habitat) && map.InWindow(hud.Current.nextPoint));
            Check("Mini map: north is up (a point 10 m north is higher on the map)", map.LocalOf(here + new Vector2(0f, 10f), 0f).y > map.LocalOf(here, 0f).y && map.LocalOf(here + new Vector2(10f, 0f), 0f).x > map.LocalOf(here, 0f).x);
            // No second copy in memory: either the terrain's own photo texture (shared), or a small one.
            bool sharedPhoto = app.terrain.HasPhoto && ReferenceEquals(map.Photo, app.terrain.LayerTexture(MapLayer.Photo));
            Check("Mini map: shows the HiRISE photo without a second copy in memory (shares the terrain's)", map.Photo != null && (!site.IsMarsData || (map.PhotoIsOrtho && (sharedPhoto || map.Photo.width <= MiniMap.PhotoWidth))),
                map.Photo != null ? map.Photo.width + " x " + map.Photo.height + (map.PhotoIsOrtho ? ", ortho.jpg" : ", height paint") : "no image");

            var copyMesh = wrist.terrainCopy.sharedMesh;
            Check("Wrist map: the same terrain as the table", copyMesh != null && copyMesh == app.terrain.GetComponent<MeshFilter>().sharedMesh);
            var bounds = wrist.terrainRenderer.bounds;
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            Check("Wrist map: about 14 cm across", width > 0.12f && width < 0.22f, (width * 100f).ToString("F1") + " cm (tilted box)");
            Vector3 pinExpect = RouteGeometry.ToMap(site, here, 26f);
            Check("Wrist map: the you-pin stands on the stop", (wrist.youPin.localPosition - pinExpect).magnitude < 0.01f);
            Check("Wrist map: tilted toward the viewer", Vector3.Dot(wrist.transform.up, (eye - wrist.transform.position).normalized) > 0.3f);
            Check("Wrist map: the raised-hand test (in view = shown, at the side = hidden)",
                WristMap.LookingAt(wrist.transform, cam.transform) && !WristMap.LookingAt(MakeProbe(eye + Vector3.right), cam.transform));

            // The map off the wrist: left in the air it is bigger and stays where it was put when the head moves.
            {
                Vector3 wristAt = wrist.transform.position;
                Check("Wrist map: on the wrist until X is pressed", !wrist.Placed && Mathf.Abs(wrist.Width - WristMap.WidthMeters) < 1e-5f);
                app.ToggleMapPlaced(cam.transform, null);
                Vector3 placedAt = wrist.transform.position;
                Quaternion placedRotation = wrist.transform.rotation;
                float placedWide = Mathf.Max(wrist.terrainRenderer.bounds.size.x, wrist.terrainRenderer.bounds.size.z);
                Vector3 camWas = cam.transform.position;
                Quaternion camLooked = cam.transform.rotation;
                cam.transform.SetPositionAndRotation(camWas + new Vector3(0.6f, 0f, -0.4f), Quaternion.LookRotation(new Vector3(-0.5f, -0.2f, 1f)));
                wrist.KeepPlaced(cam.transform, false, Quaternion.identity);
                bool stayed = (wrist.transform.position - placedAt).magnitude < 1e-5f && Quaternion.Angle(wrist.transform.rotation, placedRotation) < 0.01f;
                Check("Wrist map: X leaves it in the air, 40 cm wide, within reach in front of the viewer",
                    wrist.Placed && wrist.Visible && Mathf.Abs(wrist.Width - WristMap.PlacedWidthMeters) < 1e-5f && placedWide > 0.36f && placedWide < 0.62f &&
                    Vector3.Distance(placedAt, camWas) < 1.2f && Vector3.Dot(camLooked * Vector3.forward, placedAt - camWas) > 0f && (placedAt - wristAt).magnitude > 0.05f,
                    (placedWide * 100f).ToString("F0") + " cm (tilted box), " + Vector3.Distance(placedAt, camWas).ToString("F2") + " m from the eyes");
                Check("Wrist map: left in the air it is fixed in the room (it does not follow the head), and its label turns to the viewer",
                    stayed && Vector3.Dot(wrist.label.transform.forward, (wrist.label.transform.position - cam.transform.position).normalized) > 0.99f);
                Check("Wrist map: inside a 360° photo north is not known, so the placed map is not turned to it", !wrist.NorthAligned);
                // With north known (mission mode) it lies flat, its north on Mars' north.
                Quaternion marsTurn = Quaternion.Euler(0f, 37f, 0f);
                wrist.KeepPlaced(cam.transform, true, marsTurn);
                Vector3 mapNorth = wrist.model.TransformDirection(Vector3.forward);
                Check("Wrist map: with north known the placed map lies flat with its north on Mars' north",
                    wrist.NorthAligned && Vector3.Angle(wrist.transform.up, Vector3.up) < 0.01f && Vector3.Angle(mapNorth, marsTurn * Vector3.forward) < 0.01f &&
                    (wrist.transform.position - placedAt).magnitude < 1e-5f);
                float labelMm = wrist.label.lines[0].fontSize * wrist.label.lines[0].transform.lossyScale.y * 1000f;
                Check("Wrist map: the placed map's label is 24 mm or more (read from over a meter)", labelMm >= 24f, labelMm.ToString("F0") + " mm");
                app.ToggleMapPlaced(cam.transform, null);
                cam.transform.SetPositionAndRotation(camWas, camLooked);
                wrist.HoldInFrontOf(cam.transform);
                float backWide = Mathf.Max(wrist.terrainRenderer.bounds.size.x, wrist.terrainRenderer.bounds.size.z);
                Check("Wrist map: X again takes it back to the wrist, 14 cm wide", !wrist.Placed && !wrist.NorthAligned && backWide > 0.12f && backWide < 0.22f &&
                    (wrist.transform.position - wristAt).magnitude < 0.001f, (backWide * 100f).ToString("F1") + " cm");
            }

            // Leave as built.
            hud.Hide();
            wrist.SetVisible(false);
            app.siteJump.ExitJump();
            app.siteJump.HideNoPanorama();
            cam.transform.SetPositionAndRotation(home, homeRotation);
            Check("HUD checks leave the scene as built", !hud.Visible && !wrist.Visible && !app.siteJump.Inside && app.table.gameObject.activeSelf == tableWas);
        }

        /// <summary>
        /// The boxes the visor shows now (TURN BACK NOW always counts, shown or not), in the visor's plane:
        /// which ones overlap, and whether anything but TURN BACK NOW comes within 5 cm of the middle.
        /// </summary>
        /// <summary>
        /// The visor's arrow and the mini map's point are meshes the scene builder makes by hand: they must be
        /// wound like Unity's quad (seen from -z, where the visor is looked at from), or they would not be drawn.
        /// </summary>
        static void CheckHudMarks(HelmetHud hud)
        {
            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Quad);
            float quadFacing = MeshFacing(probe.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(probe);
            MeshFilter arrow = hud.arrow != null ? hud.arrow.GetComponentInChildren<MeshFilter>(true) : null;
            MeshFilter point = hud.miniMap != null && hud.miniMap.youHeading != null ? hud.miniMap.youHeading.GetComponent<MeshFilter>() : null;
            bool arrowFaces = arrow != null && arrow.sharedMesh != null && quadFacing != 0f && MeshFacing(arrow.sharedMesh) == quadFacing;
            bool pointFaces = point != null && point.sharedMesh != null && quadFacing != 0f && MeshFacing(point.sharedMesh) == quadFacing;

            // The arrow turns about the middle of its 100 mm plate: its farthest corner stays on the plate at any
            // turn, and "up" is one single vertex, the tip, on the axis.
            float reach = 0f, top = float.MinValue, tipX = 1f;
            int tips = 0;
            if (arrow != null && arrow.sharedMesh != null)
                foreach (Vector3 v in arrow.sharedMesh.vertices)
                {
                    reach = Mathf.Max(reach, new Vector2(v.x, v.y).magnitude);
                    if (v.y > top + 0.00001f) { top = v.y; tips = 1; tipX = v.x; }
                    else if (Mathf.Abs(v.y - top) <= 0.00001f) tips++;
                }
            bool onPlate = reach > 0.02f && reach < 0.05f && tips == 1 && Mathf.Abs(tipX) < 0.00001f;

            // The mini map's "you": a dark rim behind the dot and behind the point, a little bigger than each.
            Transform you = hud.miniMap != null ? hud.miniMap.you : null;
            Transform dotRim = you != null ? you.Find("Outline") : null;
            Transform pointRim = point != null ? point.transform.Find("Outline") : null;
            MeshRenderer youRenderer = you != null ? you.GetComponent<MeshRenderer>() : null;
            bool rim = dotRim != null && pointRim != null && youRenderer != null && dotRim.localPosition.z > 0f && pointRim.localPosition.z > 0f &&
                dotRim.localScale.x >= 1.2f && dotRim.localScale.x <= 1.5f && pointRim.GetComponent<MeshFilter>() != null && MeshFacing(pointRim.GetComponent<MeshFilter>().sharedMesh) == quadFacing &&
                dotRim.GetComponent<MeshRenderer>().sharedMaterial != youRenderer.sharedMaterial && pointRim.GetComponent<MeshRenderer>().sharedMaterial != youRenderer.sharedMaterial;
            Check("HUD: the mini map's \"you\" (dot and point) has a dark rim behind it, so it reads on the bright parts of the photo", rim,
                dotRim != null && pointRim != null ? "rim " + ((dotRim.localScale.x - 1f) / 2f * hud.miniMap.you.localScale.x * 1000f).ToString("F1") + " mm around the dot" : "no rim");
            Check("HUD: the arrow to the next stop and the mini map's point are wound like Unity's quad (seen from the visor's side); the arrow points up and stays on its plate at any turn",
                arrowFaces && pointFaces && onPlate,
                "arrow " + arrowFaces + ", point " + pointFaces + ", arrow reaches " + (reach * 1000f).ToString("F0") + " mm from the middle of a 100 mm plate");
        }

        /// <summary>-1 or +1 when every triangle of a flat mesh is wound the same way about z; 0 when they differ or one has no area.</summary>
        static float MeshFacing(Mesh mesh)
        {
            Vector3[] v = mesh.vertices;
            int[] t = mesh.triangles;
            float facing = 0f;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                float z = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).z;
                if (Mathf.Abs(z) < 1e-12f) return 0f;
                float sign = Mathf.Sign(z);
                if (facing == 0f) facing = sign;
                else if (sign != facing) return 0f;
            }
            return facing;
        }

        static string HudOverlaps(HelmetHud hud, out bool middleClear, out int count)
        {
            var names = new System.Collections.Generic.List<string>();
            var boxes = new System.Collections.Generic.List<Rect>();
            System.Action<string, Transform, float, float> add = (name, t, w, h) =>
            {
                if (name != "TurnBack" && !t.gameObject.activeInHierarchy) return;
                Vector3 c = hud.content.InverseTransformPoint(t.position);
                names.Add(name);
                boxes.Add(new Rect(c.x - w / 2f, c.y - h / 2f, w, h));
            };
            foreach (var card in new[] { hud.oxygenCard, hud.returnCard, hud.nextCard, hud.estimateTag, hud.warningCard, hud.headingCard })
                add(card.name, card.transform, card.backPlate.localScale.x, card.backPlate.localScale.y);
            add("Compass", hud.compass.transform, HudCompass.WidthMeters, HudCompass.HeightMeters);
            add("NextArrow", hud.arrowPlate, 0.10f, 0.10f);
            add("MiniMap", hud.miniMap.transform, MiniMap.SizeMeters + 0.012f, MiniMap.SizeMeters + 0.012f);
            add("MapCaption", hud.miniMap.caption.transform, hud.miniMap.caption.backPlate.localScale.x, hud.miniMap.caption.backPlate.localScale.y);
            string overlaps = "";
            middleClear = true;
            count = boxes.Count;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (names[i] != "TurnBack" && boxes[i].Overlaps(new Rect(-0.05f, -0.05f, 0.10f, 0.10f))) middleClear = false;
                for (int j = i + 1; j < boxes.Count; j++)
                    if (boxes[i].Overlaps(boxes[j])) overlaps += names[i] + " over " + names[j] + "; ";
            }
            return overlaps;
        }

        /// <summary>The visor's pure arithmetic and the compass tape, on hand-made inputs.</summary>
        static void CheckHudMath()
        {
            Vector2 o = Vector2.zero;
            Check("Compass: bearings are degrees clockwise from north (north 0, east 90, south 180, west 270, north-east 45)",
                Mathf.Abs(HudCompass.Bearing(o, new Vector2(0f, 10f))) < 0.01f && Mathf.Abs(HudCompass.Bearing(o, new Vector2(10f, 0f)) - 90f) < 0.01f &&
                Mathf.Abs(HudCompass.Bearing(o, new Vector2(0f, -10f)) - 180f) < 0.01f && Mathf.Abs(HudCompass.Bearing(o, new Vector2(-10f, 0f)) - 270f) < 0.01f &&
                Mathf.Abs(HudCompass.Bearing(new Vector2(5f, 5f), new Vector2(15f, 15f)) - 45f) < 0.01f);
            Check("Compass: the eight points and the three-digit bearing (045° NE, 000° N just under 360°)",
                HudCompass.Cardinal(0f) == "N" && HudCompass.Cardinal(22.4f) == "N" && HudCompass.Cardinal(22.6f) == "NE" && HudCompass.Cardinal(337.6f) == "N" && HudCompass.Cardinal(270f) == "W" &&
                HudCompass.BearingText(45f) == "045° NE" && HudCompass.BearingText(359.6f) == "000° N" && HudCompass.BearingText(7.2f) == "007° N" && HudCompass.BearingText(-90f) == "270° W",
                HudCompass.BearingText(45f) + ", " + HudCompass.BearingText(359.6f));
            Check("Compass: the offset from the heading goes the short way round (+ to the right)",
                Mathf.Abs(HudCompass.Offset(10f, 350f) - 20f) < 0.01f && Mathf.Abs(HudCompass.Offset(350f, 10f) + 20f) < 0.01f && Mathf.Abs(Mathf.Abs(HudCompass.Offset(180f, 0f)) - 180f) < 0.01f);
            Check("HUD: height difference in words (climb, descent, level under half a meter)",
                HudCompass.ClimbText(12.4f) == "climb 12 m" && HudCompass.ClimbText(-7.6f) == "descent 8 m" && HudCompass.ClimbText(0.3f) == "level" && HudCompass.ClimbText(-0.3f) == "level");
            float rate = HudEstimate.WalkLitersPerMinute;
            Check("HUD: way back green when the oxygen above the reserve covers it twice, amber when less, red when it does not cover it",
                HudEstimate.StateOf(100f, 40f) == HudEstimate.ReturnState.Covered && HudEstimate.StateOf(80f, 40f) == HudEstimate.ReturnState.Covered &&
                HudEstimate.StateOf(79f, 40f) == HudEstimate.ReturnState.Tight && HudEstimate.StateOf(40f, 40f) == HudEstimate.ReturnState.Tight &&
                HudEstimate.StateOf(39f, 40f) == HudEstimate.ReturnState.NotCovered && HudEstimate.StateOf(10f, 0f) == HudEstimate.ReturnState.Covered &&
                (HudEstimate.StateOf(39f, 40f) == HudEstimate.ReturnState.NotCovered) == HudEstimate.TurnBack(HudEstimate.TankLiters - HudEstimate.ReserveLiters - 39f, 40f));
            Check("HUD: the oxygen margin is the walking minutes left after the way back (negative when short)",
                Mathf.Abs(HudEstimate.MarginMinutes(100f, 40f) - 60f / rate) < 0.01f && HudEstimate.MarginMinutes(30f, 40f) < 0f &&
                HelmetHud.MarginText(HudEstimate.ReturnState.Covered, 125f, false) == "spare O2 2 h 5 min" &&
                HelmetHud.MarginText(HudEstimate.ReturnState.NotCovered, -6f, false) == "short by 6 min" &&
                HelmetHud.MarginText(HudEstimate.ReturnState.NotCovered, 0f, true) == "no way back on foot",
                HelmetHud.MarginText(HudEstimate.ReturnState.Covered, 125f, false));

            // The tape itself: 40° to each side, 5 mm per degree.
            WalkbackApp app = UnityEngine.Object.FindFirstObjectByType<WalkbackApp>();
            HudCompass tape = app != null && app.hud != null ? app.hud.compass : null;
            Check("Compass: the tape is in the visor, with 8 letters and a tick every 15° between them", tape != null && tape.letters.Length == 8 && tape.ticks.Length == 16 && tape.stopMarker != null);
            if (tape == null) return;
            tape.Show(30f, true, 30f);
            Transform north = tape.letters[0].transform, east = tape.letters[2].transform;
            bool facing = north.gameObject.activeSelf && Mathf.Abs(north.localPosition.x + 30f * HudCompass.MetersPerDegree) < 0.0005f && !east.gameObject.activeSelf &&
                tape.stopMarker.gameObject.activeSelf && Mathf.Abs(tape.stopMarker.localPosition.x) < 0.0005f && !tape.TargetOffTape;
            tape.Show(30f, true, 120f);
            bool right = tape.TargetOffTape && tape.stopMarker.localPosition.x > HudCompass.WidthMeters / 2f - 0.011f && Mathf.Abs(tape.TargetOffset - 90f) < 0.01f;
            tape.Show(30f, true, 300f);
            bool left = tape.TargetOffTape && tape.stopMarker.localPosition.x < -HudCompass.WidthMeters / 2f + 0.011f;
            tape.Show(30f, false, 0f);
            bool none = !tape.stopMarker.gameObject.activeSelf;
            int shown = 0;
            foreach (var letter in tape.letters) if (letter.gameObject.activeSelf) shown++;
            tape.Show(0f, false, 0f);
            Check("Compass: looking 30° east of north, N is 30° to the left; the amber bar is on the stop's bearing, or waits at the end of the tape on that side",
                facing && right && left && none && shown >= 1 && shown <= 2, shown + " letters in the window");
        }

        static Transform probe;
        static Transform MakeProbe(Vector3 at)
        {
            if (probe == null)
            {
                var go = new GameObject("HudTestProbe") { hideFlags = HideFlags.HideAndDontSave };
                probe = go.transform;
            }
            probe.position = at;
            return probe;
        }

        static partial void ShotsHud(WalkbackApp app)
        {
            var hud = app.hud;
            var wrist = app.wristMap;
            var site = app.terrain.Site;
            var cam = app.desktopCamera;
            Vector3 home = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;
            cam.transform.rotation = Quaternion.LookRotation(Vector3.forward);

            bool first = true;
            for (int i = 0; i < site.Stops.Length; i++)
            {
                var stop = site.Stops[i];
                app.siteJump.Jump(i);
                if (!app.siteJump.Inside)
                {
                    // At runtime the HUD only shows inside a stop's photo: no photo, no visor shot.
                    app.siteJump.HideNoPanorama();
                    Note("HUD: no visor screenshot for " + stop.name + " (no panorama for this stop)");
                    continue;
                }
                hud.ShowAt(i);
                hud.SnapTo(cam.transform);
                wrist.Prepare(app, new Vector2(stop.x, stop.y), stop.name);
                wrist.SetVisible(true);
                wrist.HoldInFrontOf(cam.transform);
                Canvas.ForceUpdateCanvases();
                string tag = (i + 1).ToString("00");
                Shot(cam, "hud_" + tag + "_visor_" + stop.id + ".png", home, home + Vector3.forward);
                if (first)
                {
                    first = false;
                    Shot(cam, "hud_" + tag + "b_minimap.png", home, hud.miniMap.transform.position);
                    Shot(cam, "hud_" + tag + "c_wrist.png", home, wrist.transform.position);
                }
                hud.Hide();
                wrist.SetVisible(false);
                app.siteJump.ExitJump();
                app.siteJump.HideNoPanorama();
            }
            cam.transform.SetPositionAndRotation(home, homeRotation);
        }
    }
}
