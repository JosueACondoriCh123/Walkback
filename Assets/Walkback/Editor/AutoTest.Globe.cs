// Written with Claude Code (AI), directed and tested by team MARSXR
using UnityEngine;

namespace Walkback.EditorTools
{
    /// <summary>Checks and screenshots of the globe opening and the site jump. Leaves the scene as built.</summary>
    public static partial class AutoTest
    {
        static partial void CheckGlobe(WalkbackApp app)
        {
            var globe = app.globe;
            var jump = app.siteJump;
            Check("Globe: opening and site jump are in the scene", globe != null && jump != null);
            Check("Globe: hidden in the built scene (it opens at runtime)", !globe.visual.activeSelf && !jump.sphere.activeSelf);
            globe.EnsureBuilt();
            jump.EnsureBuilt();

            // One latitude/longitude math for mesh, image and light (trap 11)
            Check("Globe math: 0° N 0° E faces the viewer (-z)", GlobeNear(GlobeMath.LatLonToLocal(0f, 0f, 1f), new Vector3(0f, 0f, -1f)));
            Check("Globe math: 90° N is the top", GlobeNear(GlobeMath.LatLonToLocal(90f, 37f, 1f), Vector3.up));
            Check("Globe math: 90° E is on the viewer's right", GlobeNear(GlobeMath.LatLonToLocal(0f, 90f, 1f), Vector3.right));
            Vector2 back = GlobeMath.LocalToLatLon(GlobeMath.LatLonToLocal(globe.SiteLatitude, globe.SiteLongitude, globe.radius));
            Check("Globe math: the site round-trips", Mathf.Abs(back.x - globe.SiteLatitude) < 0.01f && Mathf.Abs(Mathf.DeltaAngle(back.y, globe.SiteLongitude)) < 0.01f,
                globe.SiteName + " at " + GlobeOpening.LatLonText(globe.SiteLatitude, globe.SiteLongitude));
            if (!app.terrain.Site.IsMarsData)
                Check("Globe: while the table is a test, the light is on Gale crater (5.4° S, 137.8° E)", Mathf.Approximately(globe.SiteLatitude, -5.4f) && Mathf.Approximately(globe.SiteLongitude, 137.8f));

            Mesh mesh = globe.pivot.GetComponent<MeshFilter>().sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uv = mesh.uv;
            Vector3[] normals = mesh.normals;
            float worst = 0f;
            bool outward = true;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Vector3.Dot(normals[i], vertices[i]) <= 0f) outward = false;
                Vector2 ll = GlobeMath.LocalToLatLon(vertices[i]);
                if (Mathf.Abs(ll.x) > 89f) continue;   // longitude means nothing at the poles
                Vector2 expected = GlobeMath.LatLonToUV(ll.x, ll.y, GlobeMath.GlobeImageWestEdge);
                float du = Mathf.Abs(uv[i].x - expected.x);
                du = Mathf.Min(du, 1f - du);           // the seam column is both 0 and 1
                worst = Mathf.Max(worst, Mathf.Max(du, Mathf.Abs(uv[i].y - expected.y)));
            }
            Check("Globe: the image is mapped with the same lat/lon math as the light (trap 11)", worst < 0.001f, "worst uv error " + worst.ToString("F5"));
            Check("Globe: normals point outward", outward);
            Check("Globe: the light sits on the site", Vector3.Distance(globe.beacon.localPosition, GlobeMath.LatLonToLocal(globe.SiteLatitude, globe.SiteLongitude, globe.radius)) < 0.0001f);
            Note(globe.HasMarsImage
                ? "GLOBE: Site/mars_global.jpg loaded, " + globe.GlobeTexture.width + " x " + globe.GlobeTexture.height
                : "GLOBE: plain sphere (no Site/mars_global.jpg yet)");
            string hintText = string.Join(" | ", System.Array.ConvertAll(globe.hint.lines, l => l.text));
            Check("Globe: no Mars picture is faked (plain sphere is labelled)", globe.HasMarsImage || hintText.Contains("no global Mars image"), hintText);
            Check("Globe: the hint names the site and its coordinates", hintText.Contains(globe.SiteName) && hintText.Contains(GlobeOpening.LatLonText(globe.SiteLatitude, globe.SiteLongitude)));
            if (!app.terrain.Site.IsMarsData)
                Check("Globe: says the table terrain is a test", hintText.Contains("TEST TERRAIN"));
            float titleMeters = globe.title.lines[0].fontSize * globe.title.canvasRect.lossyScale.y;
            Check("Globe: WALKBACK title at least 0.05 m tall", globe.title.lines[0].text == "WALKBACK" && titleMeters >= WalkbackTheme.MinTitleHeightMeters, titleMeters.ToString("F3") + " m");
            bool uiOk = true;
            foreach (var label in new[] { globe.title, globe.hint, globe.siteLabel, jump.card, jump.noPanorama })
                foreach (var t in label.lines)
                    if (t.material == null || t.material.shader.name != "Walkback/UI") uiOk = false;
            Check("Globe: every text on a dark box and never writing alpha (passthrough)", uiOk);
            Check("Globe: pressing the planet reaches the globe (pointer target)", globe.pivot.GetComponent<Collider>().GetComponentInParent<IPointerTarget>() == (IPointerTarget)globe);

