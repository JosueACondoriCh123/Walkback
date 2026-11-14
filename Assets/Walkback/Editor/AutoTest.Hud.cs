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
            Check("HUD: oxygen, way back and next stop are on the visor",
                hud.oxygenCard.lines[0].text.Contains("%") && hud.returnCard.lines[0].text.Contains("min") && hud.nextCard.lines[0].text.StartsWith("NEXT"),
                hud.oxygenCard.lines[0].text + " | " + hud.returnCard.lines[0].text + " | " + hud.nextCard.lines[0].text + " | " + hud.nextCard.lines[1].text);
            Check("HUD: every number is labelled as an estimate until session C's model lands (rule 1)", hud.estimateTag.gameObject.activeSelf && hud.estimateTag.lines[0].text.StartsWith("ESTIMATE"));
            Check("HUD: no TURN BACK warning at this stop", !hud.warningCard.gameObject.activeSelf);

            Vector3 eye = cam.transform.position;
            float worst = 0f;
            foreach (var card in new[] { hud.oxygenCard, hud.returnCard, hud.nextCard, hud.estimateTag })
                worst = Mathf.Max(worst, Vector3.Angle(cam.transform.forward, card.transform.position - eye));
            worst = Mathf.Max(worst, Vector3.Angle(cam.transform.forward, hud.miniMap.transform.position - eye));
            Check("HUD: everything within 25° of the center of the view", worst < 25f, worst.ToString("F1") + "° worst");
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
            Check("Mini map: shows the HiRISE photo at most 1024 px wide (memory)", map.Photo != null && (!site.IsMarsData || (map.PhotoIsOrtho && map.Photo.width <= MiniMap.PhotoWidth)),
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

            // Leave as built.
            hud.Hide();
            wrist.SetVisible(false);
            app.siteJump.ExitJump();
            app.siteJump.HideNoPanorama();
            cam.transform.SetPositionAndRotation(home, homeRotation);
            Check("HUD checks leave the scene as built", !hud.Visible && !wrist.Visible && !app.siteJump.Inside && app.table.gameObject.activeSelf == tableWas);
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