            // The opening and the landing, as end states
            Vector3 mapScale = app.mapRoot.localScale, mapPosition = app.mapRoot.position;
            bool panelWas = app.panel.gameObject.activeSelf;
            globe.SetOpeningState();
            Vector3 eye = globe.Eye();
            Check("Opening: globe shown; map and panel hidden; table controls locked",
                globe.visual.activeSelf && !app.mapRoot.gameObject.activeSelf && !app.panel.gameObject.activeSelf && app.MapControlsLocked);
            float facing = Vector3.Dot((globe.beacon.position - globe.center).normalized, (eye - globe.center).normalized);
            Check("Opening: the light faces the viewer", facing > 0.95f, "cos " + facing.ToString("F3"));
            Check("Opening: the planet floats above the table", globe.center.y - globe.radius > SceneBuilder.TableHeight + 0.1f);

            globe.ApplyTransition(0.40f);
            Vector3 lightEarly = globe.beacon.position;
            globe.ApplyTransition(0.85f);
            Vector3 lightLate = globe.beacon.position;
            float s = globe.pivot.localScale.x;
            Check("Landing: the planet shrinks into its light", s > 0f && s < 1f && Vector3.Distance(lightEarly, lightLate) < 0.002f,
                "scale " + s.ToString("F2") + ", light moved " + (Vector3.Distance(lightEarly, lightLate) * 1000f).ToString("F1") + " mm");
            Check("Landing: the map grows while the controls stay locked",
                app.mapRoot.gameObject.activeSelf && app.mapRoot.localScale.x > 0f && app.mapRoot.localScale.x < mapScale.x && app.MapControlsLocked);
            globe.FinishTransition();
            Check("Landing: map back at exactly its size and place, panel back, globe hidden, controls free",
                app.mapRoot.localScale == mapScale && Vector3.Distance(app.mapRoot.position, mapPosition) < 1e-5f && app.mapRoot.gameObject.activeSelf &&
                app.panel.gameObject.activeSelf == panelWas && !globe.visual.activeSelf && !app.MapControlsLocked && globe.Current == GlobeOpening.State.Done);
            globe.ResetForTest();

            // The panorama sphere: seen from inside, not mirrored (trap 12)
            Mesh pano = jump.sphere.GetComponent<MeshFilter>().sharedMesh;
            Check("Jump: right of the viewer shows the right half of the photo (not mirrored)",
                Mathf.Abs(GlobeNearestU(pano, GlobeMath.PanoramaDirection(90f, 0f)) - 0.75f) < 0.02f &&
                Mathf.Abs(GlobeNearestU(pano, GlobeMath.PanoramaDirection(-90f, 0f)) - 0.25f) < 0.02f &&
                Mathf.Abs(GlobeNearestU(pano, GlobeMath.PanoramaDirection(0f, 0f)) - 0.5f) < 0.02f);
            bool inward = true;
            Vector3[] pv = pano.vertices, pn = pano.normals;
            for (int i = 0; i < pv.Length; i++) if (Vector3.Dot(pn[i], pv[i]) >= 0f) inward = false;
            Check("Jump: the photo sphere faces inward", inward);
            var sphereMaterial = jump.sphere.GetComponent<MeshRenderer>().sharedMaterial;
            Check("Jump: the photo sphere is opaque (hides the room in passthrough)", sphereMaterial.renderQueue < 2500 && (!sphereMaterial.HasProperty("_Surface") || sphereMaterial.GetFloat("_Surface") < 0.5f));
            var small = new Texture2D(16, 8, TextureFormat.RGBA32, false);
            var large = new Texture2D(64, 32, TextureFormat.RGBA32, false);
            Texture capped = SiteJump.CapWidth(large, 32);
            Check("Jump: photos wider than 4096 px are scaled down, others kept as they are (trap 13)",
                SiteJump.CapWidth(small, 32) == small && capped.width == 32 && capped.height == 16);
            if (capped is RenderTexture) ((RenderTexture)capped).Release();
            Object.DestroyImmediate(capped);
            Object.DestroyImmediate(small);
            Object.DestroyImmediate(large);

            bool tableWas = app.table.gameObject.activeSelf;
            var texture = GlobeTestPanorama(512, 256);
            jump.EnterJump(texture, "TEST PANORAMA", "Test grid made by the automatic test, not a photo", false);
            Check("Jump: inside the photo; table, map and panel hidden",
                jump.Inside && jump.sphere.activeSelf && !app.table.gameObject.activeSelf && !app.mapRoot.gameObject.activeSelf && !app.panel.gameObject.activeSelf);
            Vector3 jumpEye = app.desktopCamera.transform.position;
            Check("Jump: the sphere surrounds the viewer", Vector3.Distance(jump.sphere.transform.position, jumpEye) < 0.01f && SiteJump.SphereRadius >= 5f);
            Check("Jump: name, credit and the way back are in front of the viewer",
                jump.card.gameObject.activeSelf && jump.card.lines[0].text == "TEST PANORAMA" && jump.card.lines[2].text == SiteJump.ReturnHint &&
                Vector3.Dot(jump.card.transform.position - jumpEye, Vector3.forward) > 0.5f);
            var sphereCollider = jump.sphere.GetComponent<MeshCollider>();
            RaycastHit hit;
            bool hits = sphereCollider.Raycast(new Ray(jumpEye, Vector3.right), out hit, 20f);
            Check("Jump: pressing anywhere hits the photo sphere, which leads back", hits && sphereCollider.GetComponentInParent<IPointerTarget>() == (IPointerTarget)jump);
            // The same press through the real pointer: its ray is only Pointer.MaxDistance long, so it must
            // be longer than the sphere's radius, or hands (no B button) can never leave the photo.
            // The ray points up, right and behind the viewer: away from the table setup, so only the sphere is there.
            Physics.SyncTransforms();
            RaycastHit reach;
            bool reaches = Physics.Raycast(new Ray(jumpEye, new Vector3(0.6f, 0.5f, -0.6f).normalized), out reach, Pointer.MaxDistance, ~0, QueryTriggerInteraction.Collide) && reach.collider == sphereCollider;
            Check("Jump: the pointer's own ray is long enough to reach the photo sphere", Pointer.MaxDistance > SiteJump.SphereRadius && reaches,
                "ray " + Pointer.MaxDistance.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " m, sphere " + SiteJump.SphereRadius.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + " m");
            jump.OnPointerClick(hit.point);
            // In and out of the photo go behind the mission mode's curtain at runtime; outside Play the
            // same calls change the world at once, which is what this test drives.
            bool curtainRan = false;
            bool curtainTook = app.mission != null && app.mission.BehindCurtain(MissionMode.FadeSeconds, app.desktopCamera.transform, () => curtainRan = true);
            Check("Jump: the way in and out of the photo goes behind the fade curtain (at once outside Play)",
                !jump.Inside && app.mission != null && app.mission.curtain != null && curtainTook && curtainRan && !app.mission.Fading && !app.mission.curtain.gameObject.activeSelf);
            Check("Jump: back at the table, everything as before",
                !jump.Inside && !jump.sphere.activeSelf && app.table.gameObject.activeSelf == tableWas && app.mapRoot.gameObject.activeSelf && app.panel.gameObject.activeSelf == panelWas && !app.MapControlsLocked);

            // A stop without a photo says so instead of showing anything
            var stops = app.terrain.Site.Stops;
            if (stops.Length > 0)
            {
                jump.Jump(0);
                if (jump.Inside)
                {
                    Check("Jump: stop 1's panorama loads at most 4096 px wide", jump.CurrentTexture != null && jump.CurrentTexture.width <= SiteJump.MaxPanoramaWidth,
                        stops[0].name + ": " + jump.CurrentTexture.width + " x " + jump.CurrentTexture.height);
                    jump.ExitJump();
                }
                else
                    Check("Jump: a stop without a photo shows \"" + SiteJump.NoPanoramaText + "\"", jump.noPanorama.gameObject.activeSelf && jump.noPanorama.lines[0].text == SiteJump.NoPanoramaText, stops[0].name);
                jump.HideNoPanorama();
            }
            Object.DestroyImmediate(texture);
            Check("Globe checks leave the scene as built", app.mapRoot.localScale == mapScale && app.mapRoot.gameObject.activeSelf && app.table.gameObject.activeSelf == tableWas && !globe.visual.activeSelf && !jump.Inside);
        }

        static partial void ShotsGlobe(WalkbackApp app)
        {
            var globe = app.globe;
            var jump = app.siteJump;
            var cam = app.desktopCamera;
            Vector3 home = cam.transform.position;
            Quaternion homeRotation = cam.transform.rotation;

            globe.SetOpeningState();
            Canvas.ForceUpdateCanvases();
            Shot(cam, "globe_01_opening.png", SceneBuilder.DesktopEye, new Vector3(0f, 1.22f, 1.0f));
            Vector3 lightWorld = globe.beacon.position;
            Shot(cam, "globe_02_site_light.png", Vector3.Lerp(SceneBuilder.DesktopEye, lightWorld, 0.55f), lightWorld);
            globe.ApplyTransition(0.75f);
            Shot(cam, "globe_03_landing.png", SceneBuilder.DesktopEye, new Vector3(0f, 1.05f, 1.2f));
            globe.FinishTransition();
            globe.ResetForTest();

            cam.transform.SetPositionAndRotation(home, Quaternion.LookRotation(Vector3.forward));
            var texture = GlobeTestPanorama(2048, 1024);
            jump.EnterJump(texture, "TEST PANORAMA", "Test grid made by the automatic test, not a photo", false);
            Canvas.ForceUpdateCanvases();
            Shot(cam, "globe_04_jump_ahead.png", home, home + Vector3.forward + Vector3.down * 0.25f);
            Shot(cam, "globe_05_jump_right.png", home, home + Vector3.right);
            jump.ExitJump();
            Object.DestroyImmediate(texture);

            cam.transform.SetPositionAndRotation(home, homeRotation);
            jump.ShowNoPanorama("TEST STOP 1");
            Canvas.ForceUpdateCanvases();
            Shot(cam, "globe_06_no_panorama.png", home, home + Vector3.forward + Vector3.down * 0.3f);
            jump.HideNoPanorama();
            cam.transform.SetPositionAndRotation(home, homeRotation);
        }

        static bool GlobeNear(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b) < 1e-4f;
        }

        /// <summary>u of the panorama vertex closest to a direction.</summary>
        static float GlobeNearestU(Mesh mesh, Vector3 direction)
        {
            Vector3[] v = mesh.vertices;
            Vector2[] uv = mesh.uv;
            int best = 0;
            float bestDot = -2f;
            for (int i = 0; i < v.Length; i++)
            {
                float d = Vector3.Dot(v[i].normalized, direction.normalized);
                if (d > bestDot) { bestDot = d; best = i; }
            }
            return uv[best].x;
        }

        /// <summary>
        /// A test-only panorama (never shown at runtime): a grid every 30°, the horizon, and three
        /// markers: green straight ahead (u 0.5), red on the right (u 0.75), blue on the left (u 0.25).
        /// </summary>
        static Texture2D GlobeTestPanorama(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true) { name = "TestPanorama", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float el = (y + 0.5f) / height * 180f - 90f;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float az = (u - 0.5f) * 360f;
                    Color c = Color.Lerp(new Color(0.20f, 0.17f, 0.15f), new Color(0.42f, 0.30f, 0.22f), (el + 90f) / 180f);
                    if (Mathf.Abs(Mathf.Repeat(az + 15f, 30f) - 15f) < 0.35f || Mathf.Abs(Mathf.Repeat(el + 15f, 30f) - 15f) < 0.35f) c = new Color(0.6f, 0.6f, 0.6f);
                    if (Mathf.Abs(el) < 0.5f) c = Color.white;
                    if (Mathf.Abs(el) < 12f)
                    {
                        if (Mathf.Abs(az) < 10f) c = new Color(0.2f, 0.8f, 0.3f);
                        if (Mathf.Abs(az - 90f) < 10f) c = new Color(0.9f, 0.2f, 0.15f);
                        if (Mathf.Abs(az + 90f) < 10f) c = new Color(0.2f, 0.45f, 0.95f);
                    }
                    pixels[y * width + x] = c;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }
    }
}
